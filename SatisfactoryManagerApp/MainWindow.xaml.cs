using System.Windows;
using System.Windows.Input;
using Nodify;
using SatisfactoryManagerApp.ViewModels;

namespace SatisfactoryManagerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new EditorViewModel();
        }

        /// <summary>
        /// Handles double-click on any Nodify ItemContainer.
        /// If the node is a FactoryGroupNode, fires EnterGroupCommand to navigate inside.
        /// </summary>
        private void ItemContainer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: NodeViewModel nvm }
                && DataContext is EditorViewModel vm)
            {
                if (vm.EnterGroupCommand.CanExecute(nvm))
                {
                    vm.EnterGroupCommand.Execute(nvm);
                    e.Handled = true;   // prevent bubbling to NodifyEditor
                }
            }
        }
    }
}