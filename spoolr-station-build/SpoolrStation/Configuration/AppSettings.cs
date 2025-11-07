using System.ComponentModel;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;

namespace SpoolrStation.Configuration
{
    /// <summary>
    /// Main application settings class that aggregates all configuration sections
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        private static readonly string SettingsFileName = "appsettings.json";
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Spoolr Station",
            SettingsFileName
        );

        private NotificationSettings _notificationSettings = new();
        private AudioSettings _audioSettings = new();
        private ConnectionSettings _connectionSettings = new();
        private bool _isLoaded = false;

        public AppSettings()
        {
            // Wire up property changed events from child settings
            _notificationSettings.PropertyChanged += OnChildSettingsChanged;
            _audioSettings.PropertyChanged += OnChildSettingsChanged;
            _connectionSettings.PropertyChanged += OnChildSettingsChanged;
        }

        /// <summary>
        /// Notification and window focus settings
        /// </summary>
        public NotificationSettings NotificationSettings
        {
            get => _notificationSettings;
            set => SetProperty(ref _notificationSettings, value);
        }

        /// <summary>
        /// Audio notification settings
        /// </summary>
        public AudioSettings AudioSettings
        {
            get => _audioSettings;
            set => SetProperty(ref _audioSettings, value);
        }

        /// <summary>
        /// Connection and WebSocket settings
        /// </summary>
        public ConnectionSettings ConnectionSettings
        {
            get => _connectionSettings;
            set => SetProperty(ref _connectionSettings, value);
        }

        /// <summary>
        /// Whether settings have been loaded from file
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Load settings from file, creating default settings if file doesn't exist
        /// </summary>
        public async Task<bool> LoadAsync(ILogger? logger = null)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Load from file if exists
                if (File.Exists(SettingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, GetJsonOptions());

                    if (settings != null)
                    {
                        CopyFrom(settings);
                        _isLoaded = true;
                        
                        logger?.LogInformation("Settings loaded from {SettingsPath}", SettingsFilePath);
                        return true;
                    }
                }

                // If no file exists or loading failed, use defaults and save
                _isLoaded = true;
                await SaveAsync(logger);
                
                logger?.LogInformation("Default settings created at {SettingsPath}", SettingsFilePath);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load settings from {SettingsPath}", SettingsFilePath);
                
                // Use defaults even if loading failed
                _isLoaded = true;
                return false;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public async Task<bool> SaveAsync(ILogger? logger = null)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, GetJsonOptions());
                await File.WriteAllTextAsync(SettingsFilePath, json);
                
                logger?.LogDebug("Settings saved to {SettingsPath}", SettingsFilePath);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to save settings to {SettingsPath}", SettingsFilePath);
                return false;
            }
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            NotificationSettings = new NotificationSettings();
            AudioSettings = new AudioSettings();
            ConnectionSettings = new ConnectionSettings();
            
            OnPropertyChanged(nameof(NotificationSettings));
            OnPropertyChanged(nameof(AudioSettings));
            OnPropertyChanged(nameof(ConnectionSettings));
        }

        /// <summary>
        /// Copy settings from another instance
        /// </summary>
        private void CopyFrom(AppSettings other)
        {
            // Create new instances to avoid reference sharing
            NotificationSettings = JsonSerializer.Deserialize<NotificationSettings>(
                JsonSerializer.Serialize(other.NotificationSettings, GetJsonOptions()), 
                GetJsonOptions()) ?? new NotificationSettings();
                
            AudioSettings = JsonSerializer.Deserialize<AudioSettings>(
                JsonSerializer.Serialize(other.AudioSettings, GetJsonOptions()), 
                GetJsonOptions()) ?? new AudioSettings();
                
            ConnectionSettings = JsonSerializer.Deserialize<ConnectionSettings>(
                JsonSerializer.Serialize(other.ConnectionSettings, GetJsonOptions()), 
                GetJsonOptions()) ?? new ConnectionSettings();

            // Wire up events for new instances
            _notificationSettings.PropertyChanged += OnChildSettingsChanged;
            _audioSettings.PropertyChanged += OnChildSettingsChanged;
            _connectionSettings.PropertyChanged += OnChildSettingsChanged;
        }

        /// <summary>
        /// Get JSON serialization options
        /// </summary>
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
            };
        }

        /// <summary>
        /// Handle property changes from child settings objects
        /// </summary>
        private void OnChildSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Auto-save when any child settings change (debounced)
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Simple debounce
                await SaveAsync();
            });
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

    /// <summary>
    /// Settings for connection and WebSocket configuration
    /// </summary>
    public class ConnectionSettings : INotifyPropertyChanged
    {
        private int _reconnectAttempts = 5;
        private int _reconnectDelaySeconds = 5;
        private int _heartbeatIntervalSeconds = 30;
        private int _connectionTimeoutSeconds = 30;
        private bool _enableAutoReconnect = true;

        public int ReconnectAttempts
        {
            get => _reconnectAttempts;
            set => SetProperty(ref _reconnectAttempts, Math.Max(1, Math.Min(20, value)));
        }

        public int ReconnectDelaySeconds
        {
            get => _reconnectDelaySeconds;
            set => SetProperty(ref _reconnectDelaySeconds, Math.Max(1, Math.Min(300, value)));
        }

        public int HeartbeatIntervalSeconds
        {
            get => _heartbeatIntervalSeconds;
            set => SetProperty(ref _heartbeatIntervalSeconds, Math.Max(10, Math.Min(300, value)));
        }

        public int ConnectionTimeoutSeconds
        {
            get => _connectionTimeoutSeconds;
            set => SetProperty(ref _connectionTimeoutSeconds, Math.Max(5, Math.Min(120, value)));
        }

        public bool EnableAutoReconnect
        {
            get => _enableAutoReconnect;
            set => SetProperty(ref _enableAutoReconnect, value);
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