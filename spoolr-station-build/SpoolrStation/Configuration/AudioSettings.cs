using System.ComponentModel;

namespace SpoolrStation.Configuration
{
    /// <summary>
    /// Configuration settings for audio notifications and sound alerts
    /// </summary>
    public class AudioSettings : INotifyPropertyChanged
    {
        private bool _enableJobOfferSounds = true;
        private bool _enableConnectionSounds = true;
        private bool _enableJobActionSounds = true;
        private bool _enableErrorSounds = true;
        private double _volume = 0.7;
        private string _jobOfferSoundFile = "SystemNotification";
        private string _connectionEstablishedSoundFile = "SystemAsterisk";
        private string _connectionLostSoundFile = "SystemExclamation";
        private string _jobAcceptedSoundFile = "SystemAsterisk";
        private string _jobRejectedSoundFile = "SystemHand";
        private string _errorSoundFile = "SystemHand";

        /// <summary>
        /// Enable sound notifications for new job offers
        /// </summary>
        public bool EnableJobOfferSounds
        {
            get => _enableJobOfferSounds;
            set => SetProperty(ref _enableJobOfferSounds, value);
        }

        /// <summary>
        /// Enable sound notifications for connection status changes
        /// </summary>
        public bool EnableConnectionSounds
        {
            get => _enableConnectionSounds;
            set => SetProperty(ref _enableConnectionSounds, value);
        }

        /// <summary>
        /// Enable sound notifications for job acceptance/rejection
        /// </summary>
        public bool EnableJobActionSounds
        {
            get => _enableJobActionSounds;
            set => SetProperty(ref _enableJobActionSounds, value);
        }

        /// <summary>
        /// Enable sound notifications for errors
        /// </summary>
        public bool EnableErrorSounds
        {
            get => _enableErrorSounds;
            set => SetProperty(ref _enableErrorSounds, value);
        }

        /// <summary>
        /// Master volume for all notification sounds (0.0 to 1.0)
        /// </summary>
        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, Math.Max(0.0, Math.Min(1.0, value)));
        }

        /// <summary>
        /// Sound file or system sound name for job offer notifications
        /// </summary>
        public string JobOfferSoundFile
        {
            get => _jobOfferSoundFile;
            set => SetProperty(ref _jobOfferSoundFile, value ?? "SystemNotification");
        }

        /// <summary>
        /// Sound file or system sound name for connection established notifications
        /// </summary>
        public string ConnectionEstablishedSoundFile
        {
            get => _connectionEstablishedSoundFile;
            set => SetProperty(ref _connectionEstablishedSoundFile, value ?? "SystemAsterisk");
        }

        /// <summary>
        /// Sound file or system sound name for connection lost notifications
        /// </summary>
        public string ConnectionLostSoundFile
        {
            get => _connectionLostSoundFile;
            set => SetProperty(ref _connectionLostSoundFile, value ?? "SystemExclamation");
        }

        /// <summary>
        /// Sound file or system sound name for job accepted notifications
        /// </summary>
        public string JobAcceptedSoundFile
        {
            get => _jobAcceptedSoundFile;
            set => SetProperty(ref _jobAcceptedSoundFile, value ?? "SystemAsterisk");
        }

        /// <summary>
        /// Sound file or system sound name for job rejected notifications
        /// </summary>
        public string JobRejectedSoundFile
        {
            get => _jobRejectedSoundFile;
            set => SetProperty(ref _jobRejectedSoundFile, value ?? "SystemHand");
        }

        /// <summary>
        /// Sound file or system sound name for error notifications
        /// </summary>
        public string ErrorSoundFile
        {
            get => _errorSoundFile;
            set => SetProperty(ref _errorSoundFile, value ?? "SystemHand");
        }

        /// <summary>
        /// Available system sound options
        /// </summary>
        public static readonly string[] SystemSoundOptions = new[]
        {
            "None",
            "SystemNotification",
            "SystemExclamation",
            "SystemAsterisk", 
            "SystemQuestion",
            "SystemHand",
            "SystemBeep"
        };

        /// <summary>
        /// Supported audio file extensions
        /// </summary>
        public static readonly string[] SupportedAudioExtensions = new[]
        {
            ".wav",
            ".mp3", // Note: Limited support without additional libraries
            ".wma"  // Note: Limited support without additional libraries
        };

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