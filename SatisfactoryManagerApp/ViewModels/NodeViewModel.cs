using System.Collections.ObjectModel;
using System.Windows;
using SatisfactoryManagerApp.Graph.Nodes;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Observable wrapper for a NodeModel of any type.
    /// Nodify binds Location to place the node on the canvas.
    /// </summary>
    public class NodeViewModel : ViewModelBase
    {
        private Point _location;
        private string _name = string.Empty;
        private double _efficiency = 1.0;
        private double _totalPowerMW;

        // ── Identity ──────────────────────────────────────────────────────────

        public Guid NodeId { get; }
        public NodeModel Model { get; }
        public string NodeType { get; }  // "Machine", "Group", "Splitter", "Merger"

        // ── Bindable properties ───────────────────────────────────────────────

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        /// <summary>Position on the Nodify canvas (in canvas coordinates).</summary>
        public Point Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        /// <summary>
        /// Machine efficiency (0–1). Only meaningful for MachineNode.
        /// Shown as percentage label on the node card.
        /// </summary>
        public double Efficiency
        {
            get => _efficiency;
            set
            {
                SetField(ref _efficiency, value);
                OnPropertyChanged(nameof(EfficiencyPercent));
                OnPropertyChanged(nameof(IsStarved));
            }
        }

        public string EfficiencyPercent => $"{Efficiency * 100:F0}%";
        public bool IsStarved => Efficiency < 0.99;

        /// <summary>
        /// Total power of the group's machines (MW). Only used for FactoryGroupNode.
        /// </summary>
        public double TotalPowerMW
        {
            get => _totalPowerMW;
            set => SetField(ref _totalPowerMW, value);
        }

        // ── Ports ─────────────────────────────────────────────────────────────

        public ObservableCollection<PortViewModel> Inputs { get; } = new();
        public ObservableCollection<PortViewModel> Outputs { get; } = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public NodeViewModel(NodeModel model)
        {
            Model = model;
            NodeId = model.Id;
            _name = model.Name;
            _location = new Point(model.X, model.Y);

            NodeType = model switch
            {
                MachineNode => "Machine",
                FactoryGroupNode => "Group",
                SplitterNode => "Splitter",
                MergerNode => "Merger",
                _ => "Unknown"
            };

            foreach (var p in model.Inputs)
                Inputs.Add(new PortViewModel(p));
            foreach (var p in model.Outputs)
                Outputs.Add(new PortViewModel(p));
        }

        // ── Sync after recalculation ──────────────────────────────────────────

        /// <summary>
        /// Pulls updated values from the domain model after FlowCalculator runs.
        /// </summary>
        public void Sync()
        {
            Name = Model.Name;

            if (Model is MachineNode m)
            {
                Efficiency = m.Efficiency;
                TotalPowerMW = m.ActualPowerMW;
            }
            else if (Model is FactoryGroupNode g)
            {
                TotalPowerMW = g.TotalPowerMW;
            }

            // Sync port flows
            for (int i = 0; i < Inputs.Count && i < Model.Inputs.Count; i++)
                Inputs[i].Sync(Model.Inputs[i].CurrentFlow);
            for (int i = 0; i < Outputs.Count && i < Model.Outputs.Count; i++)
                Outputs[i].Sync(Model.Outputs[i].CurrentFlow);

            // Keep canvas position in sync
            Model.X = Location.X;
            Model.Y = Location.Y;
        }
    }
}
