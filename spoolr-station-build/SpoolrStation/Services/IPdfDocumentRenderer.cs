using System.Windows.Media.Imaging;

namespace SpoolrStation.Services.Interfaces
{
    /// <summary>
    /// Service interface for rendering PDF documents as images
    /// </summary>
    public interface IPdfDocumentRenderer
    {
        /// <summary>
        /// Load a PDF document from byte array
        /// </summary>
        /// <param name="pdfData">PDF document bytes</param>
        /// <returns>Result indicating success or failure</returns>
        PdfLoadResult LoadPdfDocument(byte[] pdfData);

        /// <summary>
        /// Get the total number of pages in the loaded PDF
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// Render a specific page as a bitmap image
        /// </summary>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="dpi">DPI for rendering quality (default: 150)</param>
        /// <returns>Rendered page as BitmapImage</returns>
        BitmapImage? RenderPage(int pageIndex, int dpi = 150);

        /// <summary>
        /// Render all pages as bitmap images
        /// </summary>
        /// <param name="dpi">DPI for rendering quality (default: 150)</param>
        /// <returns>Array of rendered pages</returns>
        BitmapImage[] RenderAllPages(int dpi = 150);

        /// <summary>
        /// Get page dimensions for a specific page
        /// </summary>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <returns>Page dimensions in points</returns>
        (double Width, double Height) GetPageDimensions(int pageIndex);

        /// <summary>
        /// Dispose of loaded PDF resources
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Result of PDF loading operation
    /// </summary>
    public class PdfLoadResult
    {
        public bool Success { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public int PageCount { get; init; }

        public static PdfLoadResult CreateSuccess(int pageCount)
        {
            return new PdfLoadResult
            {
                Success = true,
                PageCount = pageCount
            };
        }

        public static PdfLoadResult CreateFailure(string errorMessage)
        {
            return new PdfLoadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}