using System;
using System.ComponentModel.DataAnnotations;

namespace SpoolrStation.Models
{
    // ======================== REQUEST MODELS ========================
    
    /// <summary>
    /// Request model for first-time login using activation key
    /// Used when vendor initially logs in with activation key from email
    /// </summary>
    public class FirstTimeLoginRequest
    {
        [Required]
        public string ActivationKey { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for regular login using store code and password
    /// Used for all subsequent logins after first-time setup
    /// </summary>
    public class VendorLoginRequest
    {
        [Required]
        public string StoreCode { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // ======================== RESPONSE MODELS ========================
    
    /// <summary>
    /// Response model for both first-time and regular vendor login
    /// Contains complete vendor information and JWT token
    /// </summary>
    public class VendorLoginResponse
    {
        // Essential vendor info
        public long VendorId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Store status
        public bool IsStoreOpen { get; set; }
        public bool StationAppConnected { get; set; }
        public DateTime? StoreStatusUpdatedAt { get; set; }

        // QR Code info - CRITICAL: Store code returned here for local storage
        public string StoreCode { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;

        // Pricing info
        public decimal PricePerPageBWSingleSided { get; set; }
        public decimal PricePerPageBWDoubleSided { get; set; }
        public decimal PricePerPageColorSingleSided { get; set; }
        public decimal PricePerPageColorDoubleSided { get; set; }

        // Printer capabilities (JSON string)
        public string PrinterCapabilities { get; set; } = string.Empty;

        // Login info
        public string Message { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }

        // Password status
        public bool PasswordSet { get; set; }

        // JWT Token for API authentication
        public string Token { get; set; } = string.Empty;

        // Success indicator
        public bool Success { get; set; }
    }

    /// <summary>
    /// Error response model for failed authentication requests
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
    }

    // ======================== LOCAL STORAGE MODELS ========================
    
    /// <summary>
    /// Local settings and state for the Station app
    /// Persisted locally to track setup progress and store credentials
    /// </summary>
    public class StationAppSettings
    {
        /// <summary>
        /// Whether the initial setup (first-time login) has been completed
        /// </summary>
        public bool IsFirstTimeSetupCompleted { get; set; }

        /// <summary>
        /// Store code received from first-time login, used for subsequent logins
        /// Only populated after successful first-time login
        /// </summary>
        public string StoreCode { get; set; } = string.Empty;

        /// <summary>
        /// Last successful login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Business name for display purposes
        /// </summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Whether printer capabilities have been sent to backend
        /// </summary>
        public bool PrinterCapabilitiesSent { get; set; }
    }

    /// <summary>
    /// Current user session information
    /// Holds active session data while app is running
    /// </summary>
    public class UserSession
    {
        public long VendorId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string StoreCode { get; set; } = string.Empty;
        public string JwtToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool IsStoreOpen { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string PrinterCapabilities { get; set; } = string.Empty;
        
        // Additional properties for enhanced authentication management
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "VENDOR";

        public bool IsValid => !string.IsNullOrEmpty(JwtToken) && VendorId > 0 && ExpiresAt > DateTime.UtcNow;
    }

    /// <summary>
    /// Result of authentication operations (login, refresh, etc.)
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public UserSession? Session { get; set; }
    }
}
