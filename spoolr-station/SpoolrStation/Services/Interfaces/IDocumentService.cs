using SpoolrStation.Models;
using System;
using System.Threading.Tasks;

namespace SpoolrStation.Services.Interfaces
{
    public interface IDocumentService
    {
        void SetAuthService(AuthService authService);
        Task<StreamingUrlResult> GetStreamingUrlAsync(long jobId, string? authToken);
        Task<DocumentStreamResult> GetDocumentPreviewAsync(DocumentPrintJob job, string? authToken);
        Task<DocumentStreamResult> StreamDocumentToMemoryAsync(string streamingUrl, long jobId, string? authToken = null);
        SupportedFileType DetectFileType(string contentType, string? fileName = null);
        Task<DocumentStreamResult> ConvertDocxToHtmlAsync(byte[] docxData, long jobId);
        Task<DocumentStreamResult> PrepareImageForDisplayAsync(byte[] imageData, string contentType, long jobId, string? fileName = null);
        void ClearDocumentCache(long jobId);
        void ClearAllDocumentCache();
        double GetCacheMemoryUsageMB();
        bool IsStreamingUrlValid(DateTime urlObtainedAt, TimeSpan expiryDuration);
        Task<JobOwnershipResult> VerifyJobOwnershipAsync(long jobId, string trackingCode);
    }
}
