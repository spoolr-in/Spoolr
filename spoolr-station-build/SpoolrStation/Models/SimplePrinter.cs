using System.ComponentModel;

namespace SpoolrStation.Models
{
    /// <summary>
    /// Simple printer model for basic printer selection dropdown
    /// No complex compatibility scoring - just basic yes/no compatibility
    /// </summary>
    public class SimplePrinter : INotifyPropertyChanged
    {
        private bool _isSelected;

        /// <summary>
        /// System printer name
        /// </summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether printer is online/available
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// Whether this is the default system printer
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Whether this printer can handle the current job requirements
        /// </summary>
        public bool IsCompatible { get; set; } = true;

        /// <summary>
        /// Simple compatibility reason (if not compatible)
        /// </summary>
        public string CompatibilityReason { get; set; } = string.Empty;

        /// <summary>
        /// Whether this printer is currently selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // UI Display Properties

        /// <summary>
        /// Status icon for display
        /// </summary>
        public string StatusIcon => IsOnline ? (IsDefault ? "🖨️⭐" : "🖨️✅") : "🖨️❌";

        /// <summary>
        /// Compatibility message for display
        /// </summary>
        public string CompatibilityMessage => IsCompatible 
            ? "Compatible" 
            : string.IsNullOrEmpty(CompatibilityReason) 
                ? "Not Compatible" 
                : CompatibilityReason;

        /// <summary>
        /// Color for compatibility message
        /// </summary>
        public string CompatibilityColor => IsCompatible ? "#2E7D32" : "#D32F2F";

        /// <summary>
        /// Full display name with status
        /// </summary>
        public string FullDisplayName => $"{StatusIcon} {DisplayName}";

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a SimplePrinter from PrinterDiscoveryService printer data
        /// </summary>
        public static SimplePrinter FromLocalPrinter(LocalPrinter localPrinter, LockedPrintSpecifications printSpecs)
        {
            var simple = new SimplePrinter
            {
                PrinterName = localPrinter.Name,
                DisplayName = localPrinter.Name,
                IsOnline = localPrinter.IsOnline,
                IsDefault = localPrinter.IsDefault
            };

            // Simple compatibility check
            simple.CheckCompatibility(printSpecs);

            return simple;
        }

        /// <summary>
        /// Performs basic compatibility check against print specifications
        /// </summary>
        public void CheckCompatibility(LockedPrintSpecifications printSpecs)
        {
            if (!IsOnline)
            {
                IsCompatible = false;
                CompatibilityReason = "Printer is offline";
                return;
            }

            // For now, assume all online printers are compatible
            // In a real implementation, you would check paper size, color capability, etc.
            // But based on requirements, we're keeping this simple
            IsCompatible = true;
            CompatibilityReason = string.Empty;
        }
    }
}