using SpoolrStation.Models;
using SpoolrStation.Services.Interfaces;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SpoolrStation.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DocumentService> _logger;
        private AuthService? _authService;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly Dictionary<long, CachedDocument> _documentCache;
        private readonly object _cacheLock = new();
        private const int MAX_CACHE_SIZE_MB = 100;
        private const int URL_EXPIRY_WARNING_MINUTES = 5;
        private long _requestIdCounter = 0;

        public DocumentService(HttpClient httpClient, ILogger<DocumentService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentCache = new Dictionary<long, CachedDocument>();
            
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public void SetAuthService(AuthService authService)
        {
            _authService = authService;
        }

        public async Task<StreamingUrlResult> GetStreamingUrlAsync(long jobId, string? authToken)
        {
            var correlationId = GenerateCorrelationId();
            _logger.LogInformation("Requesting file stream for job {JobId} ({CorrelationId})", jobId, correlationId);

            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Auth token not provided for GetStreamingUrlAsync ({CorrelationId})", correlationId);
                return StreamingUrlResult.CreateFailure("Authentication token was not provided.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"jobs/{jobId}/file");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Backend now streams the file directly, not JSON with URL
                    // Return a dummy URL since we'll use the file endpoint directly
                    var fileUrl = $"http://localhost:8080/api/jobs/{jobId}/file";
                    _logger.LogInformation("File endpoint ready for job {JobId}: {FileUrl}", jobId, fileUrl);
                    return StreamingUrlResult.CreateSuccess(fileUrl, TimeSpan.FromHours(1), "File available via backend proxy");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to access file stream for job {JobId}. Status: {StatusCode}, Content: {ErrorContent}", jobId, response.StatusCode, errorContent);
                    return StreamingUrlResult.CreateFailure($"Backend error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetStreamingUrlAsync for job {JobId} ({CorrelationId})", jobId, correlationId);
                return StreamingUrlResult.CreateFailure($"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<DocumentStreamResult> GetDocumentPreviewAsync(DocumentPrintJob job, string? authToken)
        {
            try
            {
                _logger.LogInformation("Getting document preview for job {JobId}", job.JobId);

                if (string.IsNullOrEmpty(job.StreamingUrl))
                {
                    var urlResult = await GetStreamingUrlAsync(job.JobId, authToken);
                    if (!urlResult.Success)
                    {
                        return DocumentStreamResult.CreateFailure($"Could not get streaming URL: {urlResult.ErrorMessage}");
                    }
                    job.StreamingUrl = urlResult.StreamingUrl;
                }

                return await StreamDocumentToMemoryAsync(job.StreamingUrl, job.JobId, authToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document preview for job {JobId}", job.JobId);
                return DocumentStreamResult.CreateFailure($"Preview error: {ex.Message}");
            }
        }

        public async Task<DocumentStreamResult> StreamDocumentToMemoryAsync(string streamingUrl, long jobId, string? authToken = null)
        {
            try
            {
                // Use provided token or fall back to AuthService
                var token = authToken ?? _authService?.GetAuthToken();
                if (string.IsNullOrEmpty(token))
                {
                    return DocumentStreamResult.CreateFailure("Authentication token not available");
                }

                var request = new HttpRequestMessage(HttpMethod.Get, streamingUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _retryPolicy.ExecuteAsync(async () => await _httpClient.SendAsync(request));
                if (!response.IsSuccessStatusCode)
                {
                    return DocumentStreamResult.CreateFailure($"Failed to download document: {response.StatusCode}");
                }
                var documentData = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                CacheDocument(jobId, documentData, contentType);
                return DocumentStreamResult.CreateSuccess(documentData, contentType);
            }
            catch (Exception ex)
            {
                return DocumentStreamResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        public SupportedFileType DetectFileType(string contentType, string? fileName = null)
        {
            switch (contentType?.ToLower())
            {
                case "application/pdf": return SupportedFileType.PDF;
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                case "application/msword": return SupportedFileType.DOCX;
                case "image/jpeg": return SupportedFileType.JPG;
                case "image/png": return SupportedFileType.PNG;
            }
            if (!string.IsNullOrEmpty(fileName))
            {
                return Path.GetExtension(fileName).ToLower() switch
                {
                    ".pdf" => SupportedFileType.PDF,
                    ".docx" or ".doc" => SupportedFileType.DOCX,
                    ".jpg" or ".jpeg" => SupportedFileType.JPG,
                    ".png" => SupportedFileType.PNG,
                    _ => SupportedFileType.PDF
                };
            }
            return SupportedFileType.PDF;
        }

        public async Task<DocumentStreamResult> ConvertDocxToHtmlAsync(byte[] docxData, long jobId)
        {
            await Task.CompletedTask;
            return DocumentStreamResult.CreateFailure("DOCX conversion not implemented in this version.");
        }

        public async Task<DocumentStreamResult> PrepareImageForDisplayAsync(byte[] imageData, string contentType, long jobId, string? fileName = null)
        {
            await Task.CompletedTask;
            return DocumentStreamResult.CreateFailure("Image preparation not implemented in this version.");
        }

        public void ClearDocumentCache(long jobId)
        {
            lock (_cacheLock) { _documentCache.Remove(jobId); }
        }

        public void ClearAllDocumentCache()
        {
            lock (_cacheLock) { _documentCache.Clear(); }
        }

        public double GetCacheMemoryUsageMB()
        {
            lock (_cacheLock)
            {
                return _documentCache.Values.Sum(doc => (long)doc.Data.Length) / (1024.0 * 1024.0);
            }
        }

        public bool IsStreamingUrlValid(DateTime urlObtainedAt, TimeSpan expiryDuration)
        {
            return DateTime.Now < urlObtainedAt.Add(expiryDuration).Subtract(TimeSpan.FromMinutes(URL_EXPIRY_WARNING_MINUTES));
        }

        public async Task<JobOwnershipResult> VerifyJobOwnershipAsync(long jobId, string trackingCode)
        {
            await Task.CompletedTask;
            return JobOwnershipResult.CreateSuccess();
        }

        private void CacheDocument(long jobId, byte[] data, string contentType)
        {
            lock (_cacheLock)
            {
                if (GetCacheMemoryUsageMB() > MAX_CACHE_SIZE_MB)
                {
                    var oldest = _documentCache.OrderBy(kvp => kvp.Value.CachedAt).First();
                    _documentCache.Remove(oldest.Key);
                }
                _documentCache[jobId] = new CachedDocument { Data = data, ContentType = contentType, CachedAt = DateTime.Now };
            }
        }

        private string GenerateCorrelationId()
        {
            return $"DOC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Interlocked.Increment(ref _requestIdCounter):D6}";
        }

        private class CachedDocument
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public string ContentType { get; set; } = string.Empty;
            public DateTime CachedAt { get; set; } = DateTime.Now;
            public bool IsValid => DateTime.Now.Subtract(CachedAt).TotalMinutes < 25;
        }
    }

    public class StreamingUrlResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public string StreamingUrl { get; init; } = string.Empty;
        public TimeSpan ExpiryDuration { get; init; } = TimeSpan.FromMinutes(30);
        public string Instructions { get; init; } = string.Empty;

        public static StreamingUrlResult CreateSuccess(string streamingUrl, TimeSpan expiryDuration, string instructions)
        {
            return new StreamingUrlResult
            {
                Success = true,
                StreamingUrl = streamingUrl,
                ExpiryDuration = expiryDuration,
                Instructions = instructions
            };
        }

        public static StreamingUrlResult CreateFailure(string errorMessage)
        {
            return new StreamingUrlResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    internal class StreamingUrlApiResponse
    {
        public bool Success { get; set; }
        public string StreamingUrl { get; set; } = string.Empty;
        public int? ExpiryMinutes { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public long JobId { get; set; }
    }

    public class DocumentStreamResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = string.Empty;

        public static DocumentStreamResult CreateSuccess(byte[] data, string contentType)
        {
            return new DocumentStreamResult { Success = true, Data = data, ContentType = contentType };
        }

        public static DocumentStreamResult CreateFailure(string errorMessage)
        {
            return new DocumentStreamResult { Success = false, ErrorMessage = errorMessage };
        }
    }
}