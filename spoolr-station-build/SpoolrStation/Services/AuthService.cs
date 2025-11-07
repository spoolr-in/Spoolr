using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using SpoolrStation.Models;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service responsible for vendor authentication and local settings management
    /// Handles both first-time activation and regular login flows
    /// </summary>
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _settingsFilePath;
        private const string BASE_URL = "http://localhost:8080/api/vendors";
        
        // In-memory session storage
        public UserSession? CurrentSession { get; private set; }

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SpoolrStation/1.0");
            
            // Settings file path in app data directory
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var spoolrDir = Path.Combine(appDataPath, "Spoolr", "Station");
            Directory.CreateDirectory(spoolrDir);
            _settingsFilePath = Path.Combine(spoolrDir, "settings.json");
        }

        // ======================== AUTHENTICATION METHODS ========================

        /// <summary>
        /// Performs first-time login using activation key and sets new password
        /// Saves store code locally for future logins
        /// </summary>
        /// <param name="activationKey">Activation key from email (e.g., PW-0HHNR3-9S434X)</param>
        /// <param name="newPassword">New password to set</param>
        /// <returns>VendorLoginResponse with complete vendor details</returns>
        public async Task<VendorLoginResponse> FirstTimeLoginAsync(string activationKey, string newPassword)
        {
            try
            {
                var request = new FirstTimeLoginRequest
                {
                    ActivationKey = activationKey.Trim(),
                    NewPassword = newPassword
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BASE_URL}/first-time-login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<VendorLoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null && loginResponse.Success)
                    {
                        // Save store code and mark first-time setup as complete
                        await SaveSettingsAfterFirstTimeLogin(loginResponse);
                        
                        // Create current session
                        CurrentSession = CreateUserSession(loginResponse);
                        
                        return loginResponse;
                    }
                    else
                    {
                        // Success HTTP status but login failed
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = loginResponse?.Message ?? "First-time login failed. Please check your activation key."
                        };
                    }
                }
                else
                {
                    // HTTP error status - try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        });
                        
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = errorResponse?.Message ?? GetHttpErrorMessage(response.StatusCode)
                        };
                    }
                    catch
                    {
                        // If we can't parse the error response, use HTTP status message
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = GetHttpErrorMessage(response.StatusCode)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new VendorLoginResponse
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Performs regular login using store code and password
        /// </summary>
        /// <param name="storeCode">Store code (e.g., PW0002)</param>
        /// <param name="password">Password set during first-time login</param>
        /// <returns>VendorLoginResponse with complete vendor details</returns>
        public async Task<VendorLoginResponse> LoginAsync(string storeCode, string password)
        {
            try
            {
                var request = new VendorLoginRequest
                {
                    StoreCode = storeCode.Trim().ToUpper(),
                    Password = password
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BASE_URL}/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Login Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Login Response Content: {responseContent}");
                Console.WriteLine($"[AUTH] Status: {response.StatusCode}, Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<VendorLoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null && loginResponse.Success)
                    {
                        // Update last login time in settings
                        await UpdateLastLoginTime();
                        
                        // Create current session
                        CurrentSession = CreateUserSession(loginResponse);
                        
                        return loginResponse;
                    }
                    else
                    {
                        // Success HTTP status but login failed
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = loginResponse?.Message ?? "Login failed. Please check your store code and password."
                        };
                    }
                }
                else
                {
                    // HTTP error status - try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        });
                        
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = errorResponse?.Message ?? GetHttpErrorMessage(response.StatusCode)
                        };
                    }
                    catch
                    {
                        // If we can't parse the error response, use HTTP status message
                        return new VendorLoginResponse
                        {
                            Success = false,
                            Message = GetHttpErrorMessage(response.StatusCode)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new VendorLoginResponse
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Refreshes the JWT token using the refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token to use</param>
        /// <returns>AuthResult with refreshed session or error</returns>
        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var request = new { refreshToken = refreshToken };
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BASE_URL}/refresh-token", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refreshResponse = JsonSerializer.Deserialize<VendorLoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (refreshResponse != null && refreshResponse.Success)
                    {
                        // Update current session
                        CurrentSession = CreateUserSession(refreshResponse);
                        
                        return new AuthResult
                        {
                            Success = true,
                            Session = CurrentSession
                        };
                    }
                }

                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Token refresh failed"
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Token refresh error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        /// <returns>JWT token or null if not authenticated</returns>
        public string? GetAuthToken()
        {
            return CurrentSession?.JwtToken;
        }

        /// <summary>
        /// Logs out current user and clears session
        /// </summary>
        public void Logout()
        {
            CurrentSession = null;
        }

        // ======================== SETTINGS MANAGEMENT ========================

        /// <summary>
        /// Loads local app settings from file
        /// </summary>
        /// <returns>StationAppSettings object</returns>
        public async Task<StationAppSettings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<StationAppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    return settings ?? new StationAppSettings();
                }
            }
            catch (Exception)
            {
                // If settings file is corrupted, return default settings
            }

            return new StationAppSettings();
        }

        /// <summary>
        /// Saves app settings to file
        /// </summary>
        /// <param name="settings">Settings to save</param>
        public async Task SaveSettingsAsync(StationAppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // Handle settings save error - could show user notification
            }
        }

        /// <summary>
        /// Checks if first-time setup has been completed
        /// </summary>
        /// <returns>True if setup is complete, false otherwise</returns>
        public async Task<bool> IsFirstTimeSetupCompletedAsync()
        {
            var settings = await LoadSettingsAsync();
            return settings.IsFirstTimeSetupCompleted && !string.IsNullOrEmpty(settings.StoreCode);
        }

        /// <summary>
        /// Gets saved store code for regular login
        /// </summary>
        /// <returns>Store code or empty string if not set</returns>
        public async Task<string> GetSavedStoreCodeAsync()
        {
            var settings = await LoadSettingsAsync();
            return settings.StoreCode;
        }

        // ======================== PRIVATE HELPER METHODS ========================

        /// <summary>
        /// Saves settings after successful first-time login
        /// </summary>
        private async Task SaveSettingsAfterFirstTimeLogin(VendorLoginResponse loginResponse)
        {
            var settings = await LoadSettingsAsync();
            settings.IsFirstTimeSetupCompleted = true;
            settings.StoreCode = loginResponse.StoreCode;
            settings.BusinessName = loginResponse.BusinessName;
            settings.LastLoginAt = DateTime.Now;
            settings.PrinterCapabilitiesSent = false; // Reset for new setup
            
            await SaveSettingsAsync(settings);
        }

        /// <summary>
        /// Updates last login time in settings
        /// </summary>
        private async Task UpdateLastLoginTime()
        {
            var settings = await LoadSettingsAsync();
            settings.LastLoginAt = DateTime.Now;
            await SaveSettingsAsync(settings);
        }

        /// <summary>
        /// Creates user session from login response
        /// </summary>
        private UserSession CreateUserSession(VendorLoginResponse loginResponse)
        {
            return new UserSession
            {
                VendorId = loginResponse.VendorId,
                BusinessName = loginResponse.BusinessName,
                Email = loginResponse.Email,
                ContactPersonName = loginResponse.ContactPersonName,
                StoreCode = loginResponse.StoreCode,
                JwtToken = loginResponse.Token,
                RefreshToken = "", // TODO: Add refresh token when backend supports it
                IsStoreOpen = loginResponse.IsStoreOpen,
                LoginTime = DateTime.Now,
                ExpiresAt = DateTime.UtcNow.AddHours(8), // Default 8 hour expiry
                PrinterCapabilities = loginResponse.PrinterCapabilities,
                UserId = (int)loginResponse.VendorId, // Use VendorId as UserId for now
                Username = loginResponse.Email,
                Role = "VENDOR"
            };
        }

        /// <summary>
        /// Gets user-friendly error message based on HTTP status code
        /// </summary>
        private string GetHttpErrorMessage(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Invalid request. Please check your input.",
                System.Net.HttpStatusCode.Unauthorized => "Invalid credentials. Please check your activation key or password.",
                System.Net.HttpStatusCode.Forbidden => "Access denied. Please check your credentials.",
                System.Net.HttpStatusCode.NotFound => "Service not found. Please ensure the backend is running.",
                System.Net.HttpStatusCode.InternalServerError => "Server error. Please try again later.",
                System.Net.HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable. Please try again later.",
                _ => $"Network error ({statusCode}). Please try again."
            };
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
