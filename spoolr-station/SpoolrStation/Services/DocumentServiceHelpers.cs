using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Supporting classes and methods for enhanced DocumentService
    /// </summary>
    public static class DocumentServiceHelpers
    {
        /// <summary>
        /// Generates a unique correlation ID for request tracking
        /// </summary>
        public static string GenerateCorrelationId() => $"DOC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    /// <summary>
    /// Job ownership verification result
    /// </summary>
    public class JobOwnershipResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public JobOwnershipFailureReason FailureReason { get; init; }

        public static JobOwnershipResult CreateSuccess()
            => new() { Success = true };

        public static JobOwnershipResult CreateFailure(string errorMessage, JobOwnershipFailureReason reason)
            => new() { Success = false, ErrorMessage = errorMessage, FailureReason = reason };
    }

    /// <summary>
    /// Reasons for job ownership verification failure
    /// </summary>
    public enum JobOwnershipFailureReason
    {
        NotAuthenticated,
        OwnershipMismatch,
        JobNotFound,
        NetworkError,
        UnknownError
    }
}