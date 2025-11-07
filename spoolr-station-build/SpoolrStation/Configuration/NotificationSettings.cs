using System.ComponentModel;

namespace SpoolrStation.Configuration
{
    /// <summary>
    /// Configuration settings for notifications and window focus behavior
    /// </summary>
    public class NotificationSettings : INotifyPropertyChanged
    {
        private bool _enableSystemTrayNotifications = true;
        private bool _enableBalloonNotifications = true;
        private bool _autoFocusOnUrgentOffers = true;
        private bool _flashTaskbarOnNewOffer = true;
        private bool _temporaryTopMostForUrgentOffers = false;
        private bool _minimizeToTrayOnClose = true;
        private bool _showTrayNotificationOnMinimize = true;
        private int _balloonNotificationDuration = 5000;
        private int _taskbarFlashCount = 3;
        private int _temporaryTopMostDurationSeconds = 10;

        /// <summary>
        /// Enable system tray integration and notifications
        /// </summary>
        public bool EnableSystemTrayNotifications
        {
            get => _enableSystemTrayNotifications;
            set => SetProperty(ref _enableSystemTrayNotifications, value);
        }

        /// <summary>
        /// Enable balloon tip notifications in system tray
        /// </summary>
        public bool EnableBalloonNotifications
        {
            get => _enableBalloonNotifications;
            set => SetProperty(ref _enableBalloonNotifications, value);
        }

        /// <summary>
        /// Automatically bring window to front for urgent job offers
        /// </summary>
        public bool AutoFocusOnUrgentOffers
        {
            get => _autoFocusOnUrgentOffers;
            set => SetProperty(ref _autoFocusOnUrgentOffers, value);
        }

        /// <summary>
        /// Flash taskbar icon when new job offers arrive
        /// </summary>
        public bool FlashTaskbarOnNewOffer
        {
            get => _flashTaskbarOnNewOffer;
            set => SetProperty(ref _flashTaskbarOnNewOffer, value);
        }

        /// <summary>
        /// Temporarily set window to top-most for urgent offers
        /// </summary>
        public bool TemporaryTopMostForUrgentOffers
        {
            get => _temporaryTopMostForUrgentOffers;
            set => SetProperty(ref _temporaryTopMostForUrgentOffers, value);
        }

        /// <summary>
        /// Minimize to system tray instead of closing when X button is clicked
        /// </summary>
        public bool MinimizeToTrayOnClose
        {
            get => _minimizeToTrayOnClose;
            set => SetProperty(ref _minimizeToTrayOnClose, value);
        }

        /// <summary>
        /// Show notification when application is minimized to tray
        /// </summary>
        public bool ShowTrayNotificationOnMinimize
        {
            get => _showTrayNotificationOnMinimize;
            set => SetProperty(ref _showTrayNotificationOnMinimize, value);
        }

        /// <summary>
        /// Duration for balloon notifications (milliseconds)
        /// </summary>
        public int BalloonNotificationDuration
        {
            get => _balloonNotificationDuration;
            set => SetProperty(ref _balloonNotificationDuration, Math.Max(1000, Math.Min(30000, value)));
        }

        /// <summary>
        /// Number of times to flash taskbar for attention
        /// </summary>
        public int TaskbarFlashCount
        {
            get => _taskbarFlashCount;
            set => SetProperty(ref _taskbarFlashCount, Math.Max(1, Math.Min(10, value)));
        }

        /// <summary>
        /// Duration to keep window as top-most for urgent offers (seconds)
        /// </summary>
        public int TemporaryTopMostDurationSeconds
        {
            get => _temporaryTopMostDurationSeconds;
            set => SetProperty(ref _temporaryTopMostDurationSeconds, Math.Max(1, Math.Min(60, value)));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}