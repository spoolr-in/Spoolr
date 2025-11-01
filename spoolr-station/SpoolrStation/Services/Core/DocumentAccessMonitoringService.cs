using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpoolrStation.Services.Core
{
    /// <summary>
    /// Service for monitoring and tracking document access patterns, authorization failures, and performance metrics
    /// Helps diagnose and resolve document preview authorization issues
    /// </summary>
    public class DocumentAccessMonitoringService
    {
        private readonly ILogger<DocumentAccessMonitoringService> _logger;
        private readonly Dictionary<long, DocumentAccessAttempt> _recentAttempts;
        private readonly Dictionary<string, TokenUsageMetrics> _tokenMetrics;
        private readonly ReaderWriterLockSlim _lock;
        private const int MaxRecentAttempts = 1000;
        private const int MetricsRetentionHours = 24;

        public DocumentAccessMonitoringService(ILogger<DocumentAccessMonitoringService> logger)
        {
            _logger = logger;
            _recentAttempts = new Dictionary<long, DocumentAccessAttempt>();
            _tokenMetrics = new Dictionary<string, TokenUsageMetrics>();
            _lock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Record a document access attempt for monitoring and analysis
        /// </summary>
        public void RecordDocumentAccessAttempt(long jobId, string correlationId, string tokenHash, 
            int vendorId, string operation, bool preFlightCheck = false)
        {
            _lock.EnterWriteLock();
            try
            {
                var attempt = new DocumentAccessAttempt
                {
                    JobId = jobId,
                    CorrelationId = correlationId,
                    TokenHash = tokenHash,
                    VendorId = vendorId,
                    Operation = operation,
                    AttemptTime = DateTime.UtcNow,
                    IsPreFlightCheck = preFlightCheck
                };

                _recentAttempts[jobId] = attempt;

                // Update token metrics
                if (!_tokenMetrics.ContainsKey(tokenHash))
                {
                    _tokenMetrics[tokenHash] = new TokenUsageMetrics { TokenHash = tokenHash };
                }
                _tokenMetrics[tokenHash].TotalAttempts++;

                // Clean up old entries periodically
                if (_recentAttempts.Count > MaxRecentAttempts)
                {
                    CleanupOldEntries();
                }

                _logger.LogInformation("Document access attempt recorded: JobId={JobId}, CorrelationId={CorrelationId}, " +
                    "Operation={Operation}, PreFlight={PreFlight}, Token={TokenHash}",
                    jobId, correlationId, operation, preFlightCheck, tokenHash.Substring(0, 8) + "...");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Record a successful document access
        /// </summary>
        public void RecordSuccessfulAccess(long jobId, string correlationId, TimeSpan responseTime)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_recentAttempts.TryGetValue(jobId, out var attempt))
                {
                    attempt.IsSuccessful = true;
                    attempt.ResponseTime = responseTime;
                    attempt.CompletionTime = DateTime.UtcNow;

                    // Update token metrics
                    if (_tokenMetrics.TryGetValue(attempt.TokenHash, out var tokenMetrics))
                    {
                        tokenMetrics.SuccessfulAttempts++;
                        tokenMetrics.TotalResponseTime += responseTime;
                    }

                    _logger.LogInformation("Document access successful: JobId={JobId}, CorrelationId={CorrelationId}, " +
                        "ResponseTime={ResponseTimeMs}ms",
                        jobId, correlationId, responseTime.TotalMilliseconds);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Record a failed document access attempt
        /// </summary>
        public void RecordFailedAccess(long jobId, string correlationId, string errorType, string errorMessage, 
            int? httpStatusCode = null)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_recentAttempts.TryGetValue(jobId, out var attempt))
                {
                    attempt.IsSuccessful = false;
                    attempt.ErrorType = errorType;
                    attempt.ErrorMessage = errorMessage;
                    attempt.HttpStatusCode = httpStatusCode;
                    attempt.CompletionTime = DateTime.UtcNow;

                    // Update token metrics
                    if (_tokenMetrics.TryGetValue(attempt.TokenHash, out var tokenMetrics))
                    {
                        tokenMetrics.FailedAttempts++;
                        
                        if (httpStatusCode == 403)
                        {
                            tokenMetrics.AuthorizationFailures++;
                        }
                        else if (httpStatusCode >= 500)
                        {
                            tokenMetrics.ServerErrors++;
                        }
                    }

                    _logger.LogWarning("Document access failed: JobId={JobId}, CorrelationId={CorrelationId}, " +
                        "ErrorType={ErrorType}, StatusCode={StatusCode}, Message={Message}",
                        jobId, correlationId, errorType, httpStatusCode, errorMessage);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get recent authorization failure patterns for analysis
        /// </summary>
        public List<DocumentAccessAttempt> GetRecentAuthorizationFailures(TimeSpan? lookbackPeriod = null)
        {
            var cutoff = DateTime.UtcNow - (lookbackPeriod ?? TimeSpan.FromHours(1));
            
            _lock.EnterReadLock();
            try
            {
                return _recentAttempts.Values
                    .Where(a => a.AttemptTime >= cutoff && 
                               !a.IsSuccessful && 
                               a.HttpStatusCode == 403)
                    .OrderByDescending(a => a.AttemptTime)
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get token usage metrics for analysis
        /// </summary>
        public List<TokenUsageMetrics> GetTokenMetrics()
        {
            _lock.EnterReadLock();
            try
            {
                return _tokenMetrics.Values.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get comprehensive monitoring summary
        /// </summary>
        public DocumentAccessMonitoringSummary GetMonitoringSummary(TimeSpan? lookbackPeriod = null)
        {
            var cutoff = DateTime.UtcNow - (lookbackPeriod ?? TimeSpan.FromHours(1));

            _lock.EnterReadLock();
            try
            {
                var recentAttempts = _recentAttempts.Values
                    .Where(a => a.AttemptTime >= cutoff)
                    .ToList();

                var authFailures = recentAttempts.Where(a => !a.IsSuccessful && a.HttpStatusCode == 403).ToList();
                var serverErrors = recentAttempts.Where(a => !a.IsSuccessful && a.HttpStatusCode >= 500).ToList();
                var successful = recentAttempts.Where(a => a.IsSuccessful).ToList();

                return new DocumentAccessMonitoringSummary
                {
                    TotalAttempts = recentAttempts.Count,
                    SuccessfulAttempts = successful.Count,
                    AuthorizationFailures = authFailures.Count,
                    ServerErrors = serverErrors.Count,
                    SuccessRate = recentAttempts.Count > 0 ? (double)successful.Count / recentAttempts.Count : 0,
                    AverageResponseTime = successful.Count > 0 
                        ? TimeSpan.FromMilliseconds(successful.Average(a => a.ResponseTime?.TotalMilliseconds ?? 0)) 
                        : TimeSpan.Zero,
                    UniqueTokensUsed = recentAttempts.Select(a => a.TokenHash).Distinct().Count(),
                    LookbackPeriod = lookbackPeriod ?? TimeSpan.FromHours(1)
                };
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(MetricsRetentionHours);
            var oldEntries = _recentAttempts
                .Where(kv => kv.Value.AttemptTime < cutoff)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in oldEntries)
            {
                _recentAttempts.Remove(key);
            }

            // Clean up token metrics that haven't been used recently
            var oldTokens = _tokenMetrics
                .Where(kv => DateTime.UtcNow - kv.Value.LastUsed > TimeSpan.FromHours(MetricsRetentionHours))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var token in oldTokens)
            {
                _tokenMetrics.Remove(token);
            }
        }

        /// <summary>
        /// Check if a job has recent authorization failures that might indicate a pattern
        /// </summary>
        public bool HasRecentAuthorizationFailures(long jobId, TimeSpan lookbackPeriod)
        {
            var cutoff = DateTime.UtcNow - lookbackPeriod;
            
            _lock.EnterReadLock();
            try
            {
                return _recentAttempts.Values
                    .Any(a => a.JobId == jobId && 
                             a.AttemptTime >= cutoff && 
                             !a.IsSuccessful && 
                             a.HttpStatusCode == 403);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Represents a document access attempt for monitoring purposes
    /// </summary>
    public class DocumentAccessAttempt
    {
        public long JobId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string Operation { get; set; } = string.Empty;
        public DateTime AttemptTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsPreFlightCheck { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
        public int? HttpStatusCode { get; set; }
    }

    /// <summary>
    /// Token usage metrics for analyzing authentication patterns
    /// </summary>
    public class TokenUsageMetrics
    {
        public string TokenHash { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int FailedAttempts { get; set; }
        public int AuthorizationFailures { get; set; }
        public int ServerErrors { get; set; }
        public TimeSpan TotalResponseTime { get; set; }
        public DateTime FirstUsed { get; set; } = DateTime.UtcNow;
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;

        public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulAttempts / TotalAttempts : 0;
        public TimeSpan AverageResponseTime => SuccessfulAttempts > 0 
            ? TimeSpan.FromMilliseconds(TotalResponseTime.TotalMilliseconds / SuccessfulAttempts) 
            : TimeSpan.Zero;
    }

    /// <summary>
    /// Summary of document access monitoring metrics
    /// </summary>
    public class DocumentAccessMonitoringSummary
    {
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int AuthorizationFailures { get; set; }
        public int ServerErrors { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int UniqueTokensUsed { get; set; }
        public TimeSpan LookbackPeriod { get; set; }
    }
}