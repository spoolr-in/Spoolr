using Microsoft.Web.WebView2.Core;
using SpoolrStation.Models;
using SpoolrStation.ViewModels;
using SpoolrStation.Services;
using SpoolrStation.Services.Interfaces;
using System.Windows;
using System.IO;

namespace SpoolrStation.Views
{
    /// <summary>
    /// Document Preview Window for displaying print jobs with WebView2 integration
    /// Shows locked print specifications and allows only printer selection
    /// </summary>
    public partial class DocumentPreviewWindow : Window
    {
#pragma warning disable CS0649 // Field is assigned by DataContext in XAML or other initialization
        private DocumentPreviewViewModel? _viewModel;
#pragma warning restore CS0649
        private bool _isWebViewInitialized = false;

        public DocumentPreviewWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

    public DocumentPreviewWindow(DocumentPrintJob job, IDocumentService? documentService = null) : this()
    {
        // Use provided DocumentService (with authentication) or create a new one
        var docService = documentService ?? Services.ServiceProvider.GetDocumentService();
        
        _viewModel = new DocumentPreviewViewModel(job, docService);
        DataContext = _viewModel;
        
        // Subscribe to ViewModel property changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Load document into WebView after window loads
        Loaded += async (s, e) => await LoadDocumentFromJob();
    }

        private async void InitializeAsync()
        {
            try
            {
                // Initialize WebView2
                await DocumentWebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;

                // Configure WebView2 settings for document preview
                DocumentWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                DocumentWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                // DocumentWebView.CoreWebView2.Settings.IsScriptDebuggingEnabled = false; // Not available in this version
                DocumentWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                DocumentWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                DocumentWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;

                // Handle navigation events
                DocumentWebView.CoreWebView2.NavigationCompleted += OnWebViewNavigationCompleted;
                DocumentWebView.CoreWebView2.DOMContentLoaded += OnWebViewDOMContentLoaded;
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Failed to initialize document preview: {ex.Message}", 
                    "Preview Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        #region Event Handlers

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.PrintCommand?.CanExecute(null) == true)
                {
                    _viewModel.PrintCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Print operation failed: {ex.Message}", 
                    "Print Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.RejectJobCommand?.CanExecute(null) == true)
                {
                    _viewModel.RejectJobCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Job rejection failed: {ex.Message}", 
                    "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.RefreshPreviewCommand?.CanExecute(null) == true)
                {
                    _viewModel.RefreshPreviewCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Refresh failed: {ex.Message}", 
                    "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PreviousPageButton_Click called. ViewModel: {_viewModel != null}, Command: {_viewModel?.PreviousPageCommand != null}");
                
                if (_viewModel?.PreviousPageCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing PreviousPageCommand");
                    _viewModel.PreviousPageCommand.Execute(null);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot execute PreviousPageCommand. CanExecute: {_viewModel?.PreviousPageCommand?.CanExecute(null)}");
                }
            }
            catch (Exception ex)
            {
                // Page navigation errors are not critical - just log them
                System.Diagnostics.Debug.WriteLine($"Previous page navigation failed: {ex.Message}");
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"NextPageButton_Click called. ViewModel: {_viewModel != null}, Command: {_viewModel?.NextPageCommand != null}");
                
                if (_viewModel?.NextPageCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing NextPageCommand");
                    _viewModel.NextPageCommand.Execute(null);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot execute NextPageCommand. CanExecute: {_viewModel?.NextPageCommand?.CanExecute(null)}");
                }
            }
            catch (Exception ex)
            {
                // Page navigation errors are not critical - just log them
                System.Diagnostics.Debug.WriteLine($"Next page navigation failed: {ex.Message}");
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.ZoomInCommand?.CanExecute(null) == true)
                {
                    _viewModel.ZoomInCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zoom in failed: {ex.Message}");
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.ZoomOutCommand?.CanExecute(null) == true)
                {
                    _viewModel.ZoomOutCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zoom out failed: {ex.Message}");
            }
        }

        private void FitToWidthButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.FitToWidthCommand?.CanExecute(null) == true)
                {
                    _viewModel.FitToWidthCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fit to width failed: {ex.Message}");
            }
        }

        private void DocumentWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.OnDocumentLoaded(e.IsSuccess);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation completed handler failed: {ex.Message}");
            }
        }

        private async void OnWebViewNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (e.IsSuccess && _isWebViewInitialized)
                {
                    // Apply print preview styles after document loads
                    await ApplyPrintPreviewStyles();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView navigation completed handler failed: {ex.Message}");
            }
        }

        private async void OnWebViewDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            try
            {
                // DOM is loaded, we can now inject custom styles
                await ApplyPrintPreviewStyles();
                
                if (_viewModel != null)
                {
                    _viewModel.OnDOMContentLoaded();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DOM content loaded handler failed: {ex.Message}");
            }
        }

        #endregion
        
        #region ViewModel Event Handlers
        
        /// <summary>
        /// Handle ViewModel property changes to refresh WebView when needed
        /// </summary>
        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(DocumentPreviewViewModel.DocumentImage) && 
                    _viewModel?.IsPdfDocument == true && 
                    _isWebViewInitialized)
                {
                    // Refresh the WebView with updated PDF page/zoom
                    await LoadDocumentIntoWebView();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling property change: {ex.Message}");
            }
        }
        
        #endregion

        #region Document Loading

        /// <summary>
        /// Load document from the current job using the ViewModel
        /// </summary>
        private async Task LoadDocumentFromJob()
        {
            try
            {
                if (_viewModel?.Job != null)
                {
                    // Wait for WebView2 to fully initialize
                    await Task.Delay(1000);
                    
                    if (_isWebViewInitialized)
                    {
                        // Wait for the ViewModel to load the document
                        await WaitForDocumentLoad();
                        
                        // Load the document into WebView2
                        await LoadDocumentIntoWebView();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading document: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Wait for the ViewModel to finish loading the document
        /// </summary>
        private async Task WaitForDocumentLoad()
        {
            const int maxWaitSeconds = 30;
            const int pollIntervalMs = 500;
            var startTime = DateTime.Now;
            
            while (_viewModel?.IsLoading == true && 
                   DateTime.Now.Subtract(startTime).TotalSeconds < maxWaitSeconds)
            {
                await Task.Delay(pollIntervalMs);
            }
            
            // Give a moment for the final property updates
            await Task.Delay(100);
        }
        
        /// <summary>
        /// Load the processed document into the WebView2 control
        /// </summary>
        private async Task LoadDocumentIntoWebView()
        {
            try
            {
                if (_viewModel == null || !_isWebViewInitialized)
                    return;
                
                if (_viewModel.HasError)
                {
                    await LoadErrorPage(_viewModel.ErrorMessage);
                    return;
                }
                
                if (_viewModel.IsPdfDocument && _viewModel.DocumentImage != null)
                {
                    // For PDF documents, display as image with zoom support
                    await LoadPdfImageInWebView(_viewModel.DocumentImage, _viewModel.ZoomLevel);
                }
                else if (!string.IsNullOrEmpty(_viewModel.DocumentDataUrl))
                {
                    // Load document from data URL (HTML, text)
                    if (_viewModel.DocumentDataUrl.StartsWith("data:"))
                    {
                        // For data URLs, we need to use NavigateToString with the decoded content
                        await LoadDataUrlInWebView(_viewModel.DocumentDataUrl);
                    }
                    else
                    {
                        // For regular URLs, use Source property
                        DocumentWebView.Source = new Uri(_viewModel.DocumentDataUrl);
                    }
                }
                else if (_viewModel.DocumentImage != null)
                {
                    // For regular images, create an HTML wrapper
                    await LoadImageInWebView(_viewModel.DocumentImage);
                }
                else
                {
                    await LoadErrorPage("No document content available");
                }
            }
            catch (Exception ex)
            {
                await LoadErrorPage($"Failed to load document: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load an error page in the WebView
        /// </summary>
        private Task LoadErrorPage(string errorMessage)
        {
            var encodedMessage = System.Web.HttpUtility.HtmlEncode(errorMessage);
            var errorHtml = @"<!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Document Error</title>
                <style>
                    body {
                        font-family: 'Segoe UI', Arial, sans-serif;
                        padding: 40px;
                        text-align: center;
                        background-color: #f8f9fa;
                    }
                    .error-container {
                        background: white;
                        border-radius: 8px;
                        padding: 30px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        max-width: 500px;
                        margin: 0 auto;
                    }
                    .error-icon {
                        font-size: 48px;
                        color: #dc3545;
                        margin-bottom: 20px;
                    }
                    .error-message {
                        font-size: 16px;
                        color: #6c757d;
                        margin-bottom: 20px;
                    }
                    .retry-button {
                        background-color: #007bff;
                        color: white;
                        border: none;
                        padding: 10px 20px;
                        border-radius: 4px;
                        cursor: pointer;
                        font-size: 14px;
                    }
                    .retry-button:hover {
                        background-color: #0056b3;
                    }
                </style>
            </head>
            <body>
                <div class='error-container'>
                    <div class='error-icon'>&#9888;</div>
                    <div class='error-message'>" + encodedMessage + @"</div>
                    <button class='retry-button' onclick='alert(""Refresh clicked"")'>
                        Retry Loading
                    </button>
                </div>
            </body>
            </html>";
            
            DocumentWebView.NavigateToString(errorHtml);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Load a data URL in the WebView by decoding the content
        /// </summary>
        private async Task LoadDataUrlInWebView(string dataUrl)
        {
            try
            {
                // Parse data URL format: data:[mediatype][;base64],<data>
                var parts = dataUrl.Split(',');
                if (parts.Length != 2)
                {
                    await LoadErrorPage("Invalid data URL format");
                    return;
                }
                
                var headerPart = parts[0];
                var dataPart = parts[1];
                
                // Check if it's base64 encoded
                if (headerPart.Contains("base64"))
                {
                    // Decode base64 content
                    try
                    {
                        var bytes = Convert.FromBase64String(dataPart);
                        var content = System.Text.Encoding.UTF8.GetString(bytes);
                        
                        // For HTML content, navigate directly
                        if (headerPart.Contains("text/html"))
                        {
                            DocumentWebView.NavigateToString(content);
                        }
                        else if (headerPart.Contains("application/pdf"))
                        {
                            // For PDF, we need to create an HTML wrapper that can display the PDF
                            var pdfHtml = CreatePdfViewerHtml(dataUrl);
                            DocumentWebView.NavigateToString(pdfHtml);
                        }
                        else
                        {
                            // For other content types, wrap in HTML
                            var wrappedHtml = $@"
                            <!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset='utf-8'>
                                <title>Document Content</title>
                                <style>
                                    body {{
                                        font-family: 'Segoe UI', Arial, sans-serif;
                                        padding: 20px;
                                        line-height: 1.6;
                                    }}
                                </style>
                            </head>
                            <body>
                                <pre>{System.Web.HttpUtility.HtmlEncode(content)}</pre>
                            </body>
                            </html>";
                            DocumentWebView.NavigateToString(wrappedHtml);
                        }
                    }
                    catch (Exception ex)
                    {
                        await LoadErrorPage($"Failed to decode document content: {ex.Message}");
                    }
                }
                else
                {
                    await LoadErrorPage("Non-base64 data URLs are not supported");
                }
            }
            catch (Exception ex)
            {
                await LoadErrorPage($"Failed to load data URL: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create HTML for displaying PDF in an iframe
        /// </summary>
        private string CreatePdfViewerHtml(string pdfDataUrl)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>PDF Document</title>
                <style>
                    body {{
                        margin: 0;
                        padding: 0;
                        height: 100vh;
                        display: flex;
                        flex-direction: column;
                        background-color: #f5f5f5;
                    }}
                    .pdf-container {{
                        flex: 1;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        padding: 20px;
                    }}
                    iframe {{
                        width: 100%;
                        height: 100%;
                        border: none;
                        border-radius: 8px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    }}
                    .pdf-info {{
                        text-align: center;
                        padding: 10px;
                        background: white;
                        color: #666;
                        font-family: 'Segoe UI', Arial, sans-serif;
                        font-size: 12px;
                    }}
                </style>
            </head>
            <body>
                <div class='pdf-info'>PDF Document Preview</div>
                <div class='pdf-container'>
                    <iframe src='{pdfDataUrl}' type='application/pdf'>
                        <p>Your browser does not support PDFs. <a href='{pdfDataUrl}'>Download the PDF</a>.</p>
                    </iframe>
                </div>
            </body>
            </html>";
        }
        
        /// <summary>
        /// Load a PDF page image in the WebView with zoom support
        /// </summary>
        private async Task LoadPdfImageInWebView(System.Windows.Media.Imaging.BitmapImage image, double zoomLevel)
        {
            try
            {
                // Convert BitmapImage to base64 for display
                var base64 = BitmapImageToBase64(image);
                
                var imageHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>PDF Document - Page {_viewModel?.CurrentPage ?? 1}</title>
                    <style>
                        body {{
                            margin: 0;
                            padding: 20px;
                            background-color: #f5f5f5;
                            display: flex;
                            justify-content: center;
                            align-items: flex-start;
                            min-height: 100vh;
                            font-family: 'Segoe UI', Arial, sans-serif;
                        }}
                        .pdf-container {{
                            background: white;
                            border-radius: 8px;
                            padding: 20px;
                            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
                            text-align: center;
                            max-width: 100%;
                        }}
                        .page-info {{
                            color: #666;
                            font-size: 14px;
                            margin-bottom: 15px;
                            padding: 10px;
                            background-color: #f8f9fa;
                            border-radius: 4px;
                        }}
                        img {{
                            max-width: 100%;
                            height: auto;
                            border-radius: 4px;
                            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                            transform: scale({zoomLevel});
                            transform-origin: top center;
                            transition: transform 0.3s ease;
                        }}
                        .zoom-info {{
                            margin-top: 15px;
                            color: #888;
                            font-size: 12px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='pdf-container'>
                        <div class='page-info'>
                            📝 PDF Document - Page {_viewModel?.CurrentPage ?? 1} of {_viewModel?.TotalPages ?? 1}
                        </div>
                        <img src='data:image/png;base64,{base64}' alt='PDF Page {_viewModel?.CurrentPage ?? 1}' />
                        <div class='zoom-info'>
                            Zoom: {(zoomLevel * 100):0}% | Use toolbar controls to navigate and zoom
                        </div>
                    </div>
                </body>
                </html>";
                
                DocumentWebView.NavigateToString(imageHtml);
            }
            catch (Exception ex)
            {
                await LoadErrorPage($"Failed to display PDF page: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load an image document in the WebView with proper styling
        /// </summary>
        private Task LoadImageInWebView(System.Windows.Media.Imaging.BitmapImage image)
        {
            // Convert BitmapImage to base64 for display
            var base64 = BitmapImageToBase64(image);
            
            var imageHtml = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Image Document</title>
                <style>
                    body {{
                        margin: 0;
                        padding: 20px;
                        background-color: #f5f5f5;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                    }}
                    .image-container {{
                        background: white;
                        border-radius: 8px;
                        padding: 20px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        text-align: center;
                    }}
                    img {{
                        max-width: 100%;
                        max-height: 80vh;
                        border-radius: 4px;
                    }}
                </style>
            </head>
            <body>
                <div class='image-container'>
                    <img src='data:image/png;base64,{base64}' alt='Document Image' />
                </div>
            </body>
            </html>";
            
            DocumentWebView.NavigateToString(imageHtml);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Convert BitmapImage to base64 string
        /// </summary>
        private string BitmapImageToBase64(System.Windows.Media.Imaging.BitmapImage bitmapImage)
        {
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
            
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }

        #endregion

        #region WebView2 Integration

        /// <summary>
        /// Loads a document into the WebView2 control with print preview styling
        /// </summary>
        public Task LoadDocumentAsync(string documentUrl, LockedPrintSpecifications specs)
        {
            try
            {
                if (!_isWebViewInitialized)
                {
                    throw new InvalidOperationException("WebView2 is not initialized");
                }

                // Generate CSS for print specifications
                var printStyles = GeneratePrintPreviewCSS(specs);
                
                // Create HTML wrapper with document and print styles
                var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Document Preview</title>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    {printStyles}
                </head>
                <body>
                    <div class='document-container'>
                        <iframe src='{documentUrl}' 
                                id='documentFrame'
                                style='width:100%;height:100vh;border:none;'
                                onload='window.dispatchEvent(new Event(""documentFrameLoaded""));'>
                        </iframe>
                    </div>
                    <div class='print-overlay'>
                        <div class='spec-indicator'>{specs.GetSpecificationsSummary()}</div>
                        <div class='copy-indicator'>{(specs.Copies > 1 ? $"Copy 1 of {specs.Copies}" : "")}</div>
                    </div>
                </body>
                </html>";

                DocumentWebView.NavigateToString(html);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load document: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies print preview CSS styles to show document as it will print
        /// </summary>
        private async Task ApplyPrintPreviewStyles()
        {
            try
            {
                if (!_isWebViewInitialized || _viewModel?.Job?.PrintSpecs == null)
                    return;

                var specs = _viewModel.Job.PrintSpecs;
                var cssInjection = GenerateStyleInjection(specs);
                
                await DocumentWebView.CoreWebView2.ExecuteScriptAsync(cssInjection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply print preview styles: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates CSS for print preview based on print specifications
        /// </summary>
        private static string GeneratePrintPreviewCSS(LockedPrintSpecifications specs)
        {
            var paperDimensions = GetPaperDimensions(specs.PaperSize);
            var colorFilter = specs.IsColor ? "" : "filter: grayscale(100%);";
            var orientationClass = specs.Orientation.ToLower();

            return $@"
                <style>
                /* Print Preview Styles */
                body {{
                    margin: 0;
                    padding: 20px;
                    background: #f5f5f5;
                    font-family: 'Segoe UI', Arial, sans-serif;
                }}

                .document-container {{
                    background: white;
                    margin: 0 auto;
                    box-shadow: 0 0 10px rgba(0,0,0,0.3);
                    border-radius: 8px;
                    overflow: hidden;
                    position: relative;
                    {colorFilter}
                }}

                .document-container.{orientationClass} {{
                    width: {paperDimensions.Width};
                    height: {paperDimensions.Height};
                }}

                .print-overlay {{
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    pointer-events: none;
                    z-index: 1000;
                }}

                .spec-indicator {{
                    position: fixed;
                    top: 15px;
                    left: 15px;
                    background: rgba(33, 150, 243, 0.9);
                    color: white;
                    padding: 8px 12px;
                    border-radius: 6px;
                    font-size: 11px;
                    font-weight: 500;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.2);
                }}

                .copy-indicator {{
                    position: fixed;
                    top: 15px;
                    right: 15px;
                    background: rgba(76, 175, 80, 0.9);
                    color: white;
                    padding: 8px 12px;
                    border-radius: 6px;
                    font-size: 11px;
                    font-weight: 500;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.2);
                    display: {(specs.Copies > 1 ? "block" : "none")};
                }}

                /* Print media styles */
                @media print {{
                    @page {{
                        size: {specs.PaperSize.ToLower()} {orientationClass};
                        margin: 1in;
                    }}
                    
                    body {{
                        background: white;
                        padding: 0;
                    }}
                    
                    .print-overlay {{
                        display: none;
                    }}
                }}
                </style>";
        }

        /// <summary>
        /// Generates JavaScript for injecting styles into the document
        /// </summary>
        private static string GenerateStyleInjection(LockedPrintSpecifications specs)
        {
            return @"
                (function() {
                    try {
                        // Remove any existing preview styles
                        const existingStyle = document.getElementById('printPreviewStyles');
                        if (existingStyle) {
                            existingStyle.remove();
                        }

                        // Add updated preview styles
                        const style = document.createElement('style');
                        style.id = 'printPreviewStyles';
                        style.innerHTML = `" + GenerateInlineCSS(specs) + @"`;
                        document.head.appendChild(style);

                        // Update overlay information
                        const specIndicator = document.querySelector('.spec-indicator');
                        if (specIndicator) {
                            specIndicator.textContent = '" + specs.GetSpecificationsSummary() + @"';
                        }

                        console.log('Print preview styles applied successfully');
                    } catch (error) {
                        console.error('Failed to apply print preview styles:', error);
                    }
                })();
            ";
        }

        private static string GenerateInlineCSS(LockedPrintSpecifications specs)
        {
            var colorFilter = specs.IsColor ? "" : "filter: grayscale(100%);";
            return $@"
                iframe#documentFrame {{
                    {colorFilter}
                    border-radius: 4px;
                }}
                
                .document-container {{
                    {colorFilter}
                }}
            ";
        }

        /// <summary>
        /// Gets paper dimensions for CSS styling
        /// </summary>
        private static (string Width, string Height) GetPaperDimensions(string paperSize)
        {
            return paperSize.ToUpperInvariant() switch
            {
                "A4" => ("21cm", "29.7cm"),
                "A3" => ("29.7cm", "42cm"),
                "LETTER" => ("8.5in", "11in"),
                "LEGAL" => ("8.5in", "14in"),
                _ => ("21cm", "29.7cm") // Default to A4
            };
        }

        #endregion

        #region Window Lifecycle

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Clean up WebView2 resources
                if (_isWebViewInitialized)
                {
                    DocumentWebView.Dispose();
                }

                // Clean up ViewModel resources
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window cleanup: {ex.Message}");
            }

            base.OnClosing(e);
        }

        #endregion
    }
}