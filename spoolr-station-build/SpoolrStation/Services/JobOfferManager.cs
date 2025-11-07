using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpoolrStation.WebSocket.Models;
using SpoolrStation.Views;
using SpoolrStation.ViewModels.JobOffers;
using System.Windows;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Manages multiple concurrent job offers with priority handling and UI coordination
    /// </summary>
    public class JobOfferManager : IDisposable
    {
        private readonly ILogger<JobOfferManager> _logger;
        private readonly ConcurrentDictionary<long, ActiveJobOffer> _activeOffers = new();
        private readonly System.Threading.Timer _expirationTimer;
        private bool _disposed;

        public event EventHandler<JobOfferAcceptedEventArgs>? JobOfferAccepted;
        public event EventHandler<JobOfferDeclinedEventArgs>? JobOfferDeclined;
        public event EventHandler<JobOfferExpiredEventArgs>? JobOfferExpired;

        public JobOfferManager(ILogger<JobOfferManager>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<JobOfferManager>.Instance;
            
            // Check for expired offers every 5 seconds
            _expirationTimer = new System.Threading.Timer(CheckExpiredOffers, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            _logger.LogInformation("JobOfferManager initialized");
        }

        /// <summary>
        /// Number of currently active job offers
        /// </summary>
        public int ActiveOffersCount => _activeOffers.Count;

        /// <summary>
        /// Get all active job offers
        /// </summary>
        public IReadOnlyCollection<JobOfferMessage> ActiveOffers => 
            _activeOffers.Values.Select(ao => ao.JobOffer).ToList();

        /// <summary>
        /// Add a new job offer to the management system
        /// </summary>
        public async Task<bool> AddJobOfferAsync(JobOfferMessage jobOffer)
        {
            if (_disposed) return false;

            try
            {
                var activeOffer = new ActiveJobOffer(jobOffer, DateTime.UtcNow.AddSeconds(jobOffer.OfferExpiresInSeconds));
                
                if (!_activeOffers.TryAdd(jobOffer.JobId, activeOffer))
                {
                    _logger.LogWarning("Job offer {JobId} already exists, ignoring duplicate", jobOffer.JobId);
                    return false;
                }

                _logger.LogInformation("Added job offer {JobId} from {Customer} (Value: {Price:C}, Priority: {Priority})", 
                    jobOffer.JobId, jobOffer.DisplayCustomer, jobOffer.TotalPrice, CalculatePriority(jobOffer));

                // Show job offer popup on UI thread
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    ShowJobOfferPopup(activeOffer);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add job offer {JobId}", jobOffer.JobId);
                return false;
            }
        }

        /// <summary>
        /// Accept a job offer
        /// </summary>
        public Task<bool> AcceptJobOfferAsync(long jobId, string response = "accept")
        {
            if (_activeOffers.TryRemove(jobId, out var activeOffer))
            {
                activeOffer.Status = JobOfferStatus.Accepted;
                activeOffer.Window?.Close();

                _logger.LogInformation("Job offer {JobId} accepted", jobId);
                
                JobOfferAccepted?.Invoke(this, new JobOfferAcceptedEventArgs(activeOffer.JobOffer, response));
                return Task.FromResult(true);
            }

            _logger.LogWarning("Attempted to accept non-existent job offer {JobId}", jobId);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Decline a job offer
        /// </summary>
        public Task<bool> DeclineJobOfferAsync(long jobId, string response = "decline")
        {
            if (_activeOffers.TryRemove(jobId, out var activeOffer))
            {
                activeOffer.Status = JobOfferStatus.Declined;
                activeOffer.Window?.Close();

                _logger.LogInformation("Job offer {JobId} declined", jobId);
                
                JobOfferDeclined?.Invoke(this, new JobOfferDeclinedEventArgs(jobId, response));
                return Task.FromResult(true);
            }

            _logger.LogWarning("Attempted to decline non-existent job offer {JobId}", jobId);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Cancel a job offer (called when backend cancels)
        /// </summary>
        public async Task<bool> CancelJobOfferAsync(long jobId, string reason)
        {
            if (_activeOffers.TryRemove(jobId, out var activeOffer))
            {
                activeOffer.Status = JobOfferStatus.Cancelled;
                
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    activeOffer.Window?.Close();
                });

                _logger.LogInformation("Job offer {JobId} cancelled: {Reason}", jobId, reason);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get job offer by ID
        /// </summary>
        public JobOfferMessage? GetJobOffer(long jobId)
        {
            return _activeOffers.TryGetValue(jobId, out var activeOffer) ? activeOffer.JobOffer : null;
        }

        /// <summary>
        /// Check if a job offer exists and is still active
        /// </summary>
        public bool IsJobOfferActive(long jobId)
        {
            return _activeOffers.ContainsKey(jobId);
        }

        /// <summary>
        /// Calculate priority score for job offer (higher = more urgent)
        /// </summary>
        private int CalculatePriority(JobOfferMessage jobOffer)
        {
            var priority = 0;

            // Price-based priority (higher value jobs get higher priority)
            if (jobOffer.TotalPrice >= 50) priority += 30;
            else if (jobOffer.TotalPrice >= 20) priority += 20;
            else if (jobOffer.TotalPrice >= 10) priority += 10;

            // Urgency-based priority (shorter expiration time gets higher priority)
            if (jobOffer.OfferExpiresInSeconds <= 30) priority += 20;
            else if (jobOffer.OfferExpiresInSeconds <= 60) priority += 10;

            // Customer type priority (non-anonymous gets slight boost)
            if (!jobOffer.IsAnonymous) priority += 5;

            return priority;
        }

        /// <summary>
        /// Show job offer popup with proper positioning for multiple offers
        /// </summary>
        private void ShowJobOfferPopup(ActiveJobOffer activeOffer)
        {
            try
            {
                var popup = new JobOfferWindow(activeOffer.JobOffer);
                activeOffer.Window = popup;

                // Position multiple popups in a staggered pattern
                var existingWindows = _activeOffers.Values.Count(ao => ao.Window?.IsVisible == true);
                var offsetX = existingWindows * 20;
                var offsetY = existingWindows * 20;

                popup.Left = SystemParameters.PrimaryScreenWidth / 2 - popup.Width / 2 + offsetX;
                popup.Top = SystemParameters.PrimaryScreenHeight / 2 - popup.Height / 2 + offsetY;

                // Wire up events
                if (popup.DataContext is JobOfferViewModel viewModel)
                {
                    viewModel.JobAccepted += async (sender, e) => await AcceptJobOfferAsync(e.JobId, e.Response);
                    viewModel.JobDeclined += async (sender, e) => await DeclineJobOfferAsync(e.JobId, e.Response);
                    viewModel.JobExpired += async (sender, e) => await ExpireJobOfferAsync(e.JobId);
                }

                popup.Show();
                popup.Activate(); // Bring to front

                _logger.LogDebug("Job offer popup displayed for {JobId} at position ({X}, {Y})", 
                    activeOffer.JobOffer.JobId, popup.Left, popup.Top);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show job offer popup for {JobId}", activeOffer.JobOffer.JobId);
            }
        }

        /// <summary>
        /// Expire a job offer
        /// </summary>
        private async Task<bool> ExpireJobOfferAsync(long jobId)
        {
            if (_activeOffers.TryRemove(jobId, out var activeOffer))
            {
                activeOffer.Status = JobOfferStatus.Expired;
                
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    activeOffer.Window?.Close();
                });

                _logger.LogInformation("Job offer {JobId} expired", jobId);
                
                JobOfferExpired?.Invoke(this, new JobOfferExpiredEventArgs(jobId));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check for and handle expired offers
        /// </summary>
        private async void CheckExpiredOffers(object? state)
        {
            if (_disposed) return;

            try
            {
                var now = DateTime.UtcNow;
                var expiredOffers = _activeOffers.Values
                    .Where(offer => offer.ExpiresAt <= now && offer.Status == JobOfferStatus.Active)
                    .ToList();

                foreach (var expiredOffer in expiredOffers)
                {
                    await ExpireJobOfferAsync(expiredOffer.JobOffer.JobId);
                }

                if (expiredOffers.Count > 0)
                {
                    _logger.LogInformation("Processed {Count} expired job offers", expiredOffers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for expired offers");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // Stop expiration timer
                _expirationTimer?.Dispose();

                // Close all active popups
                foreach (var activeOffer in _activeOffers.Values)
                {
                    try
                    {
                        WpfApplication.Current?.Dispatcher?.Invoke(() =>
                        {
                            activeOffer.Window?.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing job offer window during dispose");
                    }
                }

                _activeOffers.Clear();
                
                _logger.LogInformation("JobOfferManager disposed");
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents an active job offer being managed
    /// </summary>
    public class ActiveJobOffer
    {
        public JobOfferMessage JobOffer { get; }
        public DateTime ExpiresAt { get; }
        public JobOfferStatus Status { get; set; }
        public JobOfferWindow? Window { get; set; }

        public ActiveJobOffer(JobOfferMessage jobOffer, DateTime expiresAt)
        {
            JobOffer = jobOffer;
            ExpiresAt = expiresAt;
            Status = JobOfferStatus.Active;
        }
    }

    /// <summary>
    /// Status of a job offer in the management system
    /// </summary>
    public enum JobOfferStatus
    {
        Active,
        Accepted,
        Declined,
        Expired,
        Cancelled
    }

    // Event argument classes
    public class JobOfferAcceptedEventArgs : EventArgs
    {
        public JobOfferMessage JobOffer { get; }
        public string Response { get; }

        public JobOfferAcceptedEventArgs(JobOfferMessage jobOffer, string response)
        {
            JobOffer = jobOffer;
            Response = response;
        }
    }

    public class JobOfferDeclinedEventArgs : EventArgs
    {
        public long JobId { get; }
        public string Response { get; }

        public JobOfferDeclinedEventArgs(long jobId, string response)
        {
            JobId = jobId;
            Response = response;
        }
    }

    public class JobOfferExpiredEventArgs : EventArgs
    {
        public long JobId { get; }

        public JobOfferExpiredEventArgs(long jobId)
        {
            JobId = jobId;
        }
    }
}
