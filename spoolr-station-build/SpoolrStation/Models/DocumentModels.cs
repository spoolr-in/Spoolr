using System.ComponentModel;

namespace SpoolrStation.Models
{
    /// <summary>
    /// Supported file types for document preview and printing
    /// </summary>
    public enum SupportedFileType
    {
        PDF,
        DOCX,
        JPG,
        PNG
    }
    /// <summary>
    /// Represents locked print specifications received from Spoolr Core backend.
    /// These specifications cannot be modified by vendors - only printer selection is allowed.
    /// </summary>
    public class LockedPrintSpecifications
    {
        // All properties are read-only after construction - received from backend
        public string PaperSize { get; init; } = string.Empty;           // "A4", "A3", "LETTER"
        public string Orientation { get; init; } = "PORTRAIT";           // "PORTRAIT", "LANDSCAPE"
        public bool IsColor { get; init; } = false;                     // true = color, false = B&W
        public bool IsDoubleSided { get; init; } = false;               // true = duplex, false = single
        public int Copies { get; init; } = 1;                          // Number of copies
        public int PrintQuality { get; init; } = 600;                  // DPI (300, 600, 1200)
        public decimal TotalCost { get; init; } = 0;                   // Final cost (locked)
        public int TotalPages { get; init; } = 1;                      // Document page count

        // Only changeable setting - printer selection
        public string SelectedPrinter { get; set; } = string.Empty;

        // Display properties for UI binding
        public string PaperSizeDisplay => $"{PaperSize} {Orientation}";
        public string ColorModeDisplay => IsColor ? "Color" : "Black & White";
        public string SidesDisplay => IsDoubleSided ? "Double-sided" : "Single-sided";
        public string CopiesDisplay => $"{Copies} {(Copies == 1 ? "copy" : "copies")}";
        public string QualityDisplay => $"{PrintQuality} DPI";
        public string TotalCostDisplay => $"₹{TotalCost:F2}";

        /// <summary>
        /// Gets a summary of all print specifications for display
        /// </summary>
        public string GetSpecificationsSummary()
        {
            return $"{PaperSizeDisplay}, {ColorModeDisplay}, {SidesDisplay}, {CopiesDisplay}";
        }

        /// <summary>
        /// Validates if the selected printer can handle these specifications
        /// </summary>
        public bool IsCompatibleWithPrinter(DocumentPrinterCapabilities printer)
        {
            return printer.SupportsPaperSize(PaperSize) &&
                   (!IsColor || printer.SupportsColor) &&
                   (!IsDoubleSided || printer.SupportsDuplex);
        }
    }

    /// <summary>
    /// Represents a print job with document and customer information
    /// </summary>
    public class DocumentPrintJob : INotifyPropertyChanged
    {
        private string _status = string.Empty;
        private string _streamingUrl = string.Empty;

        // Job identification
        public long JobId { get; init; }
        public string TrackingCode { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        // Customer information
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerPhone { get; init; } = string.Empty;
        public bool IsAnonymous { get; init; } = false;

        // Document information
        public string OriginalFileName { get; init; } = string.Empty;
        public string FileType { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; } = 0;
        public int TotalPages { get; init; } = 1;

        // Print specifications (locked from backend)
        public LockedPrintSpecifications PrintSpecs { get; init; } = new();

        // Dynamic properties
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public string StreamingUrl
        {
            get => _streamingUrl;
            set
            {
                if (_streamingUrl != value)
                {
                    _streamingUrl = value;
                    OnPropertyChanged(nameof(StreamingUrl));
                    OnPropertyChanged(nameof(HasStreamingUrl));
                }
            }
        }

        // Display properties
        public string CustomerDisplayName => IsAnonymous ? "Anonymous Customer" : CustomerName;
        public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
        public string FileDisplayInfo => $"{OriginalFileName} ({TotalPages} pages, {FileSizeDisplay})";
        public string StatusDisplay => Status.Replace("_", " ");
        public bool HasStreamingUrl => !string.IsNullOrEmpty(StreamingUrl);

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents printer capabilities for document compatibility checking
    /// </summary>
    public class DocumentPrinterCapabilities
    {
        public string PrinterName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsOnline { get; init; } = false;
        public bool IsDefault { get; init; } = false;
        public List<string> SupportedPaperSizes { get; init; } = new();
        public bool SupportsColor { get; init; } = false;
        public bool SupportsDuplex { get; init; } = false;
        public string Location { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;

        // Display properties
        public string StatusIcon => IsOnline ? "✅" : "❌";
        public string StatusText => IsOnline ? "Ready" : "Offline";
        public string StatusColor => IsOnline ? "Green" : "Red";
        public string CapabilitiesText => $"{(SupportsColor ? "Color" : "B&W")}, {(SupportsDuplex ? "Duplex" : "Single")}-sided";

        /// <summary>
        /// Check if this printer supports the specified paper size
        /// </summary>
        public bool SupportsPaperSize(string paperSize)
        {
            return SupportedPaperSizes.Contains(paperSize, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get compatibility message for specific print specifications
        /// </summary>
        public string GetCompatibilityMessage(LockedPrintSpecifications specs)
        {
            var issues = new List<string>();

            if (!SupportsPaperSize(specs.PaperSize))
                issues.Add($"Does not support {specs.PaperSize}");

            if (specs.IsColor && !SupportsColor)
                issues.Add("Does not support color printing");

            if (specs.IsDoubleSided && !SupportsDuplex)
                issues.Add("Does not support double-sided printing");

            if (issues.Count == 0)
                return "✅ Compatible with all job requirements";

            return $"⚠️ {string.Join(", ", issues)}";
        }
    }

    /// <summary>
    /// Represents the result of a document streaming operation
    /// </summary>
    public class DocumentStreamResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public byte[] DocumentData { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = string.Empty;
        public long ContentLength { get; init; } = 0;
        public DateTime StreamedAt { get; init; } = DateTime.Now;

        public static DocumentStreamResult CreateSuccess(byte[] data, string contentType)
        {
            return new DocumentStreamResult
            {
                Success = true,
                DocumentData = data,
                ContentType = contentType,
                ContentLength = data.Length
            };
        }

        public static DocumentStreamResult CreateFailure(string errorMessage)
        {
            return new DocumentStreamResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Represents the result of a print operation
    /// </summary>
    public class PrintResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public string PrintJobId { get; init; } = string.Empty;
        public DateTime PrintStartTime { get; init; } = DateTime.Now;
        public int PagesPrinted { get; init; } = 0;

        public static PrintResult CreateSuccess(string printJobId, int pagesPrinted)
        {
            return new PrintResult
            {
                Success = true,
                PrintJobId = printJobId,
                PagesPrinted = pagesPrinted
            };
        }

        public static PrintResult CreateFailure(string errorMessage)
        {
            return new PrintResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}