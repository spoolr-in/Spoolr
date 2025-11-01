using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using SpoolrStation.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PdfiumViewer;

namespace SpoolrStation.Services
{
    /// <summary>
    /// PDF document renderer using PdfiumViewer library
    /// </summary>
    public class PdfDocumentRenderer : IPdfDocumentRenderer, IDisposable
    {
        private readonly ILogger<PdfDocumentRenderer> _logger;
        private PdfDocument? _pdfDocument;
        private bool _disposed = false;

        public PdfDocumentRenderer(ILogger<PdfDocumentRenderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get the total number of pages in the loaded PDF
        /// </summary>
        public int PageCount => _pdfDocument?.PageCount ?? 0;

        /// <summary>
        /// Load a PDF document from byte array
        /// </summary>
        public PdfLoadResult LoadPdfDocument(byte[] pdfData)
        {
            try
            {
                _logger.LogInformation("Loading PDF document from memory: {Size} bytes", pdfData.Length);

                // Dispose existing document if any
                _pdfDocument?.Dispose();

                // Create PDF document from memory stream
                using var stream = new MemoryStream(pdfData);
                _pdfDocument = PdfDocument.Load(stream);

                var pageCount = _pdfDocument.PageCount;
                _logger.LogInformation("Successfully loaded PDF document: {PageCount} pages", pageCount);

                return PdfLoadResult.CreateSuccess(pageCount);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF library compatibility issue - PdfiumViewer may not be compatible with .NET 9.0");
                return PdfLoadResult.CreateFailure($"PDF viewer compatibility issue: {ex.Message}. The PDF library may not be compatible with .NET 9.0. Consider using a browser-based PDF viewer.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load PDF document");
                return PdfLoadResult.CreateFailure($"Failed to load PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Render a specific page as a bitmap image
        /// </summary>
        public BitmapImage? RenderPage(int pageIndex, int dpi = 150)
        {
            try
            {
                if (_pdfDocument == null)
                {
                    _logger.LogWarning("Attempted to render page without loaded PDF document");
                    return null;
                }

                if (pageIndex < 0 || pageIndex >= _pdfDocument.PageCount)
                {
                    _logger.LogWarning("Page index {PageIndex} is out of range (0-{MaxIndex})", 
                        pageIndex, _pdfDocument.PageCount - 1);
                    return null;
                }

                _logger.LogDebug("Rendering PDF page {PageIndex} at {DPI} DPI", pageIndex, dpi);

                // Render the page as a bitmap
                using var image = _pdfDocument.Render(pageIndex, dpi, dpi, false);
                using var bitmap = new Bitmap(image);
                
                // Convert System.Drawing.Bitmap to WPF BitmapImage
                var bitmapImage = ConvertToBitmapImage(bitmap);
                
                _logger.LogDebug("Successfully rendered PDF page {PageIndex}: {Width}x{Height}", 
                    pageIndex, bitmap.Width, bitmap.Height);

                return bitmapImage;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF rendering compatibility issue on page {PageIndex} - PdfiumViewer may not be compatible with .NET 9.0", pageIndex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render PDF page {PageIndex}", pageIndex);
                return null;
            }
        }

        /// <summary>
        /// Render all pages as bitmap images
        /// </summary>
        public BitmapImage[] RenderAllPages(int dpi = 150)
        {
            if (_pdfDocument == null)
            {
                _logger.LogWarning("Attempted to render all pages without loaded PDF document");
                return Array.Empty<BitmapImage>();
            }

            _logger.LogInformation("Rendering all {PageCount} pages at {DPI} DPI", _pdfDocument.PageCount, dpi);

            var pages = new List<BitmapImage>();
            for (int i = 0; i < _pdfDocument.PageCount; i++)
            {
                var page = RenderPage(i, dpi);
                if (page != null)
                {
                    pages.Add(page);
                }
            }

            _logger.LogInformation("Successfully rendered {RenderedCount} of {TotalCount} pages", 
                pages.Count, _pdfDocument.PageCount);

            return pages.ToArray();
        }

        /// <summary>
        /// Get page dimensions for a specific page
        /// </summary>
        public (double Width, double Height) GetPageDimensions(int pageIndex)
        {
            try
            {
                if (_pdfDocument == null || pageIndex < 0 || pageIndex >= _pdfDocument.PageCount)
                {
                    return (0, 0);
                }

                var pageSize = _pdfDocument.PageSizes[pageIndex];
                return (pageSize.Width, pageSize.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get page dimensions for page {PageIndex}", pageIndex);
                return (0, 0);
            }
        }

        /// <summary>
        /// Convert System.Drawing.Bitmap to WPF BitmapImage
        /// </summary>
        private static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            
            // Save bitmap to memory stream as PNG to preserve quality
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            // Create BitmapImage from stream
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Make it thread-safe

            return bitmapImage;
        }

        /// <summary>
        /// Dispose of loaded PDF resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _pdfDocument?.Dispose();
                _pdfDocument = null;
                _disposed = true;
                _logger.LogDebug("PDF document renderer disposed");
            }
        }

        ~PdfDocumentRenderer()
        {
            Dispose(false);
        }
    }
}