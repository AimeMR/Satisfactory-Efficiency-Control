using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SatisfactoryManagerApp.Graph;
using SatisfactoryManagerApp.Graph.Nodes;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Root ViewModel bound to the NodifyEditor control.
    /// Manages the currently visible graph context (level), navigation stack,
    /// and orchestrates recalculation via FlowCalculator.
    /// </summary>
    public class EditorViewModel : ViewModelBase
    {
        // ── Domain state ──────────────────────────────────────────────────────

        /// <summary>All nodes in the entire graph (all levels).</summary>
        private readonly List<NodeModel> _allNodes = new();

        /// <summary>All connections in the entire graph (all levels).</summary>
        private readonly List<ConnectionModel> _allConnections = new();

        private readonly FlowCalculator _calculator = new();

        // ── Navigation ────────────────────────────────────────────────────────

        /// <summary>
        /// Stack of group nodes the user has navigated into.
        /// Empty = at root level; peek = current group being viewed.
        /// </summary>
        private readonly Stack<FactoryGroupNode> _navigationStack = new();

        private string _breadcrumbPath = "Fábrica principal";

        public string BreadcrumbPath
        {
            get => _breadcrumbPath;
            private set => SetField(ref _breadcrumbPath, value);
        }

        /// <summary>
        /// Clickable breadcrumb segments exposed to the breadcrumb ItemsControl.
        /// Each item carries a label and the group it represents (null = root).
        /// </summary>
        public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

        /// <summary>
        /// Bound TwoWay to NodifyEditor.ViewportLocation so we can reset the
        /// camera to (0,0) whenever the user navigates to a different level.
        /// </summary>
        private Point _viewportLocation;
        public Point ViewportLocation
        {
            get => _viewportLocation;
            set => SetField(ref _viewportLocation, value);
        }

        // ── Visible collections (bound to NodifyEditor) ───────────────────────

        /// <summary>Nodes visible in the current level of the editor.</summary>
        public ObservableCollection<NodeViewModel> Nodes { get; } = new();

        /// <summary>Connections visible in the current level of the editor.</summary>
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Invoked on double-click of a FactoryGroupNode — enters its interior.</summary>
        public ICommand EnterGroupCommand { get; }

        /// <summary>Goes back one level in the navigation stack.</summary>
        public ICommand GoBackCommand { get; }

        /// <summary>Triggers FlowCalculator and refreshes all visible ViewModels.</summary>
        public ICommand RecalculateCommand { get; }

        /// <summary>Adds a new placeholder MachineNode to the current level.</summary>
        public ICommand AddMachineCommand { get; }

        /// <summary>Adds a new FactoryGroupNode to the current level.</summary>
        public ICommand AddGroupCommand { get; }

        /// <summary>
        /// Nodify 7: invoked when the user finishes dragging a connection wire.
        /// Parameter is Tuple&lt;object, object&gt; where Item1=source port VM, Item2=target port VM.
        /// </summary>
        public ICommand ConnectionCompletedCommand { get; }

        /// <summary>
        /// Nodify 7: invoked when the user deletes a connection.
        /// Parameter is the ConnectionViewModel (DataContext of the wire).
        /// </summary>
        public ICommand RemoveConnectionCommand { get; }

        /// <summary>Navigates directly to any ancestor level when the user clicks a breadcrumb segment.</summary>
        public ICommand GoToLevelCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public EditorViewModel()
        {
            EnterGroupCommand = new RelayCommand<NodeViewModel>(EnterGroup,
                nvm => nvm?.Model is FactoryGroupNode);
            GoBackCommand = new RelayCommand(GoBack,
                () => _navigationStack.Count > 0);
            RecalculateCommand = new RelayCommand(Recalculate);
            AddMachineCommand = new RelayCommand(AddMachine);
            AddGroupCommand = new RelayCommand(AddGroup);

            // Nodify 7 MVVM commands for connection lifecycle
            ConnectionCompletedCommand = new RelayCommand<Tuple<object, object>>(OnConnectionCompleted);
            RemoveConnectionCommand = new RelayCommand<ConnectionViewModel>(DisconnectPorts);
            GoToLevelCommand = new RelayCommand<BreadcrumbItem>(GoToLevel);

            // Start at root with an empty canvas
            RefreshVisibleLevel();
        }

        // ── Navigation logic ──────────────────────────────────────────────────

        private void EnterGroup(NodeViewModel? nvm)
        {
            if (nvm?.Model is not FactoryGroupNode group) return;
            _navigationStack.Push(group);
            UpdateBreadcrumb();
            RefreshVisibleLevel();
        }

        private void GoBack()
        {
            if (_navigationStack.Count == 0) return;
            _navigationStack.Pop();
            UpdateBreadcrumb();
            RefreshVisibleLevel();
        }

        private void UpdateBreadcrumb()
        {
            // Plain text path (still used for window title / tooltip)
            if (_navigationStack.Count == 0)
            {
                BreadcrumbPath = "Fábrica principal";
            }
            else
            {
                var parts = _navigationStack.Reverse().Select(g => g.Name);
                BreadcrumbPath = "Fábrica principal > " + string.Join(" > ", parts);
            }

            // Clickable breadcrumb items (root first)
            BreadcrumbItems.Clear();
            BreadcrumbItems.Add(new BreadcrumbItem("Fábrica principal", null));
            foreach (var group in _navigationStack.Reverse())
                BreadcrumbItems.Add(new BreadcrumbItem(group.Name, group));
        }

        // ── Refresh visible level ─────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the Nodes and Connections collections based on the current
        /// navigation context (root or a specific FactoryGroupNode's children).
        /// </summary>
        private void RefreshVisibleLevel()
        {
            Nodes.Clear();
            Connections.Clear();

            // Reset camera so the user doesn't appear at a blank area
            ViewportLocation = new Point(0, 0);

            IEnumerable<NodeModel> visibleNodes = _navigationStack.Count == 0
                ? _allNodes.Where(n => n.ParentGroupId == null)
                : _navigationStack.Peek().Children;

            // Lookup table: portId → NodeViewModel (for wiring connections)
            var portToNodeVm = new Dictionary<Guid, NodeViewModel>();
            var nodeVmById = new Dictionary<Guid, NodeViewModel>();

            foreach (var node in visibleNodes)
            {
                var nvm = new NodeViewModel(node);
                Nodes.Add(nvm);
                nodeVmById[node.Id] = nvm;
                foreach (var p in nvm.Inputs) portToNodeVm[p.PortId] = nvm;
                foreach (var p in nvm.Outputs) portToNodeVm[p.PortId] = nvm;
            }

            // Only show connections whose both endpoints are in this level
            var visibleIds = nodeVmById.Keys.ToHashSet();
            foreach (var conn in _allConnections)
            {
                if (!visibleIds.Contains(conn.SourceNodeId) ||
                    !visibleIds.Contains(conn.TargetNodeId)) continue;

                var srcVm = nodeVmById[conn.SourceNodeId].Outputs
                                .FirstOrDefault(p => p.PortId == conn.SourcePortId);
                var tgtVm = nodeVmById[conn.TargetNodeId].Inputs
                                .FirstOrDefault(p => p.PortId == conn.TargetPortId);

                if (srcVm is null || tgtVm is null) continue;
                Connections.Add(new ConnectionViewModel(conn, srcVm, tgtVm));
            }
        }

        // ── Recalculation ─────────────────────────────────────────────────────

        private void Recalculate()
        {
            _calculator.Calculate(_allNodes, _allConnections);

            foreach (var nvm in Nodes)
                nvm.Sync();
            foreach (var cvm in Connections)
                cvm.Sync();
        }

        // ── Add helpers ───────────────────────────────────────────────────────

        private void AddMachine()
        {
            var node = new MachineNode { Name = "Nueva Máquina", X = 100, Y = 100 };
            RegisterNode(node);
        }

        private void AddGroup()
        {
            var group = new FactoryGroupNode { Name = "Nuevo Grupo", X = 200, Y = 200 };
            RegisterNode(group);
        }

        private void RegisterNode(NodeModel node)
        {
            // Assign parent group if navigated inside one
            if (_navigationStack.Count > 0)
            {
                _navigationStack.Peek().AddChild(node);
            }
            else
            {
                node.ParentGroupId = null;
                _allNodes.Add(node);
            }
            Nodes.Add(new NodeViewModel(node));
        }

        // ── Public API (called by Nodify's ConnectionCompletedCommand / RemoveConnectionCommand) ─

        /// <summary>
        /// Navigates directly to a specific ancestor level via breadcrumb click.
        /// Pops the navigation stack until the target group is on top
        /// (or until root, if group == null).
        /// </summary>
        private void GoToLevel(BreadcrumbItem? item)
        {
            if (item is null) return;

            if (item.Group is null)
            {
                // Navigate all the way back to root
                _navigationStack.Clear();
            }
            else
            {
                // Pop until the target group is the current context
                while (_navigationStack.Count > 0 && _navigationStack.Peek() != item.Group)
                    _navigationStack.Pop();
            }

            UpdateBreadcrumb();
            RefreshVisibleLevel();
        }

        /// <summary>
        /// Called by Nodify's ConnectionCompletedCommand.
        /// Tuple.Item1 = source PortViewModel (OutputPort), Item2 = target PortViewModel (InputPort).
        /// </summary>
        private void OnConnectionCompleted(Tuple<object, object>? tuple)
        {
            if (tuple?.Item1 is PortViewModel src && tuple.Item2 is PortViewModel tgt)
                ConnectPorts(src, tgt);
        }

        /// <summary>
        /// Creates a connection between two ports.
        /// </summary>
        public void ConnectPorts(PortViewModel source, PortViewModel target)
        {
            // Find owner nodes
            var srcNodeVm = Nodes.FirstOrDefault(n => n.Outputs.Contains(source));
            var tgtNodeVm = Nodes.FirstOrDefault(n => n.Inputs.Contains(target));
            if (srcNodeVm is null || tgtNodeVm is null) return;

            var conn = new ConnectionModel
            {
                SourceNodeId = srcNodeVm.NodeId,
                SourcePortId = source.PortId,
                TargetNodeId = tgtNodeVm.NodeId,
                TargetPortId = target.PortId
            };
            _allConnections.Add(conn);
            Connections.Add(new ConnectionViewModel(conn, source, target));
        }

        /// <summary>
        /// Removes a connection (called via RemoveConnectionCommand from Nodify).
        /// </summary>
        public void DisconnectPorts(ConnectionViewModel? cvm)
        {
            if (cvm is null) return;
            _allConnections.Remove(cvm.Model);
            Connections.Remove(cvm);
        }
    }
}
