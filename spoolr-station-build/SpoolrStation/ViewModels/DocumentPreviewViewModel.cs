using SpoolrStation.Models;
using SpoolrStation.Services;
using SpoolrStation.Services.Core;
using SpoolrStation.Services.Interfaces;
using SpoolrStation.Utilities;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpoolrStation.ViewModels
{
    public class DocumentPreviewViewModel : BaseViewModel, IDisposable
    {
        private readonly IDocumentService _documentService;
        private readonly AuthenticationStateManager _authStateManager;
        private DocumentPrintJob _job;
        private string? _documentSource;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _isPdfDocument;
        private BitmapImage? _documentImage; // FIX: Changed from BitmapSource to BitmapImage
        private int _currentPage;
        private int _totalPages;
        private double _zoomLevel = 1.0;

        public DocumentPrintJob Job { get => _job; set => SetProperty(ref _job, value); }
        public string? DocumentSource { get => _documentSource; set => SetProperty(ref _documentSource, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public bool IsPdfDocument { get => _isPdfDocument; set => SetProperty(ref _isPdfDocument, value); }
        public BitmapImage? DocumentImage { get => _documentImage; set => SetProperty(ref _documentImage, value); } // FIX: Changed from BitmapSource to BitmapImage
        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }
        public double ZoomLevel { get => _zoomLevel; set => SetProperty(ref _zoomLevel, value); }
        public string DocumentDataUrl => DocumentSource ?? string.Empty;
        
        // Printer selection
        private System.Collections.ObjectModel.ObservableCollection<DocumentPrinterCapabilities> _availablePrinters = new();
        private DocumentPrinterCapabilities? _selectedPrinter;
        public System.Collections.ObjectModel.ObservableCollection<DocumentPrinterCapabilities> AvailablePrinters 
        {
            get => _availablePrinters; 
            set => SetProperty(ref _availablePrinters, value); 
        }
        public DocumentPrinterCapabilities? SelectedPrinter 
        {
            get => _selectedPrinter; 
            set => SetProperty(ref _selectedPrinter, value); 
        }
        public bool CanPrint => SelectedPrinter != null && !IsLoading;

        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RejectJobCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToWidthCommand { get; }

        public DocumentPreviewViewModel(DocumentPrintJob job, IDocumentService? documentService = null)
        {
            _job = job;
            _documentService = documentService ?? Services.ServiceProvider.GetDocumentService();
            _authStateManager = Services.ServiceProvider.GetAuthenticationStateManager();

            PrintCommand = new RelayCommand(async () => await ExecutePrint(), () => !IsLoading);
            // FIX: Fully qualify System.Windows.Application to resolve ambiguity
            CloseCommand = new RelayCommand(() => System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext == this)?.Close());
            RejectJobCommand = new RelayCommand(() => { /* Placeholder */ });
            RefreshPreviewCommand = new RelayCommand(async () => await LoadDocumentAsync());
            NextPageCommand = new RelayCommand(() => { if (CurrentPage < TotalPages) CurrentPage++; });
            PreviousPageCommand = new RelayCommand(() => { if (CurrentPage > 1) CurrentPage--; });
            ZoomInCommand = new RelayCommand(() => { ZoomLevel *= 1.2; });
            ZoomOutCommand = new RelayCommand(() => { ZoomLevel /= 1.2; });
            FitToWidthCommand = new RelayCommand(() => { /* Placeholder */ });

            _ = LoadDocumentAsync();
            _ = LoadAvailablePrintersAsync();
        }

        // FIX: Add boolean parameter to match the call from the View's code-behind
        public void OnDocumentLoaded(bool isSuccess) 
        {
            // Placeholder for any logic needed after the document loads in the view
        }
        public void OnDOMContentLoaded() { /* Placeholder */ }

        private async Task LoadDocumentAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            try
            {
                var authToken = await _authStateManager.GetValidTokenAsync();
                if (string.IsNullOrEmpty(authToken))
                {
                    throw new InvalidOperationException("Authentication required - please log in.");
                }

                var result = await _documentService.GetDocumentPreviewAsync(Job, authToken);

                if (result.Success && !string.IsNullOrEmpty(result.TempFilePath))
                {
                    DocumentSource = result.TempFilePath;
                }
                else
                {
                    throw new Exception(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load document: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecutePrint()
        {
            try
            {
                if (SelectedPrinter == null)
                {
                    System.Windows.MessageBox.Show("Please select a printer first.", "No Printer Selected", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                if (string.IsNullOrEmpty(DocumentSource) || !File.Exists(DocumentSource))
                {
                    System.Windows.MessageBox.Show("Document file not found. Please refresh the preview.", "File Not Found", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                IsLoading = true;
                ErrorMessage = string.Empty;
                
                var printService = new PrintService();
                var printResult = await printService.PrintPdfAsync(DocumentSource, SelectedPrinter.PrinterName, Job.PrintSpecs);
                
                if (printResult.Success)
                {
                    var completionResult = System.Windows.MessageBox.Show(
                        $"Print job sent successfully!\n\nHas the customer picked up the printed documents?",
                        "Print Complete - Mark as Completed?",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);
                    
                    if (completionResult == System.Windows.MessageBoxResult.Yes)
                    {
                        var webSocketClient = Services.ServiceProvider.GetWebSocketClient();
                        if (webSocketClient != null)
                        {
                            var completedStatusUpdated = await webSocketClient.UpdateJobStatusToCompletedAsync(Job.JobId);
                            if (completedStatusUpdated)
                            {
                                var completionDialog = new SpoolrStation.Views.CompletionDialog();
                                if (completionDialog.ShowDialog() == true)
                                {
                                    if (MainViewModel.Instance != null)
                                    {
                                        var jobToComplete = MainViewModel.Instance.AcceptedJobs.FirstOrDefault(j => j.JobId == Job.JobId);
                                        if (jobToComplete != null)
                                        {
                                            MainViewModel.Instance.CompleteJobCommand.Execute(jobToComplete);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    "Print succeeded but failed to mark as completed. Please mark it manually later.",
                                    "Status Update Failed",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Warning);
                            }
                        }
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                            .SingleOrDefault(w => w.DataContext == this);
                        window?.Close();
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Print failed: {printResult.ErrorMessage}\n\nPlease check the printer and try again.",
                        "Print Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"An error occurred while printing:\n{ex.Message}",
                    "Print Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(DocumentSource) && File.Exists(DocumentSource))
            {
                try { File.Delete(DocumentSource); } catch { /* Ignore */ }
            }
        }

        private async Task LoadAvailablePrintersAsync()
        {
            try
            {
                var printerService = Services.ServiceProvider.GetPrinterDiscoveryService();
                var result = await printerService.DiscoverPrintersAsync();
                
                if (!result.Success || result.Printers == null)
                    return;
                
                // Convert LocalPrinter to DocumentPrinterCapabilities with compatibility info
                foreach (var printer in result.Printers)
                {
                    var printerName = printer.Name;
                    var isOnline = printer.Status == PrinterStatus.Ready;
                    var isDefault = printer.IsDefault;
                    
                    // Get paper sizes as strings
                    var paperSizes = printer.Capabilities?.SupportedPaperSizes
                        .Select(ps => ps.Name)
                        .ToList() ?? new List<string>();
                    
                    var compatibilityMsg = GetCompatibilityMessage(Job.PrintSpecs, paperSizes, 
                        printer.Capabilities?.SupportsColor ?? false, 
                        printer.Capabilities?.SupportsDuplex ?? false);
                    
                    var docPrinter = new DocumentPrinterCapabilities
                    {
                        PrinterName = printerName,
                        DisplayName = $"{(isDefault ? "⭐ " : "")}{printerName} - {compatibilityMsg}",
                        IsOnline = isOnline,
                        IsDefault = isDefault,
                        SupportedPaperSizes = paperSizes,
                        SupportsColor = printer.Capabilities?.SupportsColor ?? false,
                        SupportsDuplex = printer.Capabilities?.SupportsDuplex ?? false,
                        Status = isOnline ? "Ready" : "Offline"
                    };
                    
                    AvailablePrinters.Add(docPrinter);
                }
                
                // Auto-select default printer or first online printer
                SelectedPrinter = AvailablePrinters.FirstOrDefault(p => p.IsDefault && p.IsOnline) 
                               ?? AvailablePrinters.FirstOrDefault(p => p.IsOnline)
                               ?? AvailablePrinters.FirstOrDefault();
                               
                OnPropertyChanged(nameof(CanPrint));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load printers: {ex.Message}");
            }
        }
        
        private string GetCompatibilityMessage(LockedPrintSpecifications specs, List<string> supportedPaperSizes, bool supportsColor, bool supportsDuplex)
        {
            var issues = new List<string>();
            
            if (!supportedPaperSizes.Any(size => size.Contains(specs.PaperSize, StringComparison.OrdinalIgnoreCase)))
                issues.Add($"No {specs.PaperSize}");
            
            if (specs.IsColor && !supportsColor)
                issues.Add("No color");
            
            if (specs.IsDoubleSided && !supportsDuplex)
                issues.Add("No duplex");
            
            if (issues.Count == 0)
                return "✅ Compatible";
            
            return $"⚠️ {string.Join(", ", issues)}";
        }
    }
}
