using System;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PdfiumViewer;
using SpoolrStation.Models;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service for printing PDF documents to Windows printers
    /// Uses PdfiumViewer for high-quality PDF rendering
    /// </summary>
    public class PrintService
    {
        private readonly ILogger<PrintService>? _logger;

        public PrintService(ILogger<PrintService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Prints a PDF document with specified print specifications
        /// </summary>
        public async Task<PrintResult> PrintPdfAsync(string pdfFilePath, string printerName, LockedPrintSpecifications specs)
        {
            try
            {
                _logger?.LogInformation("Starting print job: {FileName} to {Printer}", Path.GetFileName(pdfFilePath), printerName);

                if (!File.Exists(pdfFilePath))
                {
                    return PrintResult.CreateFailure($"File not found: {pdfFilePath}");
                }

                // Validate printer exists
                var printerSettings = new PrinterSettings { PrinterName = printerName };
                if (!printerSettings.IsValid)
                {
                    return PrintResult.CreateFailure($"Printer '{printerName}' is not available");
                }

                // Print using PdfiumViewer (runs synchronously on print thread)
                var pagesPrinted = await Task.Run(() =>
                {
                    using var pdfDocument = PdfDocument.Load(pdfFilePath);
                    using var printDoc = pdfDocument.CreatePrintDocument();
                    
                    // Apply printer settings
                    printDoc.PrinterSettings.PrinterName = printerName;
                    printDoc.PrinterSettings.Copies = (short)specs.Copies;
                    printDoc.PrinterSettings.Duplex = specs.IsDoubleSided ? Duplex.Vertical : Duplex.Simplex;
                    
                    // Apply page settings
                    printDoc.DefaultPageSettings.Color = specs.IsColor;
                    printDoc.DefaultPageSettings.Landscape = specs.Orientation.Equals("LANDSCAPE", StringComparison.OrdinalIgnoreCase);
                    
                    // Set paper size
                    bool paperSizeSet = false;
                    foreach (System.Drawing.Printing.PaperSize paperSize in printDoc.PrinterSettings.PaperSizes)
                    {
                        if (paperSize.PaperName.Contains(specs.PaperSize, StringComparison.OrdinalIgnoreCase))
                        {
                            printDoc.DefaultPageSettings.PaperSize = paperSize;
                            paperSizeSet = true;
                            _logger?.LogInformation("Paper size set to: {PaperSize}", paperSize.PaperName);
                            break;
                        }
                    }
                    
                    if (!paperSizeSet)
                    {
                        _logger?.LogWarning("Paper size {RequestedSize} not found, using printer default", specs.PaperSize);
                    }

                    _logger?.LogInformation("Print settings: {PaperSize} {Orientation}, {ColorMode}, {Duplex}, {Copies} copies",
                        specs.PaperSize, specs.Orientation, specs.IsColor ? "Color" : "B&W",
                        specs.IsDoubleSided ? "Duplex" : "Simplex", specs.Copies);

                    // Send to printer
                    printDoc.Print();
                    
                    return pdfDocument.PageCount * specs.Copies;
                });

                _logger?.LogInformation("Print job completed: {Pages} pages printed", pagesPrinted);

                return PrintResult.CreateSuccess($"Print_{DateTime.Now:yyyyMMddHHmmss}", pagesPrinted);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Print failed for {FileName}", Path.GetFileName(pdfFilePath));
                return PrintResult.CreateFailure($"Print failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates if the printer can handle the print specifications
        /// </summary>
        public bool ValidatePrinterCapabilities(string printerName, LockedPrintSpecifications specs, out string validationMessage)
        {
            try
            {
                var printerSettings = new PrinterSettings { PrinterName = printerName };

                if (!printerSettings.IsValid)
                {
                    validationMessage = $"Printer '{printerName}' is not available";
                    return false;
                }

                var issues = new System.Collections.Generic.List<string>();

                // Check color support
                if (specs.IsColor && !printerSettings.SupportsColor)
                {
                    issues.Add("Printer does not support color printing");
                }

                // Check duplex support
                if (specs.IsDoubleSided && !printerSettings.CanDuplex)
                {
                    issues.Add("Printer does not support double-sided printing");
                }

                // Check paper size support
                bool paperSizeSupported = false;
                foreach (System.Drawing.Printing.PaperSize paperSize in printerSettings.PaperSizes)
                {
                    if (paperSize.PaperName.Contains(specs.PaperSize, StringComparison.OrdinalIgnoreCase))
                    {
                        paperSizeSupported = true;
                        break;
                    }
                }

                if (!paperSizeSupported)
                {
                    issues.Add($"Printer does not support {specs.PaperSize} paper size");
                }

                if (issues.Count > 0)
                {
                    validationMessage = string.Join("; ", issues);
                    return false;
                }

                validationMessage = "Printer is compatible with all specifications";
                return true;
            }
            catch (Exception ex)
            {
                validationMessage = $"Validation error: {ex.Message}";
                return false;
            }
        }
    }
}
