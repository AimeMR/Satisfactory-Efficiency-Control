using System.Windows;
using System.Windows.Input;
using Nodify;
using SatisfactoryManagerApp.ViewModels;

namespace SatisfactoryManagerApp
{
    public partial class MainWindow : Window
    {
        // ── DragDrop state ────────────────────────────────────────────────────
        private Point _dragStartPoint;
        private MachineLibraryItem? _dragItem;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new EditorViewModel();
        }

        // ── Double-click: enter FactoryGroupNode ──────────────────────────────

        private void ItemContainer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: NodeViewModel nvm }
                && DataContext is EditorViewModel vm)
            {
                if (vm.EnterGroupCommand.CanExecute(nvm))
                {
                    vm.EnterGroupCommand.Execute(nvm);
                    e.Handled = true;
                }
            }
        }

        // ── Sidebar drag trigger ──────────────────────────────────────────────

        private void MachineList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _dragItem = (e.OriginalSource as FrameworkElement)?.DataContext as MachineLibraryItem;
        }

        private void MachineList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed) return;

            var delta = e.GetPosition(null) - _dragStartPoint;
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            // Start the WPF drag operation — DragDrop data is the library item itself
            DragDrop.DoDragDrop(MachineList, _dragItem, DragDropEffects.Copy);
            _dragItem = null;
        }

        // ── Canvas drop ───────────────────────────────────────────────────────

        private void Editor_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetData(typeof(MachineLibraryItem)) is MachineLibraryItem
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Editor_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(MachineLibraryItem)) is not MachineLibraryItem item) return;
            if (DataContext is not EditorViewModel vm) return;

            // Convert drop position from editor-local space → canvas (viewport) space
            var editorPos = e.GetPosition(Editor);
            var canvasPos = Editor.ViewportTransform.Inverse.Transform(editorPos);

            vm.AddMachineByName(item.MachineName, canvasPos.X, canvasPos.Y);
            e.Handled = true;
        }
    }
}