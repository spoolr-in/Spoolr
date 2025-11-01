using System;

namespace SpoolrStation.Configuration
{
    /// <summary>
    /// Configuration settings for Spoolr Station connection to Spoolr Core
    /// </summary>
    public static class SpoolrConfiguration
    {
        /// <summary>
        /// Get the WebSocket endpoint URL based on environment
        /// Can be overridden with environment variables
        /// </summary>
        public static string GetWebSocketEndpoint()
        {
            // Check for custom endpoint first
            var customEndpoint = Environment.GetEnvironmentVariable("SPOOLR_WEBSOCKET_ENDPOINT");
            if (!string.IsNullOrEmpty(customEndpoint))
            {
                return customEndpoint;
            }

            // Determine environment
            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            
            return environment.ToLower() switch
            {
                "production" => "wss://api.spoolr.com/ws",
                "staging" => "wss://staging-api.spoolr.com/ws",
                "development" => "ws://localhost:8080/ws",
                "local" => "ws://localhost:8080/ws",
                _ => "ws://localhost:8080/ws"
            };
        }

        /// <summary>
        /// Get the base API URL for REST API calls
        /// </summary>
        public static string GetApiBaseUrl()
        {
            // Check for custom API URL first
            var customUrl = Environment.GetEnvironmentVariable("SPOOLR_API_BASE_URL");
            if (!string.IsNullOrEmpty(customUrl))
            {
                return customUrl;
            }

            // Determine environment
            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            
            return environment.ToLower() switch
            {
                "production" => "https://api.spoolr.com",
                "staging" => "https://staging-api.spoolr.com", 
                "development" => "http://localhost:8080",
                "local" => "http://localhost:8080",
                _ => "http://localhost:8080"
            };
        }

        /// <summary>
        /// Whether to enable detailed logging
        /// </summary>
        public static bool EnableDetailedLogging()
        {
            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            var logLevel = Environment.GetEnvironmentVariable("SPOOLR_LOG_LEVEL");
            
            return environment.ToLower() == "development" || 
                   logLevel?.ToLower() == "debug" ||
                   logLevel?.ToLower() == "trace";
        }

        /// <summary>
        /// Get connection timeout in seconds
        /// </summary>
        public static int GetConnectionTimeout()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("SPOOLR_CONNECTION_TIMEOUT"), out var timeout))
            {
                return Math.Max(5, timeout); // Minimum 5 seconds
            }
            
            return 30; // Default 30 seconds
        }

        /// <summary>
        /// Print current configuration for debugging
        /// </summary>
        public static string GetConfigurationSummary()
        {
            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            var wsEndpoint = GetWebSocketEndpoint();
            var apiUrl = GetApiBaseUrl();
            var timeout = GetConnectionTimeout();
            var detailedLogging = EnableDetailedLogging();

            return $"""
                Spoolr Station Configuration:
                - Environment: {environment}
                - WebSocket Endpoint: {wsEndpoint}
                - API Base URL: {apiUrl}
                - Connection Timeout: {timeout}s
                - Detailed Logging: {detailedLogging}
                """;
        }
    }
}