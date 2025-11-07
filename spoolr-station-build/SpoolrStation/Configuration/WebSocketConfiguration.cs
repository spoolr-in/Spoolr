using System;

namespace SpoolrStation.Configuration
{
    /// <summary>
    /// Configuration settings for WebSocket connections
    /// </summary>
    public class WebSocketConfiguration
    {
        /// <summary>
        /// Whether to use mock WebSocket service (for testing/demo)
        /// </summary>
        public bool UseMockService { get; set; }

        /// <summary>
        /// WebSocket endpoint URL (if not using environment-based detection)
        /// </summary>
        public string? WebSocketEndpoint { get; set; }

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Keep alive interval in seconds
        /// </summary>
        public int KeepAliveIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum message size in bytes
        /// </summary>
        public int MaxMessageSizeBytes { get; set; } = 8192;

        /// <summary>
        /// Whether to enable detailed WebSocket logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Get configuration based on environment variables and default settings
        /// </summary>
        public static WebSocketConfiguration GetConfiguration()
        {
            var config = new WebSocketConfiguration();

            // Check if we should use mock service
            var useMock = Environment.GetEnvironmentVariable("SPOOLR_USE_MOCK_WEBSOCKET");
            config.UseMockService = !string.IsNullOrEmpty(useMock) && 
                                   (useMock.ToLower() == "true" || useMock == "1");

            // Get custom WebSocket endpoint
            config.WebSocketEndpoint = Environment.GetEnvironmentVariable("SPOOLR_WEBSOCKET_ENDPOINT");

            // Get timeout settings
            if (int.TryParse(Environment.GetEnvironmentVariable("SPOOLR_WS_TIMEOUT"), out var timeout))
            {
                config.ConnectionTimeoutSeconds = timeout;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("SPOOLR_WS_KEEPALIVE"), out var keepAlive))
            {
                config.KeepAliveIntervalSeconds = keepAlive;
            }

            // Enable detailed logging in development
            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            config.EnableDetailedLogging = environment.ToLower() == "development";

            return config;
        }

        /// <summary>
        /// Get WebSocket endpoint URL based on environment
        /// </summary>
        public string GetWebSocketEndpoint()
        {
            if (!string.IsNullOrEmpty(WebSocketEndpoint))
            {
                return WebSocketEndpoint;
            }

            var environment = Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development";
            
            return environment.ToLower() switch
            {
                "production" => "wss://api.spoolr.com/ws",
                "staging" => "wss://staging-api.spoolr.com/ws", 
                "development" => "ws://localhost:8080/ws",
                _ => "ws://localhost:8080/ws"
            };
        }

        /// <summary>
        /// Create a development configuration (uses mock service)
        /// </summary>
        public static WebSocketConfiguration CreateDevelopmentConfig()
        {
            return new WebSocketConfiguration
            {
                UseMockService = true,
                EnableDetailedLogging = true,
                ConnectionTimeoutSeconds = 10,
                KeepAliveIntervalSeconds = 15
            };
        }

        /// <summary>
        /// Create a production configuration (uses real WebSocket)
        /// </summary>
        public static WebSocketConfiguration CreateProductionConfig(string? customEndpoint = null)
        {
            return new WebSocketConfiguration
            {
                UseMockService = false,
                WebSocketEndpoint = customEndpoint,
                EnableDetailedLogging = false,
                ConnectionTimeoutSeconds = 30,
                KeepAliveIntervalSeconds = 30
            };
        }
    }
}