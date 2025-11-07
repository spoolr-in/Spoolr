using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using SpoolrStation.Models;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service for discovering local printers and their capabilities using Windows APIs
    /// </summary>
    public class PrinterDiscoveryService
    {
        private readonly List<LocalPrinter> _lastDiscoveredPrinters = new();
        
        /// <summary>
        /// Discovers all available local printers and their capabilities
        /// </summary>
        /// <returns>PrinterDiscoveryResult with found printers</returns>
        public async Task<PrinterDiscoveryResult> DiscoverPrintersAsync()
        {
            var startTime = DateTime.Now;
            var result = new PrinterDiscoveryResult();
            
            try
            {
                var printers = new List<LocalPrinter>();
                
                // Get printers using .NET PrinterSettings (faster, basic info)
                var basicPrinters = await GetBasicPrinterInfoAsync();
                
                // Enhance with detailed capabilities using WMI (slower, detailed info)
                foreach (var basicPrinter in basicPrinters)
                {
                    try
                    {
                        var detailedPrinter = await EnhancePrinterWithCapabilitiesAsync(basicPrinter);
                        printers.Add(detailedPrinter);
                    }
                    catch (Exception ex)
                    {
                        // If we can't get detailed info, use basic info
                        Console.WriteLine($"[PRINTER] Warning: Could not get detailed capabilities for {basicPrinter.Name}: {ex.Message}");
                        printers.Add(basicPrinter);
                    }
                }
                
                result.Success = true;
                result.Printers = printers;
                result.Message = $"Found {printers.Count} printer(s)";
                result.DiscoveryDuration = DateTime.Now - startTime;
                
                // Cache the results
                _lastDiscoveredPrinters.Clear();
                _lastDiscoveredPrinters.AddRange(printers);
                
                Console.WriteLine($"[PRINTER] Discovery completed: {result.Message} in {result.DiscoveryDuration.TotalMilliseconds:F0}ms");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Printer discovery failed: {ex.Message}";
                result.DiscoveryDuration = DateTime.Now - startTime;
                
                Console.WriteLine($"[PRINTER] Discovery failed: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets basic printer information using .NET PrinterSettings
        /// This is fast and works reliably across all Windows versions
        /// </summary>
        private async Task<List<LocalPrinter>> GetBasicPrinterInfoAsync()
        {
            return await Task.Run(() =>
            {
                var printers = new List<LocalPrinter>();
                
                try
                {
                    // Get all installed printers
                    foreach (string printerName in PrinterSettings.InstalledPrinters)
                    {
                        try
                        {
                            var printerSettings = new PrinterSettings { PrinterName = printerName };
                            
                            var printer = new LocalPrinter
                            {
                                Name = printerName,
                                IsDefault = printerSettings.IsDefaultPrinter,
                                Status = printerSettings.IsValid ? PrinterStatus.Ready : PrinterStatus.Offline,
                                LastUpdated = DateTime.Now
                            };
                            
                            // Get basic capabilities
                            printer.Capabilities = GetBasicCapabilities(printerSettings);
                            
                            printers.Add(printer);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[PRINTER] Error getting basic info for {printerName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PRINTER] Error enumerating printers: {ex.Message}");
                }
                
                return printers;
            });
        }
        
        /// <summary>
        /// Gets basic printer capabilities from PrinterSettings
        /// </summary>
        private PrinterCapabilities GetBasicCapabilities(PrinterSettings printerSettings)
        {
            var capabilities = new PrinterCapabilities();
            
            try
            {
                // Check if printer supports color
                capabilities.SupportsColor = printerSettings.SupportsColor;
                
                // Get supported paper sizes
                foreach (System.Drawing.Printing.PaperSize paperSize in printerSettings.PaperSizes)
                {
                    capabilities.SupportedPaperSizes.Add(new Models.PaperSize
                    {
                        Name = paperSize.PaperName,
                        Width = paperSize.Width,
                        Height = paperSize.Height
                    });
                }
                
                // Get supported resolutions
                foreach (PrinterResolution resolution in printerSettings.PrinterResolutions)
                {
                    capabilities.SupportedQualities.Add(new PrintQuality
                    {
                        Name = resolution.Kind.ToString(),
                        DpiX = resolution.X,
                        DpiY = resolution.Y
                    });
                }
                
                // Basic settings
                capabilities.SupportsDuplex = printerSettings.CanDuplex;
                capabilities.SupportsCollation = printerSettings.Collate;
                capabilities.MaxCopies = printerSettings.MaximumCopies;
                
                // Paper sources (trays)
                foreach (PaperSource source in printerSettings.PaperSources)
                {
                    capabilities.Trays.Add(new PrinterTray
                    {
                        Name = source.SourceName
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRINTER] Error getting capabilities for {printerSettings.PrinterName}: {ex.Message}");
            }
            
            return capabilities;
        }
        
        /// <summary>
        /// Enhances printer information with detailed capabilities using WMI
        /// This provides more detailed information but is slower
        /// </summary>
        private async Task<LocalPrinter> EnhancePrinterWithCapabilitiesAsync(LocalPrinter basicPrinter)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Printer WHERE Name = '{basicPrinter.Name.Replace("'", "''")}'");
                    using var results = searcher.Get();
                    
                    foreach (ManagementObject printer in results)
                    {
                        // Enhanced printer information
                        basicPrinter.DriverName = printer["DriverName"]?.ToString() ?? "";
                        basicPrinter.PortName = printer["PortName"]?.ToString() ?? "";
                        basicPrinter.IsNetworkPrinter = (bool)(printer["Network"] ?? false);
                        
                        // Printer status from WMI
                        var printerState = printer["PrinterState"];
                        if (printerState != null)
                        {
                            basicPrinter.Status = MapWmiStatusToPrinterStatus((uint)printerState);
                        }
                        
                        // Additional capability detection could go here
                        // For now, we'll stick with the basic capabilities from PrinterSettings
                        // as WMI capability detection is complex and varies by printer driver
                        
                        break; // We only expect one result
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PRINTER] Warning: WMI enhancement failed for {basicPrinter.Name}: {ex.Message}");
                    // Don't fail - just return the basic printer info
                }
                
                return basicPrinter;
            });
        }
        
        /// <summary>
        /// Maps WMI printer state to our PrinterStatus enum
        /// </summary>
        private PrinterStatus MapWmiStatusToPrinterStatus(uint wmiState)
        {
            return wmiState switch
            {
                0 => PrinterStatus.Idle,
                1 => PrinterStatus.Paused,
                2 => PrinterStatus.Error,
                3 => PrinterStatus.PaperEmpty,
                4 => PrinterStatus.PaperJam,
                5 => PrinterStatus.Offline,
                6 => PrinterStatus.Printing,
                _ => PrinterStatus.Unknown
            };
        }
        
        /// <summary>
        /// Gets the current status of a specific printer
        /// </summary>
        public async Task<PrinterStatus> GetPrinterStatusAsync(string printerName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher($"SELECT PrinterState FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'");
                    using var results = searcher.Get();
                    
                    foreach (ManagementObject printer in results)
                    {
                        var printerState = printer["PrinterState"];
                        if (printerState != null)
                        {
                            return MapWmiStatusToPrinterStatus((uint)printerState);
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PRINTER] Error getting status for {printerName}: {ex.Message}");
                }
                
                return PrinterStatus.Unknown;
            });
        }
        
        /// <summary>
        /// Refreshes the status of all previously discovered printers
        /// This is lighter weight than full discovery
        /// </summary>
        public async Task<List<LocalPrinter>> RefreshPrinterStatusAsync()
        {
            var updatedPrinters = new List<LocalPrinter>();
            
            foreach (var printer in _lastDiscoveredPrinters)
            {
                var updatedPrinter = new LocalPrinter
                {
                    Name = printer.Name,
                    DriverName = printer.DriverName,
                    PortName = printer.PortName,
                    IsDefault = printer.IsDefault,
                    IsNetworkPrinter = printer.IsNetworkPrinter,
                    Capabilities = printer.Capabilities,
                    Status = await GetPrinterStatusAsync(printer.Name),
                    LastUpdated = DateTime.Now
                };
                
                updatedPrinters.Add(updatedPrinter);
            }
            
            // Update cache
            _lastDiscoveredPrinters.Clear();
            _lastDiscoveredPrinters.AddRange(updatedPrinters);
            
            return updatedPrinters;
        }
        
        /// <summary>
        /// Gets the default printer name
        /// </summary>
        public string GetDefaultPrinterName()
        {
            try
            {
                var printerSettings = new PrinterSettings();
                return printerSettings.PrinterName;
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Checks if a specific printer is available
        /// </summary>
        public bool IsPrinterAvailable(string printerName)
        {
            try
            {
                var printerSettings = new PrinterSettings { PrinterName = printerName };
                return printerSettings.IsValid;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the last discovered printers from cache
        /// </summary>
        public List<LocalPrinter> GetCachedPrinters()
        {
            return new List<LocalPrinter>(_lastDiscoveredPrinters);
        }
    }
}
