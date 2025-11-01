using SpoolrStation.Models;

namespace SpoolrStation.Services.Interfaces
{
    /// <summary>
    /// Interface for printer discovery operations - compatible with existing PrinterDiscoveryService
    /// </summary>
    public interface IPrinterDiscoveryService
    {
        /// <summary>
        /// Discovers all available printers on the system
        /// </summary>
        /// <returns>List of available printers with their capabilities</returns>
        Task<List<DocumentPrinterCapabilities>> DiscoverPrintersAsync();

        /// <summary>
        /// Gets the default printer for the system
        /// </summary>
        /// <returns>Default printer capabilities or null if none found</returns>
        Task<DocumentPrinterCapabilities?> GetDefaultPrinterAsync();

        /// <summary>
        /// Finds printers compatible with the specified print specifications
        /// </summary>
        /// <param name="specs">Print specifications to match against</param>
        /// <returns>List of compatible printers, ordered by best match</returns>
        Task<List<DocumentPrinterCapabilities>> FindCompatiblePrintersAsync(LockedPrintSpecifications specs);

        /// <summary>
        /// Refreshes printer status and capabilities (clears cache)
        /// </summary>
        void RefreshPrinters();

        /// <summary>
        /// Checks if a specific printer is online and available
        /// </summary>
        /// <param name="printerName">Name of the printer to check</param>
        /// <returns>True if printer is online and ready</returns>
        Task<bool> IsPrinterOnlineAsync(string printerName);
    }
}