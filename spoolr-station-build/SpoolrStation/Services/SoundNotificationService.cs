using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service for playing notification sounds
    /// </summary>
    public class SoundNotificationService : IDisposable
    {
        private readonly ILogger<SoundNotificationService> _logger;
        private SoundPlayer? _jobOfferPlayer;
        private SoundPlayer? _successPlayer;
        private SoundPlayer? _errorPlayer;
        private bool _isEnabled = true;
        private bool _disposed = false;

        public SoundNotificationService(ILogger<SoundNotificationService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SoundNotificationService>.Instance;
            InitializeSounds();
        }

        /// <summary>
        /// Whether sound notifications are enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Play notification sound for new job offers
        /// </summary>
        public async Task PlayJobOfferNotificationAsync()
        {
            if (!_isEnabled) return;

            try
            {
                await Task.Run(() =>
                {
                    SystemSounds.Exclamation.Play();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to play job offer notification sound");
            }
        }

        /// <summary>
        /// Play success sound for completed actions
        /// </summary>
        public async Task PlaySuccessNotificationAsync()
        {
            if (!_isEnabled) return;

            try
            {
                await Task.Run(() =>
                {
                    SystemSounds.Asterisk.Play();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to play success notification sound");
            }
        }

        /// <summary>
        /// Play error sound for failures
        /// </summary>
        public async Task PlayErrorNotificationAsync()
        {
            if (!_isEnabled) return;

            try
            {
                await Task.Run(() =>
                {
                    SystemSounds.Hand.Play();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to play error notification sound");
            }
        }

        /// <summary>
        /// Play system notification sound
        /// </summary>
        public void PlaySystemNotification()
        {
            if (!_isEnabled) return;

            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to play system notification sound");
            }
        }

        private void InitializeSounds()
        {
            try
            {
                // Use system sounds as defaults
                // These can be replaced with custom WAV files in the future
                
                // For simplicity, we'll just use system sounds directly in the play methods
                // rather than trying to load them as streams
                _jobOfferPlayer = new SoundPlayer();
                _successPlayer = new SoundPlayer();
                _errorPlayer = new SoundPlayer();

                _logger.LogInformation("Sound notification service initialized");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize sound notification service");
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _jobOfferPlayer?.Dispose();
                    _successPlayer?.Dispose();
                    _errorPlayer?.Dispose();
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