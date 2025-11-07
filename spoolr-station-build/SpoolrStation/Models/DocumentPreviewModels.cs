using System;

namespace SpoolrStation.Models
{
    public class DocumentPreviewResult
    {
        public bool Success { get; set; }
        public byte[]? DocumentData { get; set; }
        public string? ContentType { get; set; }
        public string? StreamingUrl { get; set; }
        public int ExpiryMinutes { get; set; }
        public string? ErrorMessage { get; set; }

        public static DocumentPreviewResult CreateSuccess(byte[] documentData, string contentType, string streamingUrl, int expiryMinutes)
        {
            return new DocumentPreviewResult
            {
                Success = true,
                DocumentData = documentData,
                ContentType = contentType,
                StreamingUrl = streamingUrl,
                ExpiryMinutes = expiryMinutes
            };
        }

        public static DocumentPreviewResult CreateFailure(string errorMessage)
        {
            return new DocumentPreviewResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class PreviewStreamingUrlResult
    {
        public bool Success { get; set; }
        public string? StreamingUrl { get; set; }
        public int ExpiryMinutes { get; set; }
        public string? ErrorMessage { get; set; }

        public static PreviewStreamingUrlResult CreateSuccess(string streamingUrl, int expiryMinutes)
        {
            return new PreviewStreamingUrlResult
            {
                Success = true,
                StreamingUrl = streamingUrl,
                ExpiryMinutes = expiryMinutes
            };
        }

        public static PreviewStreamingUrlResult CreateFailure(string errorMessage)
        {
            return new PreviewStreamingUrlResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class DocumentDownloadResult
    {
        public bool Success { get; set; }
        public byte[]? DocumentData { get; set; }
        public string? ContentType { get; set; }
        public string? ErrorMessage { get; set; }

        public static DocumentDownloadResult CreateSuccess(byte[] documentData, string contentType)
        {
            return new DocumentDownloadResult
            {
                Success = true,
                DocumentData = documentData,
                ContentType = contentType
            };
        }

        public static DocumentDownloadResult CreateFailure(string errorMessage)
        {
            return new DocumentDownloadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class StreamingUrlResponse
    {
        public bool Success { get; set; }
        public string StreamingUrl { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public long JobId { get; set; }
        public string? Error { get; set; }
    }

    public enum DocumentFileType
    {
        PDF,
        Image,
        Text,
        Generic
    }
}