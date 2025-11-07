using System.Windows;
using SpoolrStation.ViewModels;
using SpoolrStation.ViewModels.JobOffers;
using SpoolrStation.WebSocket.Models;

namespace SpoolrStation.Views
{
    /// <summary>
    /// Interaction logic for JobOfferWindow.xaml
    /// </summary>
    public partial class JobOfferWindow : Window
    {
        public JobOfferWindow()
        {
            InitializeComponent();
        }

        public JobOfferWindow(JobOfferMessage jobOffer) : this()
        {
            try
            {
                System.Console.WriteLine($"=== JobOfferWindow Constructor - JobId: {jobOffer.JobId} ===");
                System.Console.WriteLine($"Creating JobOfferViewModel...");
                var viewModel = new JobOfferViewModel(jobOffer);
                System.Console.WriteLine($"JobOfferViewModel created successfully");
                
                // Subscribe to events for window management only (closing window)
                System.Console.WriteLine($"Subscribing to window management events...");
                viewModel.JobAccepted += OnJobAccepted; // For closing window
                viewModel.JobDeclined += OnJobDeclined; // For closing window
                viewModel.JobExpired += OnJobExpired; // For closing window
                System.Console.WriteLine($"Window management events subscribed successfully");
                
                System.Console.WriteLine($"Setting DataContext...");
                DataContext = viewModel;
                System.Console.WriteLine($"DataContext set successfully - ViewModel type: {viewModel.GetType().Name}");
                
                // Timer is started automatically in the ViewModel constructor
                System.Console.WriteLine($"JobOfferWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"=== EXCEPTION IN JobOfferWindow Constructor ===");
                System.Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Console.WriteLine($"Exception Message: {ex.Message}");
                System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Console.WriteLine($"============================================");
                throw;
            }
        }

        private void OnJobAccepted(object? sender, JobOfferResponseEventArgs e)
        {
            // Close window after brief delay to show success message
            Dispatcher.BeginInvoke(() =>
            {
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(Close);
                });
            });
        }

        private void OnJobDeclined(object? sender, JobOfferResponseEventArgs e)
        {
            // Close window after brief delay to show success message
            Dispatcher.BeginInvoke(() =>
            {
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(Close);
                });
            });
        }

        private void OnJobExpired(object? sender, JobOfferResponseEventArgs e)
        {
            // Auto-close expired offers after showing the message
            Dispatcher.BeginInvoke(() =>
            {
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(Close);
                });
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup: unsubscribe from events
            if (DataContext is JobOfferViewModel viewModel)
            {
                viewModel.JobAccepted -= OnJobAccepted;
                viewModel.JobDeclined -= OnJobDeclined;
                viewModel.JobExpired -= OnJobExpired;
                viewModel.Dispose();
            }
            
            base.OnClosed(e);
        }

        // Handle window closing to ensure proper cleanup
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Allow window to close, cleanup is handled in OnClosed
            base.OnClosing(e);
        }
    }
}