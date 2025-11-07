using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using SpoolrStation.Configuration;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Handles audio notifications for job offers and system events
    /// </summary>
    public class AudioNotificationService : IDisposable
    {
        private readonly ILogger<AudioNotificationService> _logger;
        private readonly AppSettings _settings;
        private SoundPlayer? _soundPlayer;
        private bool _disposed;

        // Default system sounds
        private static readonly string[] DefaultSounds = new[]
        {
            "SystemNotification",
            "SystemExclamation", 
            "SystemAsterisk",
            "SystemQuestion"
        };

        public AudioNotificationService(ILogger<AudioNotificationService>? logger = null, AppSettings? settings = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AudioNotificationService>.Instance;
            _settings = settings ?? new AppSettings(); // Will use defaults if null
        }

        /// <summary>
        /// Play notification sound for new job offers
        /// </summary>
        public async Task PlayJobOfferSoundAsync()
        {
            if (!_settings.AudioSettings.EnableJobOfferSounds)
            {
                _logger.LogDebug("Job offer sounds are disabled");
                return;
            }

            try
            {
                var soundFile = _settings.AudioSettings.JobOfferSoundFile;
                var volume = _settings.AudioSettings.Volume;

                await PlaySoundAsync(soundFile, volume, "job offer");
                
                _logger.LogDebug("Job offer sound played successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play job offer sound");
            }
        }

        /// <summary>
        /// Play notification sound for connection status changes
        /// </summary>
        public async Task PlayConnectionSoundAsync(bool isConnected)
        {
            if (!_settings.AudioSettings.EnableConnectionSounds)
            {
                _logger.LogDebug("Connection sounds are disabled");
                return;
            }

            try
            {
                var soundFile = isConnected 
                    ? _settings.AudioSettings.ConnectionEstablishedSoundFile 
                    : _settings.AudioSettings.ConnectionLostSoundFile;
                    
                var volume = _settings.AudioSettings.Volume;

                await PlaySoundAsync(soundFile, volume, isConnected ? "connection established" : "connection lost");
                
                _logger.LogDebug("Connection sound played successfully for {Status}", isConnected ? "connected" : "disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play connection sound for {Status}", isConnected ? "connected" : "disconnected");
            }
        }

        /// <summary>
        /// Play notification sound for job acceptance/rejection
        /// </summary>
        public async Task PlayJobActionSoundAsync(bool isAccepted)
        {
            if (!_settings.AudioSettings.EnableJobActionSounds)
            {
                _logger.LogDebug("Job action sounds are disabled");
                return;
            }

            try
            {
                var soundFile = isAccepted 
                    ? _settings.AudioSettings.JobAcceptedSoundFile 
                    : _settings.AudioSettings.JobRejectedSoundFile;
                    
                var volume = _settings.AudioSettings.Volume;

                await PlaySoundAsync(soundFile, volume, isAccepted ? "job accepted" : "job rejected");
                
                _logger.LogDebug("Job action sound played successfully for {Action}", isAccepted ? "accepted" : "rejected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play job action sound for {Action}", isAccepted ? "accepted" : "rejected");
            }
        }

        /// <summary>
        /// Play a general error notification sound
        /// </summary>
        public async Task PlayErrorSoundAsync()
        {
            if (!_settings.AudioSettings.EnableErrorSounds)
            {
                _logger.LogDebug("Error sounds are disabled");
                return;
            }

            try
            {
                var soundFile = _settings.AudioSettings.ErrorSoundFile;
                var volume = _settings.AudioSettings.Volume;

                await PlaySoundAsync(soundFile, volume, "error");
                
                _logger.LogDebug("Error sound played successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play error sound");
            }
        }

        /// <summary>
        /// Test a sound file to validate it works
        /// </summary>
        public async Task<bool> TestSoundAsync(string soundFile, double volume = 0.5)
        {
            try
            {
                await PlaySoundAsync(soundFile, volume, "test");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sound test failed for {SoundFile}", soundFile);
                return false;
            }
        }

        /// <summary>
        /// Core method to play a sound file or system sound
        /// </summary>
        private async Task PlaySoundAsync(string soundFile, double volume, string context)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Dispose previous player if exists
                    _soundPlayer?.Stop();
                    _soundPlayer?.Dispose();

                    if (string.IsNullOrWhiteSpace(soundFile) || soundFile == "None")
                    {
                        _logger.LogDebug("No sound configured for {Context}", context);
                        return;
                    }

                    // Check if it's a system sound
                    if (IsSystemSound(soundFile))
                    {
                        PlaySystemSound(soundFile);
                        return;
                    }

                    // Check if it's a file path
                    if (File.Exists(soundFile))
                    {
                        PlaySoundFile(soundFile, volume);
                        return;
                    }

                    // Try to find the file in common locations
                    var resolvedPath = ResolveSoundFilePath(soundFile);
                    if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                    {
                        PlaySoundFile(resolvedPath, volume);
                        return;
                    }

                    // Fall back to default system sound
                    _logger.LogWarning("Sound file not found: {SoundFile}, using default system sound for {Context}", soundFile, context);
                    PlaySystemSound("SystemNotification");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error playing sound for {Context}", context);
                    
                    // Last resort - try system beep
                    try
                    {
                        SystemSounds.Beep.Play();
                    }
                    catch
                    {
                        // If even system beep fails, just log and continue
                        _logger.LogError("Even system beep failed for {Context}", context);
                    }
                }
            });
        }

        /// <summary>
        /// Play a system sound by name
        /// </summary>
        private void PlaySystemSound(string systemSoundName)
        {
            var systemSound = systemSoundName.ToLowerInvariant() switch
            {
                "systemnotification" or "notification" => SystemSounds.Asterisk,
                "systemexclamation" or "exclamation" => SystemSounds.Exclamation,
                "systemasterisk" or "asterisk" => SystemSounds.Asterisk,
                "systemquestion" or "question" => SystemSounds.Question,
                "systembeep" or "beep" => SystemSounds.Beep,
                "systemhand" or "hand" or "error" => SystemSounds.Hand,
                _ => SystemSounds.Asterisk
            };

            systemSound.Play();
            _logger.LogDebug("Played system sound: {SystemSoundName}", systemSoundName);
        }

        /// <summary>
        /// Play a sound file
        /// </summary>
        private void PlaySoundFile(string filePath, double volume)
        {
            _soundPlayer = new SoundPlayer(filePath);
            
            // Note: SoundPlayer doesn't support volume control directly
            // For volume control, you would need to use more advanced audio libraries
            // like NAudio or implement Windows multimedia API calls
            
            _soundPlayer.Play();
            _logger.LogDebug("Played sound file: {FilePath}", filePath);
        }

        /// <summary>
        /// Check if the sound name refers to a system sound
        /// </summary>
        private static bool IsSystemSound(string soundName)
        {
            return soundName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                   Array.Exists(DefaultSounds, s => s.Equals(soundName, StringComparison.OrdinalIgnoreCase)) ||
                   soundName.Equals("beep", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("notification", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("exclamation", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("asterisk", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("question", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("hand", StringComparison.OrdinalIgnoreCase) ||
                   soundName.Equals("error", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolve sound file path by checking common locations
        /// </summary>
        private string? ResolveSoundFilePath(string soundFile)
        {
            try
            {
                // If it's already an absolute path, return as-is
                if (Path.IsPathRooted(soundFile))
                {
                    return soundFile;
                }

                // Check common locations
                var searchPaths = new[]
                {
                    // Application directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, soundFile),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", soundFile),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", soundFile),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", soundFile),
                    
                    // User's Documents folder
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spoolr Station", soundFile),
                    
                    // Windows system sounds folder
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", soundFile)
                };

                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving sound file path for {SoundFile}", soundFile);
                return null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                try
                {
                    _soundPlayer?.Stop();
                    _soundPlayer?.Dispose();
                    _soundPlayer = null;

                    _logger.LogInformation("Audio notification service disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing audio notification service");
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}