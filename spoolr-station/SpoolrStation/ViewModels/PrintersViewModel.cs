using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SpoolrStation.Models;
using SpoolrStation.Services;
using SpoolrStation.Utilities;

namespace SpoolrStation.ViewModels
{
    /// <summary>
    /// ViewModel for the Printers tab - handles printer discovery, selection, and capabilities management
    /// </summary>
    public class PrintersViewModel : BaseViewModel
    {
        private readonly PrinterDiscoveryService _printerDiscoveryService;
        private readonly PrinterCapabilitiesService _printerCapabilitiesService;
        private readonly AuthService _authService;

        // ======================== PROPERTIES ========================

        private ObservableCollection<PrinterViewModel> _printers = new();
        public ObservableCollection<PrinterViewModel> Printers
        {
            get => _printers;
            set => SetProperty(ref _printers, value);
        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        private bool _isSendingCapabilities;
        public bool IsSendingCapabilities
        {
            get => _isSendingCapabilities;
            set => SetProperty(ref _isSendingCapabilities, value);
        }

        private string _statusMessage = "No printers scanned yet. Click 'Scan for Printers' to discover available printers.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private PrinterViewModel? _selectedPrinter;
        public PrinterViewModel? SelectedPrinter
        {
            get => _selectedPrinter;
            set => SetProperty(ref _selectedPrinter, value);
        }

        private bool _selectAll;
        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                if (SetProperty(ref _selectAll, value))
                {
                    // Update all printer selections
                    foreach (var printer in Printers)
                    {
                        printer.IsSelected = value;
                    }
                    OnPropertyChanged(nameof(SelectedPrintersCount));
                    OnPropertyChanged(nameof(HasSelectedPrinters));
                }
            }
        }

        // ======================== COMPUTED PROPERTIES ========================

        public int SelectedPrintersCount => Printers.Count(p => p.IsSelected);
        public bool HasSelectedPrinters => SelectedPrintersCount > 0;
        public bool HasPrinters => Printers.Any();
        public int TotalPrinters => Printers.Count;
        public int OnlinePrinters => Printers.Count(p => p.IsOnline);

        public string PrintersSummary
        {
            get
            {
                if (!HasPrinters) return "No printers found";
                return $"{TotalPrinters} printer(s) found ({OnlinePrinters} online)";
            }
        }

        // ======================== COMMANDS ========================

        public ICommand ScanPrintersCommand { get; }
        public ICommand SendCapabilitiesCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand ViewCapabilitiesCommand { get; }

        // ======================== CONSTRUCTOR ========================

        public PrintersViewModel(AuthService authService)
        {
            _authService = authService;
            _printerDiscoveryService = new PrinterDiscoveryService();
            _printerCapabilitiesService = new PrinterCapabilitiesService(authService);

            // Initialize commands
            ScanPrintersCommand = new RelayCommand(async () => await ScanForPrintersAsync(), () => !IsScanning);
            SendCapabilitiesCommand = new RelayCommand(async () => await SendSelectedCapabilitiesAsync(), CanSendCapabilities);
            SelectAllCommand = new RelayCommand(() => SelectAll = !SelectAll);
            RefreshStatusCommand = new RelayCommand(async () => await RefreshPrinterStatusAsync());
            ViewCapabilitiesCommand = new RelayCommand<PrinterViewModel>(ViewPrinterCapabilities);
        }

        // ======================== PRINTER DISCOVERY ========================

        public async Task ScanForPrintersAsync()
        {
            try
            {
                IsScanning = true;
                HasError = false;
                StatusMessage = "Scanning for printers...";

                // Clear existing printers
                Printers.Clear();
                OnPropertyChanged(nameof(HasPrinters));
                OnPropertyChanged(nameof(PrintersSummary));

                // Discover printers
                var result = await _printerDiscoveryService.DiscoverPrintersAsync();

                if (result.Success)
                {
                    // Convert to ViewModels and add to collection
                    foreach (var printer in result.Printers)
                    {
                        var printerVM = new PrinterViewModel(printer);
                        
                        // Subscribe to selection changes to update counts
                        printerVM.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(PrinterViewModel.IsSelected))
                            {
                                OnPropertyChanged(nameof(SelectedPrintersCount));
                                OnPropertyChanged(nameof(HasSelectedPrinters));
                            }
                        };

                        Printers.Add(printerVM);
                    }

                    StatusMessage = $"Scan complete: {result.Message}. Found {result.Printers.Count} printer(s) in {result.DiscoveryDuration.TotalSeconds:F1}s";
                }
                else
                {
                    HasError = true;
                    StatusMessage = $"Scan failed: {result.Message}";
                }

                // Update computed properties
                OnPropertyChanged(nameof(HasPrinters));
                OnPropertyChanged(nameof(TotalPrinters));
                OnPropertyChanged(nameof(OnlinePrinters));
                OnPropertyChanged(nameof(PrintersSummary));
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        // ======================== CAPABILITIES MANAGEMENT ========================

        public async Task SendSelectedCapabilitiesAsync()
        {
            try
            {
                IsSendingCapabilities = true;
                HasError = false;

                var selectedPrinters = Printers.Where(p => p.IsSelected).Select(p => p.Printer).ToList();
                
                if (!selectedPrinters.Any())
                {
                    StatusMessage = "No printers selected. Please select at least one printer to send capabilities.";
                    return;
                }

                StatusMessage = $"Sending capabilities for {selectedPrinters.Count} selected printer(s) to backend...";

                // Validate capabilities
                var validation = _printerCapabilitiesService.ValidateCapabilities(selectedPrinters);
                if (!validation.IsValid)
                {
                    HasError = true;
                    StatusMessage = $"Validation failed: {validation.ValidationMessage}";
                    return;
                }

                // Send capabilities
                var result = await _printerCapabilitiesService.SendCapabilitiesToBackendAsync(selectedPrinters);

                if (result.Success)
                {
                    // Mark capabilities as sent in local settings
                    await _printerCapabilitiesService.MarkCapabilitiesAsSentAsync();
                    
                    // Mark selected printers as sent
                    foreach (var printerVM in Printers.Where(p => p.IsSelected))
                    {
                        printerVM.IsCapabilitiesSent = true;
                    }

                    StatusMessage = $"✅ {result.Message}";
                }
                else
                {
                    HasError = true;
                    StatusMessage = $"❌ Failed to send capabilities: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"❌ Error sending capabilities: {ex.Message}";
            }
            finally
            {
                IsSendingCapabilities = false;
            }
        }

        private bool CanSendCapabilities()
        {
            return HasSelectedPrinters && !IsSendingCapabilities && _authService.CurrentSession?.IsValid == true;
        }

        // ======================== PRINTER STATUS MANAGEMENT ========================

        public async Task RefreshPrinterStatusAsync()
        {
            try
            {
                if (!HasPrinters) return;

                StatusMessage = "Refreshing printer status...";

                var updatedPrinters = await _printerDiscoveryService.RefreshPrinterStatusAsync();

                // Update existing printer ViewModels
                foreach (var printerVM in Printers)
                {
                    var updated = updatedPrinters.FirstOrDefault(p => p.Name == printerVM.Name);
                    if (updated != null)
                    {
                        printerVM.UpdatePrinter(updated);
                    }
                }

                OnPropertyChanged(nameof(OnlinePrinters));
                OnPropertyChanged(nameof(PrintersSummary));
                StatusMessage = $"Status refreshed: {OnlinePrinters}/{TotalPrinters} printers online";
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Status refresh failed: {ex.Message}";
            }
        }

        // ======================== CAPABILITY VIEWING ========================

        private void ViewPrinterCapabilities(PrinterViewModel? printer)
        {
            if (printer == null) return;

            SelectedPrinter = printer;
            
            // In a real implementation, this might open a detailed capabilities dialog
            StatusMessage = $"Viewing capabilities for {printer.Name}: {printer.CapabilitiesSummary}";
        }

        // ======================== HELPER METHODS ========================

        /// <summary>
        /// Gets summary of selected printers for display
        /// </summary>
        public string GetSelectedPrintersSummary()
        {
            var selectedPrinters = Printers.Where(p => p.IsSelected).ToList();
            if (!selectedPrinters.Any())
                return "No printers selected";

            var onlineSelected = selectedPrinters.Count(p => p.IsOnline);
            var colorSelected = selectedPrinters.Count(p => p.Printer.Capabilities.SupportsColor);
            
            return $"{selectedPrinters.Count} selected ({onlineSelected} online, {colorSelected} color)";
        }

        /// <summary>
        /// Auto-select all online printers
        /// </summary>
        public void SelectOnlinePrinters()
        {
            foreach (var printer in Printers)
            {
                printer.IsSelected = printer.IsOnline;
            }
            
            OnPropertyChanged(nameof(SelectedPrintersCount));
            OnPropertyChanged(nameof(HasSelectedPrinters));
            StatusMessage = $"Auto-selected {SelectedPrintersCount} online printer(s)";
        }
    }
}
