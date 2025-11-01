using System;
using System.Windows.Input;
using System.Windows.Threading;
using SpoolrStation.Models;
using SpoolrStation.Utilities;
using SpoolrStation.WebSocket.Models;

namespace SpoolrStation.ViewModels.JobOffers
{
    /// <summary>
    /// ViewModel for job offer popup window with countdown timer and accept/decline functionality
    /// </summary>
    public class JobOfferViewModel : BaseViewModel, IDisposable
    {
        private readonly JobOfferMessage _jobOffer;
        private readonly DispatcherTimer _countdownTimer;
        private int _secondsRemaining;
        private bool _isExpired;
        private bool _hasResponded;
        private bool _disposed;

        // Events for the parent window to handle
        public event EventHandler<JobOfferResponseEventArgs>? JobAccepted;
        public event EventHandler<JobOfferResponseEventArgs>? JobDeclined;
        public event EventHandler<JobOfferResponseEventArgs>? JobExpired;

    public JobOfferViewModel(JobOfferMessage jobOffer)
    {
        System.Console.WriteLine($"=== JOBOFFERVM CONSTRUCTOR: Thread {System.Threading.Thread.CurrentThread.ManagedThreadId} ===");
        System.Console.WriteLine($"Is UI Thread: {System.Windows.Application.Current.Dispatcher.CheckAccess()}");
        
        _jobOffer = jobOffer ?? throw new ArgumentNullException(nameof(jobOffer));
        
        // Debug output to see what data we're receiving
        System.Diagnostics.Debug.WriteLine($"=== JOB OFFER DEBUG ===");
        System.Diagnostics.Debug.WriteLine($"JobId: {_jobOffer.JobId}");
        System.Diagnostics.Debug.WriteLine($"Customer: '{_jobOffer.Customer}'");
        System.Diagnostics.Debug.WriteLine($"FileName: '{_jobOffer.FileName}'");
        System.Diagnostics.Debug.WriteLine($"PrintSpecs: '{_jobOffer.PrintSpecs}'");
        System.Diagnostics.Debug.WriteLine($"TotalPrice: {_jobOffer.TotalPrice}");
        System.Diagnostics.Debug.WriteLine($"Earnings: {_jobOffer.Earnings}");
        System.Diagnostics.Debug.WriteLine($"TrackingCode: '{_jobOffer.TrackingCode}'");
        System.Diagnostics.Debug.WriteLine($"JobSummary: '{JobSummary}'");
        System.Diagnostics.Debug.WriteLine($"========================");
            
            // Calculate initial seconds remaining
            // Use offerExpiresInSeconds directly as fallback if ExpiresAt calculation fails
            var timeSpan = _jobOffer.ExpiresAt - DateTime.UtcNow;
            _secondsRemaining = Math.Max(0, Math.Min(_jobOffer.OfferExpiresInSeconds, (int)timeSpan.TotalSeconds));
            
            // If timespan is negative or too large, use the original countdown value
            if (timeSpan.TotalSeconds < 0 || timeSpan.TotalSeconds > _jobOffer.OfferExpiresInSeconds + 10)
            {
                _secondsRemaining = _jobOffer.OfferExpiresInSeconds;
            }
            
            // Initialize timer (TEMPORARILY DISABLED FOR DEBUGGING)
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            // _countdownTimer.Tick += OnCountdownTick;  // DISABLED
            
            // Initialize commands
            System.Console.WriteLine($"Creating AcceptJobCommand...");
            AcceptJobCommand = new RelayCommand(ExecuteAcceptJob, CanExecuteAcceptJob);
            System.Console.WriteLine($"AcceptJobCommand created: {AcceptJobCommand != null}");
            
            System.Console.WriteLine($"Creating DeclineJobCommand...");
            DeclineJobCommand = new RelayCommand(ExecuteDeclineJob, CanExecuteDeclineJob);
            System.Console.WriteLine($"DeclineJobCommand created: {DeclineJobCommand != null}");
            
            // Start countdown (TEMPORARILY DISABLED)
            // _countdownTimer.Start();  // DISABLED
            
            System.Console.WriteLine($"JobOfferViewModel constructor completed successfully");
        }

        // ======================== PROPERTIES ========================

        /// <summary>
        /// The job offer data
        /// </summary>
        public JobOfferMessage JobOffer => _jobOffer;

        /// <summary>
        /// Seconds remaining for this offer
        /// </summary>
        public int SecondsRemaining
        {
            get => _secondsRemaining;
            set => SetProperty(ref _secondsRemaining, value);
        }

        /// <summary>
        /// Whether the offer has expired
        /// </summary>
        public bool IsExpired
        {
            get => _isExpired;
            set 
            {
                if (SetProperty(ref _isExpired, value))
                {
                    OnPropertyChanged(nameof(CanAcceptJob));
                    OnPropertyChanged(nameof(CanDeclineJob));
                }
            }
        }

        /// <summary>
        /// Whether the user has already responded to this offer
        /// </summary>
        public bool HasResponded
        {
            get => _hasResponded;
            set 
            {
                if (SetProperty(ref _hasResponded, value))
                {
                    OnPropertyChanged(nameof(CanAcceptJob));
                    OnPropertyChanged(nameof(CanDeclineJob));
                }
            }
        }

        /// <summary>
        /// Formatted countdown text
        /// </summary>
        public string CountdownText
        {
            get
            {
                if (IsExpired) return "EXPIRED";
                if (HasResponded) return "RESPONDED";
                
                var minutes = SecondsRemaining / 60;
                var seconds = SecondsRemaining % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        /// <summary>
        /// Countdown progress percentage (0-100)
        /// </summary>
        public double CountdownProgress
        {
            get
            {
                var totalSeconds = _jobOffer.OfferExpiresInSeconds;
                if (totalSeconds <= 0) return 0;
                
                var progress = (double)SecondsRemaining / totalSeconds * 100;
                return Math.Max(0, Math.Min(100, progress));
            }
        }

        /// <summary>
        /// Countdown bar color based on time remaining
        /// </summary>
        public string CountdownBarColor
        {
            get
            {
                if (IsExpired) return "#E74C3C"; // Red
                if (SecondsRemaining <= 10) return "#E67E22"; // Orange
                if (SecondsRemaining <= 30) return "#F39C12"; // Yellow
                return "#27AE60"; // Green
            }
        }

    /// <summary>
    /// Urgency indicator text
    /// </summary>
    public string UrgencyText
    {
        get
        {
            if (IsExpired) return "EXPIRED";
            if (SecondsRemaining <= 10) return "URGENT!";
            if (SecondsRemaining <= 30) return "Running out of time";
            return "Please respond quickly";
        }
    }
    
    /// <summary>
    /// Whether the accept job action can be executed
    /// </summary>
    public bool CanAcceptJob => !HasResponded && !IsExpired;
    
    /// <summary>
    /// Whether the decline job action can be executed
    /// </summary>
    public bool CanDeclineJob => !HasResponded && !IsExpired;

        /// <summary>
        /// Job details summary for display
        /// </summary>
        public string JobSummary => $"{_jobOffer.DisplayCustomer} • {_jobOffer.FileName}";

        /// <summary>
        /// Print specifications for display
        /// </summary>
        public string PrintSpecs => _jobOffer.PrintSpecs;

        /// <summary>
        /// Price information for display
        /// </summary>
        public string PriceDisplay => $"Customer pays: {_jobOffer.FormattedPrice} • You earn: {_jobOffer.FormattedEarnings}";

        /// <summary>
        /// Window title with job ID
        /// </summary>
        public string WindowTitle => $"Job Offer #{_jobOffer.JobId}";

        // ======================== COMMANDS ========================

        public ICommand AcceptJobCommand { get; }
        public ICommand DeclineJobCommand { get; }

        private void ExecuteAcceptJob()
        {
            System.Console.WriteLine($"=== SIMPLE ExecuteAcceptJob - JobId: {_jobOffer.JobId} ===");
            
            if (HasResponded || IsExpired) 
            {
                System.Console.WriteLine($"Already responded or expired - ignoring");
                return;
            }

            System.Console.WriteLine($"Setting HasResponded = true");
            HasResponded = true;
            
            System.Console.WriteLine($"Firing JobAccepted event");
            JobAccepted?.Invoke(this, new JobOfferResponseEventArgs(_jobOffer.JobId, "accept"));
            
            System.Console.WriteLine($"ExecuteAcceptJob completed successfully");
        }

        private bool CanExecuteAcceptJob()
        {
            var canExecute = !HasResponded && !IsExpired;
            System.Console.WriteLine($"CanExecuteAcceptJob called: HasResponded={HasResponded}, IsExpired={IsExpired}, Result={canExecute}");
            return canExecute;
        }

        private void ExecuteDeclineJob()
        {
            if (HasResponded || IsExpired) return;

            HasResponded = true;
            _countdownTimer.Stop();
            
            JobDeclined?.Invoke(this, new JobOfferResponseEventArgs(_jobOffer.JobId, "decline"));
        }

        private bool CanExecuteDeclineJob()
        {
            return !HasResponded && !IsExpired;
        }

        // ======================== PRIVATE METHODS ========================

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            try
            {
                System.Console.WriteLine($"=== TIMER TICK: Thread {System.Threading.Thread.CurrentThread.ManagedThreadId} ===");
                System.Console.WriteLine($"Is UI Thread: {System.Windows.Application.Current?.Dispatcher.CheckAccess()}");
                
                if (HasResponded)
                {
                    System.Console.WriteLine($"Timer stopped - HasResponded: {HasResponded}");
                    _countdownTimer.Stop();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"=== TIMER TICK EXCEPTION ===");
                System.Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Console.WriteLine($"Exception Message: {ex.Message}");
                System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Console.WriteLine($"=============================");
                throw;
            }

            SecondsRemaining--;
            
            // Update computed properties
            OnPropertyChanged(nameof(CountdownText));
            OnPropertyChanged(nameof(CountdownProgress));
            OnPropertyChanged(nameof(CountdownBarColor));
            OnPropertyChanged(nameof(UrgencyText));

            if (SecondsRemaining <= 0)
            {
                IsExpired = true;
                _countdownTimer.Stop();
                
                // Update computed properties after expiry
                OnPropertyChanged(nameof(CountdownText));
                OnPropertyChanged(nameof(CountdownProgress));
                OnPropertyChanged(nameof(CountdownBarColor));
                OnPropertyChanged(nameof(UrgencyText));
                OnPropertyChanged(nameof(CanAcceptJob));
                OnPropertyChanged(nameof(CanDeclineJob));
                
                JobExpired?.Invoke(this, new JobOfferResponseEventArgs(_jobOffer.JobId, "expired"));
            }
        }

        // ======================== CLEANUP ========================

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _countdownTimer?.Stop();
                    if (_countdownTimer != null)
                        _countdownTimer.Tick -= OnCountdownTick;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}