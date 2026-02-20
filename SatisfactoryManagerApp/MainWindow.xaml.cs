using System.Windows;
using SatisfactoryManagerApp.ViewModels;

namespace SatisfactoryManagerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Wires the EditorViewModel as DataContext.
    /// Connection lifecycle (create/remove) is handled via Nodify's
    /// ConnectionCompletedCommand and RemoveConnectionCommand bound in XAML.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new EditorViewModel();
        }
    }
}