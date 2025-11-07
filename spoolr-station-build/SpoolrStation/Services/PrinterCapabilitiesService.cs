using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SpoolrStation.Models;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service for managing printer capabilities and syncing with the backend
    /// </summary>
    public class PrinterCapabilitiesService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private const string BASE_URL = "http://localhost:8080/api/vendors";

        public PrinterCapabilitiesService(AuthService authService)
        {
            _authService = authService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SpoolrStation/1.0");
        }

        /// <summary>
        /// Sends printer capabilities to the backend
        /// </summary>
        /// <param name="printers">List of discovered printers with capabilities</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<(bool Success, string Message)> SendCapabilitiesToBackendAsync(List<LocalPrinter> printers)
        {
            try
            {
                // Check if we have a valid session
                var session = _authService.CurrentSession;
                if (session == null || !session.IsValid)
                {
                    return (false, "No valid authentication session. Please login first.");
                }

                // Convert printers to backend format
                var backendCapabilities = ConvertToBackendFormat(printers);
                
                // Serialize to JSON
                var capabilitiesJson = JsonSerializer.Serialize(backendCapabilities, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                // Create request
                var request = new PrinterCapabilitiesRequest
                {
                    Capabilities = capabilitiesJson
                };

                var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Add authorization header
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {session.JwtToken}");

                // Send request
                var url = $"{BASE_URL}/{session.VendorId}/update-capabilities";
                Console.WriteLine($"[PRINTER] Sending capabilities to: {url}");
                Console.WriteLine($"[PRINTER] Capabilities payload: {capabilitiesJson}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PRINTER] Capabilities sent successfully");
                    return (true, $"Printer capabilities updated successfully for {printers.Count} printer(s)");
                }
                else
                {
                    Console.WriteLine($"[PRINTER] Failed to send capabilities. Status: {response.StatusCode}, Response: {responseContent}");
                    return (false, $"Failed to update capabilities: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRINTER] Error sending capabilities: {ex.Message}");
                return (false, $"Error sending capabilities: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts local printer list to backend-compatible format
        /// </summary>
        private BackendPrinterCapabilities ConvertToBackendFormat(List<LocalPrinter> printers)
        {
            var backendCapabilities = new BackendPrinterCapabilities
            {
                Printers = printers.Select(ConvertPrinterToBackendFormat).ToList(),
                LastUpdated = DateTime.Now,
                StationVersion = "1.0.0"
            };

            return backendCapabilities;
        }

        /// <summary>
        /// Converts a single printer to backend format
        /// </summary>
        private BackendPrinter ConvertPrinterToBackendFormat(LocalPrinter printer)
        {
            return new BackendPrinter
            {
                Name = printer.Name,
                Driver = printer.DriverName,
                IsDefault = printer.IsDefault,
                IsOnline = printer.IsOnline,
                Status = printer.StatusText,
                SupportsColor = printer.Capabilities.SupportsColor,
                SupportsDuplex = printer.Capabilities.SupportsDuplex,
                SupportedPaperSizes = printer.Capabilities.SupportedPaperSizes.Select(p => p.Name).ToList(),
                SupportedQualities = printer.Capabilities.SupportedQualities.Select(q => q.DisplayName).ToList(),
                MaxCopies = printer.Capabilities.MaxCopies
            };
        }

        /// <summary>
        /// Checks if capabilities have been sent to backend recently
        /// </summary>
        public async Task<bool> AreCapabilitiesUpToDateAsync()
        {
            try
            {
                var settings = await _authService.LoadSettingsAsync();
                return settings.PrinterCapabilitiesSent;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Marks capabilities as sent in local settings
        /// </summary>
        public async Task MarkCapabilitiesAsSentAsync()
        {
            try
            {
                var settings = await _authService.LoadSettingsAsync();
                settings.PrinterCapabilitiesSent = true;
                await _authService.SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRINTER] Error updating capabilities status: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets capabilities sent status (for testing or when printers change significantly)
        /// </summary>
        public async Task ResetCapabilitiesStatusAsync()
        {
            try
            {
                var settings = await _authService.LoadSettingsAsync();
                settings.PrinterCapabilitiesSent = false;
                await _authService.SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRINTER] Error resetting capabilities status: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a summary of printer capabilities for display
        /// </summary>
        public string GetCapabilitiesSummary(List<LocalPrinter> printers)
        {
            if (printers == null || !printers.Any())
                return "No printers detected";

            var onlinePrinters = printers.Count(p => p.IsOnline);
            var colorPrinters = printers.Count(p => p.Capabilities.SupportsColor);
            var duplexPrinters = printers.Count(p => p.Capabilities.SupportsDuplex);
            var totalPaperSizes = printers.SelectMany(p => p.Capabilities.SupportedPaperSizes).Select(p => p.Name).Distinct().Count();

            var summary = $"{printers.Count} printer(s) found";
            if (onlinePrinters < printers.Count)
                summary += $" ({onlinePrinters} online)";
            
            if (colorPrinters > 0)
                summary += $", {colorPrinters} color";
            
            if (duplexPrinters > 0)
                summary += $", {duplexPrinters} duplex";
            
            summary += $", {totalPaperSizes} paper sizes";

            return summary;
        }

        /// <summary>
        /// Validates printer capabilities before sending
        /// </summary>
        public (bool IsValid, string ValidationMessage) ValidateCapabilities(List<LocalPrinter> printers)
        {
            if (printers == null || !printers.Any())
                return (false, "No printers to send");

            var onlinePrinters = printers.Where(p => p.IsOnline).ToList();
            if (!onlinePrinters.Any())
                return (false, "No online printers detected. Please check printer connections.");

            var printersWithCapabilities = printers.Where(p => p.Capabilities.SupportedPaperSizes.Any()).ToList();
            if (!printersWithCapabilities.Any())
                return (false, "No printer capabilities detected. This may indicate driver issues.");

            return (true, $"Ready to send capabilities for {printers.Count} printer(s)");
        }

        /// <summary>
        /// Disposes HTTP client resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
