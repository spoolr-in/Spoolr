using System.Windows;

namespace SpoolrStation.Views
{
    public partial class CompletionDialog : Window
    {
        public CompletionDialog()
        {
            InitializeComponent();
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}