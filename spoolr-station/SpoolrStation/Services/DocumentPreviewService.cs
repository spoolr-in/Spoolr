using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpoolrStation.Models;
using System.IO;
using System.Diagnostics;

namespace SpoolrStation.Services
{
    /// <summary>
    /// New robust document preview service with proper authentication handling
    /// Completely replaces the old DocumentService for preview functionality
    /// </summary>
    public class DocumentPreviewService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DocumentPreviewService> _logger;
        private bool _disposed = false;

        public DocumentPreviewService(HttpClient httpClient, ILogger<DocumentPreviewService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main method to get document preview for a job with full authentication flow
        /// </summary>
        /// <param name="jobId">The job ID to preview</param>
        /// <param name="authToken">JWT authentication token</param>
        /// <param name="vendorId">Vendor ID for logging</param>
        /// <returns>DocumentPreviewResult with success status and data</returns>
        public async Task<DocumentPreviewResult> GetDocumentPreviewAsync(long jobId, string authToken, long vendorId)
        {
            try
            {
                _logger.LogInformation("Starting document preview for job {JobId} with vendor {VendorId}", jobId, vendorId);
                
                // Validate inputs
                if (string.IsNullOrEmpty(authToken))
                {
                    return DocumentPreviewResult.CreateFailure("Authentication token is required");
                }

                // Step 1: Get streaming URL from backend
                var streamingUrlResult = await GetStreamingUrlAsync(jobId, authToken);
                if (!streamingUrlResult.Success)
                {
                    return DocumentPreviewResult.CreateFailure($"Failed to get streaming URL: {streamingUrlResult.ErrorMessage}");
                }

                // Step 2: Download document from streaming URL
                var documentResult = await DownloadDocumentFromStreamingUrlAsync(streamingUrlResult.StreamingUrl!, jobId);
                if (!documentResult.Success)
                {
                    return DocumentPreviewResult.CreateFailure($"Failed to download document: {documentResult.ErrorMessage}");
                }

                _logger.LogInformation("Successfully prepared document preview for job {JobId}", jobId);
                
                return DocumentPreviewResult.CreateSuccess(
                    documentResult.DocumentData!,
                    documentResult.ContentType!,
                    streamingUrlResult.StreamingUrl!,
                    streamingUrlResult.ExpiryMinutes
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document preview for job {JobId}", jobId);
                return DocumentPreviewResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Step 1: Get streaming URL from Spoolr Core backend
        /// </summary>
        private async Task<PreviewStreamingUrlResult> GetStreamingUrlAsync(long jobId, string authToken)
        {
            try
            {
                _logger.LogInformation("Requesting streaming URL for job {JobId}", jobId);

                // Create HTTP request with proper authentication
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/jobs/{jobId}/file");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                // Enhanced debugging - log complete request details
                _logger.LogInformation("=== STATION APP DEBUG: Request Details ===");
                _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
                _logger.LogInformation("Request URI: {RequestUri}", request.RequestUri);
                _logger.LogInformation("Absolute Request URI: {AbsoluteUri}", _httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, request.RequestUri!) : request.RequestUri);
                _logger.LogInformation("Authorization Header: Bearer {TokenPrefix}... (length: {TokenLength})", 
                    authToken.Length > 20 ? authToken.Substring(0, 20) : authToken, authToken.Length);
                
                // Console output for immediate debugging
                Console.WriteLine($"=== STATION DEBUG: Sending request ===");
                Console.WriteLine($"Base Address: {_httpClient.BaseAddress}");
                Console.WriteLine($"Request URI: {request.RequestUri}");
                Console.WriteLine($"Full URL: {(_httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, request.RequestUri!) : request.RequestUri)}");
                Console.WriteLine($"Token length: {authToken.Length}");
                Console.WriteLine($"Token prefix: {(authToken.Length > 20 ? authToken.Substring(0, 20) : authToken)}...");

                // Send request
                using var response = await _httpClient.SendAsync(request);

                // Enhanced response debugging
                Console.WriteLine($"=== STATION DEBUG: Response received ===");
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Reason Phrase: {response.ReasonPhrase}");
                _logger.LogInformation("Streaming URL request response: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var streamingResponse = JsonSerializer.Deserialize<StreamingUrlResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (streamingResponse != null && streamingResponse.Success)
                    {
                        _logger.LogInformation("Successfully obtained streaming URL for job {JobId}, expires in {ExpiryMinutes} minutes", 
                            jobId, streamingResponse.ExpiryMinutes);
                        
                        return PreviewStreamingUrlResult.CreateSuccess(
                            streamingResponse.StreamingUrl,
                            streamingResponse.ExpiryMinutes
                        );
                    }
                    else
                    {
                        var error = streamingResponse?.Error ?? "Unknown error from backend";
                        _logger.LogError("Backend returned error: {Error}", error);
                        return PreviewStreamingUrlResult.CreateFailure(error);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"=== STATION DEBUG: Error Response ===");
                    Console.WriteLine($"Status: {response.StatusCode}");
                    Console.WriteLine($"Content: {errorContent}");
                    _logger.LogError("HTTP error {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                    
                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => PreviewStreamingUrlResult.CreateFailure("Authentication failed - please log in again"),
                        System.Net.HttpStatusCode.Forbidden => PreviewStreamingUrlResult.CreateFailure("Access denied - job may not belong to your vendor account"),
                        System.Net.HttpStatusCode.NotFound => PreviewStreamingUrlResult.CreateFailure("Job not found"),
                        _ => PreviewStreamingUrlResult.CreateFailure($"Server error: {response.StatusCode}")
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error requesting streaming URL for job {JobId}", jobId);
                return PreviewStreamingUrlResult.CreateFailure($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout for streaming URL job {JobId}", jobId);
                return PreviewStreamingUrlResult.CreateFailure("Request timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error requesting streaming URL for job {JobId}", jobId);
                return PreviewStreamingUrlResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Step 2: Download document from the MinIO streaming URL
        /// </summary>
        private async Task<DocumentDownloadResult> DownloadDocumentFromStreamingUrlAsync(string streamingUrl, long jobId)
        {
            try
            {
                _logger.LogInformation("Downloading document from streaming URL for job {JobId}", jobId);

                using var response = await _httpClient.GetAsync(streamingUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var documentData = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                    _logger.LogInformation("Successfully downloaded document for job {JobId}: {Size} bytes, type: {ContentType}", 
                        jobId, documentData.Length, contentType);

                    return DocumentDownloadResult.CreateSuccess(documentData, contentType);
                }
                else
                {
                    _logger.LogError("Failed to download document: HTTP {StatusCode}", response.StatusCode);
                    return DocumentDownloadResult.CreateFailure($"Download failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document from streaming URL for job {JobId}", jobId);
                return DocumentDownloadResult.CreateFailure($"Download error: {ex.Message}");
            }
        }

        /// <summary>
        /// Open document preview window with the downloaded document data
        /// </summary>
        public void OpenPreviewWindow(DocumentPreviewResult previewResult, long jobId, string fileName)
        {
            try
            {
                if (!previewResult.Success)
                {
                    throw new InvalidOperationException($"Cannot open preview for failed result: {previewResult.ErrorMessage}");
                }

                _logger.LogInformation("Opening preview window for job {JobId}, file: {FileName}", jobId, fileName);

                // Determine file type and appropriate preview method
                var fileType = DetermineFileType(previewResult.ContentType!, fileName);
                
                switch (fileType)
                {
                    case DocumentFileType.PDF:
                        OpenPdfPreview(previewResult.DocumentData!, fileName, jobId);
                        break;
                    
                    case DocumentFileType.Image:
                        OpenImagePreview(previewResult.DocumentData!, fileName, jobId);
                        break;
                    
                    case DocumentFileType.Text:
                        OpenTextPreview(previewResult.DocumentData!, fileName, jobId);
                        break;
                    
                    default:
                        OpenGenericPreview(previewResult.DocumentData!, fileName, jobId, previewResult.ContentType!);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening preview window for job {JobId}", jobId);
                throw;
            }
        }

        #region File Type Handling

        private DocumentFileType DetermineFileType(string contentType, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => DocumentFileType.PDF,
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" => DocumentFileType.Image,
                ".txt" or ".log" => DocumentFileType.Text,
                _ => contentType.StartsWith("image/") ? DocumentFileType.Image :
                     contentType.Contains("pdf") ? DocumentFileType.PDF :
                     contentType.StartsWith("text/") ? DocumentFileType.Text :
                     DocumentFileType.Generic
            };
        }

        private void OpenPdfPreview(byte[] documentData, string fileName, long jobId)
        {
            _logger.LogInformation("Opening PDF preview for job {JobId}", jobId);
            
            // For now, save to temp file and open with default PDF viewer
            // Later, this can be enhanced with embedded PDF viewer
            var tempPath = Path.Combine(Path.GetTempPath(), $"spoolr_preview_{jobId}_{Path.GetFileName(fileName)}");
            File.WriteAllBytes(tempPath, documentData);
            
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }

        private void OpenImagePreview(byte[] documentData, string fileName, long jobId)
        {
            _logger.LogInformation("Opening image preview for job {JobId}", jobId);
            
            var tempPath = Path.Combine(Path.GetTempPath(), $"spoolr_preview_{jobId}_{Path.GetFileName(fileName)}");
            File.WriteAllBytes(tempPath, documentData);
            
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }

        private void OpenTextPreview(byte[] documentData, string fileName, long jobId)
        {
            _logger.LogInformation("Opening text preview for job {JobId}", jobId);
            
            var tempPath = Path.Combine(Path.GetTempPath(), $"spoolr_preview_{jobId}_{Path.GetFileName(fileName)}");
            File.WriteAllBytes(tempPath, documentData);
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = tempPath,
                UseShellExecute = true
            });
        }

        private void OpenGenericPreview(byte[] documentData, string fileName, long jobId, string contentType)
        {
            _logger.LogInformation("Opening generic preview for job {JobId}, content type: {ContentType}", jobId, contentType);
            
            var tempPath = Path.Combine(Path.GetTempPath(), $"spoolr_preview_{jobId}_{Path.GetFileName(fileName)}");
            File.WriteAllBytes(tempPath, documentData);
            
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                // HttpClient is managed by dependency injection, don't dispose it here
                _disposed = true;
            }
        }
    }

    // Models moved to SpoolrStation.Models.DocumentPreviewModels
}