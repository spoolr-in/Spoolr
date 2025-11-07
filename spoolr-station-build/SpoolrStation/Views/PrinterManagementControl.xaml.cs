using System.Windows;
using System.Windows.Controls;
using SpoolrStation.ViewModels;

namespace SpoolrStation.Views
{
    /// <summary>
    /// Interaction logic for PrinterManagementControl.xaml
    /// </summary>
    public partial class PrinterManagementControl : System.Windows.Controls.UserControl
    {
        public PrinterManagementControl()
        {
            InitializeComponent();
        }

        private void SelectOnlinePrinters_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mainViewModel && mainViewModel.PrintersViewModel != null)
            {
                mainViewModel.PrintersViewModel.SelectOnlinePrinters();
            }
        }
    }
}
