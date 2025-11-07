using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SpoolrStation.ViewModels;

namespace SpoolrStation.Models
{
    // ======================== PRINTER INFORMATION MODELS ========================

    /// <summary>
    /// Represents a local printer with its capabilities and status
    /// </summary>
    public class LocalPrinter
    {
        public string Name { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public PrinterStatus Status { get; set; } = PrinterStatus.Unknown;
        public bool IsDefault { get; set; }
        public bool IsNetworkPrinter { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // Capabilities
        public PrinterCapabilities Capabilities { get; set; } = new PrinterCapabilities();
        
        public bool IsOnline => Status == PrinterStatus.Ready || Status == PrinterStatus.Idle;
        public string StatusText => GetStatusText();
        
        private string GetStatusText()
        {
            return Status switch
            {
                PrinterStatus.Ready => "Ready",
                PrinterStatus.Idle => "Idle",
                PrinterStatus.Printing => "Printing",
                PrinterStatus.Error => "Error",
                PrinterStatus.PaperEmpty => "Paper Empty",
                PrinterStatus.PaperJam => "Paper Jam",
                PrinterStatus.Offline => "Offline",
                PrinterStatus.TonerLow => "Toner Low",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Printer status enumeration
    /// </summary>
    public enum PrinterStatus
    {
        Unknown = 0,
        Ready = 1,
        Idle = 2,
        Printing = 3,
        Error = 4,
        PaperEmpty = 5,
        PaperJam = 6,
        Offline = 7,
        TonerLow = 8,
        Paused = 9
    }

    /// <summary>
    /// Comprehensive printer capabilities
    /// </summary>
    public class PrinterCapabilities
    {
        // Basic capabilities
        public bool SupportsColor { get; set; }
        public bool SupportsDuplex { get; set; }
        public bool SupportsCollation { get; set; }
        public int MaxCopies { get; set; } = 1;
        
        // Paper sizes supported
        public List<PaperSize> SupportedPaperSizes { get; set; } = new List<PaperSize>();
        
        // Print qualities/resolutions
        public List<PrintQuality> SupportedQualities { get; set; } = new List<PrintQuality>();
        
        // Media types
        public List<string> SupportedMediaTypes { get; set; } = new List<string>();
        
        // Print orientations
        public bool SupportsPortrait { get; set; } = true;
        public bool SupportsLandscape { get; set; } = true;
        
        // Additional capabilities
        public int MinMarginLeft { get; set; }
        public int MinMarginRight { get; set; }
        public int MinMarginTop { get; set; }
        public int MinMarginBottom { get; set; }
        
        // Tray information
        public List<PrinterTray> Trays { get; set; } = new List<PrinterTray>();

        /// <summary>
        /// Gets a summary of printer capabilities for display
        /// </summary>
        public string GetCapabilitiesSummary()
        {
            var capabilities = new List<string>();
            
            if (SupportsColor)
                capabilities.Add("Color");
            else
                capabilities.Add("B&W");
                
            if (SupportsDuplex)
                capabilities.Add("Duplex");
                
            capabilities.Add($"{SupportedPaperSizes.Count} Paper Sizes");
            capabilities.Add($"{SupportedQualities.Count} Quality Options");
            
            if (MaxCopies > 1)
                capabilities.Add($"Up to {MaxCopies} copies");
            
            return string.Join(", ", capabilities);
        }
    }

    /// <summary>
    /// Paper size information
    /// </summary>
    public class PaperSize
    {
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; } // in hundredths of millimeters
        public int Height { get; set; } // in hundredths of millimeters
        public bool IsDefault { get; set; }
        
        public string DisplayName => $"{Name} ({WidthInches:F1}\" x {HeightInches:F1}\")";
        public double WidthInches => Width / 2540.0;
        public double HeightInches => Height / 2540.0;
    }

    /// <summary>
    /// Print quality/resolution information
    /// </summary>
    public class PrintQuality
    {
        public string Name { get; set; } = string.Empty;
        public int DpiX { get; set; }
        public int DpiY { get; set; }
        public bool IsDefault { get; set; }
        
        public string DisplayName => $"{Name} ({DpiX}x{DpiY} DPI)";
    }

    /// <summary>
    /// Printer tray information
    /// </summary>
    public class PrinterTray
    {
        public string Name { get; set; } = string.Empty;
        public PaperSize? CurrentPaperSize { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public bool IsEmpty { get; set; }
    }

    // ======================== BACKEND API MODELS ========================

    /// <summary>
    /// Request model for updating printer capabilities on the backend
    /// Must match the backend PrinterCapabilitiesRequest DTO
    /// </summary>
    public class PrinterCapabilitiesRequest
    {
        [Required]
        public string Capabilities { get; set; } = string.Empty; // JSON string of capabilities
    }

    /// <summary>
    /// Simplified printer capabilities for backend API
    /// This will be serialized to JSON and sent as the "capabilities" string
    /// </summary>
    public class BackendPrinterCapabilities
    {
        public List<BackendPrinter> Printers { get; set; } = new List<BackendPrinter>();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string StationVersion { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Simplified printer information for backend
    /// </summary>
    public class BackendPrinter
    {
        public string Name { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsOnline { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Capabilities simplified for backend
        public bool SupportsColor { get; set; }
        public bool SupportsDuplex { get; set; }
        public List<string> SupportedPaperSizes { get; set; } = new List<string>();
        public List<string> SupportedQualities { get; set; } = new List<string>();
        public int MaxCopies { get; set; } = 1;
    }

    // ======================== UI MODELS ========================

    /// <summary>
    /// ViewModel representation of a printer for UI binding
    /// </summary>
    public class PrinterViewModel : BaseViewModel
    {
        private LocalPrinter _printer;
        private bool _isSelected;
        private bool _isCapabilitiesSent;

        public PrinterViewModel(LocalPrinter printer)
        {
            _printer = printer;
        }

        public LocalPrinter Printer
        {
            get => _printer;
            set => SetProperty(ref _printer, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsCapabilitiesSent
        {
            get => _isCapabilitiesSent;
            set => SetProperty(ref _isCapabilitiesSent, value);
        }

        // Convenience properties for UI binding
        public string Name => Printer.Name;
        public string StatusText => Printer.StatusText;
        public bool IsOnline => Printer.IsOnline;
        public bool IsDefault => Printer.IsDefault;
        public string DriverName => Printer.DriverName;
        public string PortName => Printer.PortName;
        
        // Capabilities summary for UI
        public string CapabilitiesSummary
        {
            get
            {
                var capabilities = new List<string>();
                
                if (Printer.Capabilities.SupportsColor)
                    capabilities.Add("Color");
                else
                    capabilities.Add("B&W");
                    
                if (Printer.Capabilities.SupportsDuplex)
                    capabilities.Add("Duplex");
                    
                capabilities.Add($"{Printer.Capabilities.SupportedPaperSizes.Count} Paper Sizes");
                
                return string.Join(", ", capabilities);
            }
        }

        public void UpdatePrinter(LocalPrinter updatedPrinter)
        {
            Printer = updatedPrinter;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(CapabilitiesSummary));
        }
    }

    // ======================== PRINTER DISCOVERY RESULT ========================

    /// <summary>
    /// Result of printer discovery operation
    /// </summary>
    public class PrinterDiscoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<LocalPrinter> Printers { get; set; } = new List<LocalPrinter>();
        public DateTime DiscoveryTime { get; set; } = DateTime.Now;
        public TimeSpan DiscoveryDuration { get; set; }
    }
}
