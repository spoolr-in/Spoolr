using System;
using SpoolrStation.Services.Interfaces;

namespace SpoolrStation.WebSocket.Services
{
    /// <summary>
    /// Configuration settings for WebSocket connections
    /// Contains environment-specific settings and connection parameters
    /// </summary>
    public class WebSocketConfiguration : IWebSocketConfiguration
    {
        /// <summary>
        /// WebSocket endpoint URL (ws://localhost:8080/ws for dev, wss://api.spoolr.com/ws for prod)
        /// </summary>
        public string WebSocketEndpoint { get; set; } = "ws://localhost:8080/ws";

        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 60_000; // 60 seconds

        /// <summary>
        /// Heartbeat interval in milliseconds
        /// </summary>
        public int HeartbeatIntervalMs { get; set; } = 10_000; // 10 seconds

        /// <summary>
        /// Maximum retry attempts for failed connections
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 6;

        /// <summary>
        /// Retry delay intervals (exponential backoff)
        /// </summary>
        public TimeSpan[] RetryDelays { get; set; } = new[]
        {
            TimeSpan.FromSeconds(1),    // Immediate retry
            TimeSpan.FromSeconds(2),    // Quick retry
            TimeSpan.FromSeconds(5),    // Short delay
            TimeSpan.FromSeconds(10),   // Medium delay
            TimeSpan.FromSeconds(20),   // Longer delay
            TimeSpan.FromSeconds(30),   // Maximum delay
            TimeSpan.FromSeconds(60)    // Extended failure
        };

        /// <summary>
        /// Maximum message buffer size during disconnection
        /// </summary>
        public int MaxBufferSize { get; set; } = 1000;

        /// <summary>
        /// Whether to enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Creates default development configuration
        /// </summary>
        /// <returns>Development configuration</returns>
        public static WebSocketConfiguration CreateDevelopment()
        {
            return new WebSocketConfiguration
            {
                WebSocketEndpoint = "ws://localhost:8080/ws",
                ConnectionTimeoutMs = 30_000,
                HeartbeatIntervalMs = 10_000,
                EnableDetailedLogging = true
            };
        }

        /// <summary>
        /// Creates production configuration
        /// </summary>
        /// <returns>Production configuration</returns>
        public static WebSocketConfiguration CreateProduction()
        {
            return new WebSocketConfiguration
            {
                WebSocketEndpoint = "wss://api.spoolr.com/ws",
                ConnectionTimeoutMs = 60_000,
                HeartbeatIntervalMs = 10_000,
                EnableDetailedLogging = false
            };
        }
    }
}