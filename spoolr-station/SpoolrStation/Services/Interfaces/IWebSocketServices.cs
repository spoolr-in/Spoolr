using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive;
using SpoolrStation.Models;

namespace SpoolrStation.Services.Interfaces
{
    // ======================== LAYER 1: WEBSOCKET TRANSPORT ========================

    /// <summary>
    /// Interface for WebSocket transport layer providing raw WebSocket connection management
    /// Handles connection establishment, maintenance, and basic messaging
    /// </summary>
    public interface IWebSocketTransport : IDisposable
    {
        /// <summary>
        /// Establishes WebSocket connection to the specified endpoint
        /// </summary>
        /// <param name="endpoint">WebSocket endpoint URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync(Uri endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects the WebSocket connection gracefully
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Sends a raw string message over WebSocket
        /// </summary>
        /// <param name="message">Message to send</param>
        Task SendAsync(string message);

        /// <summary>
        /// Observable stream of received messages
        /// </summary>
        IObservable<string> MessageReceived { get; }

        /// <summary>
        /// Observable stream of connection state changes
        /// </summary>
        IObservable<WebSocketConnectionState> ConnectionStateChanged { get; }

        /// <summary>
        /// Current WebSocket connection state
        /// </summary>
        WebSocketConnectionState CurrentState { get; }

        /// <summary>
        /// Whether the transport is currently connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Last error that occurred (if any)
        /// </summary>
        Exception? LastError { get; }
    }

    // ======================== LAYER 2: STOMP PROTOCOL ========================

    /// <summary>
    /// Interface for STOMP protocol handler over WebSocket transport
    /// Manages STOMP frames, subscriptions, and protocol-level communication
    /// </summary>
    public interface IStompProtocolHandler : IDisposable
    {
        /// <summary>
        /// Connects to STOMP server with authentication
        /// </summary>
        /// <param name="host">STOMP host</param>
        /// <param name="login">Login credential (JWT token)</param>
        /// <param name="passcode">Passcode (can be empty for JWT)</param>
        Task<bool> ConnectAsync(string host, string login, string passcode = "");

        /// <summary>
        /// Subscribes to a STOMP destination
        /// </summary>
        /// <param name="destination">Destination to subscribe to (e.g., /queue/job-offers-123)</param>
        /// <param name="messageHandler">Handler for received messages</param>
        Task<string> SubscribeAsync(string destination, Action<StompFrame> messageHandler);

        /// <summary>
        /// Unsubscribes from a STOMP destination
        /// </summary>
        /// <param name="subscriptionId">Subscription ID returned by SubscribeAsync</param>
        Task UnsubscribeAsync(string subscriptionId);

        /// <summary>
        /// Sends a message to a STOMP destination
        /// </summary>
        /// <param name="destination">Destination to send to</param>
        /// <param name="body">Message body</param>
        /// <param name="headers">Additional headers</param>
        Task SendAsync(string destination, string body, Dictionary<string, string>? headers = null);

        /// <summary>
        /// Disconnects from STOMP server
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Whether STOMP connection is established
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Observable stream of connection state changes
        /// </summary>
        IObservable<bool> ConnectionStateChanged { get; }

        /// <summary>
        /// Observable stream of STOMP errors
        /// </summary>
        IObservable<StompFrame> ErrorReceived { get; }
    }

    // ======================== LAYER 3: MESSAGE ROUTING ========================

    /// <summary>
    /// Interface for Spoolr-specific message routing and processing
    /// Handles job offer lifecycle, validation, and business logic
    /// </summary>
    public interface ISpoolrMessageRouter : IDisposable
    {
        /// <summary>
        /// Event fired when a new job offer is received
        /// </summary>
        event EventHandler<JobOfferReceivedEventArgs> JobOfferReceived;

        /// <summary>
        /// Event fired when a job offer is cancelled
        /// </summary>
        event EventHandler<JobOfferCancelledEventArgs> JobOfferCancelled;

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Event fired when a message processing error occurs
        /// </summary>
        event EventHandler<MessageErrorEventArgs> MessageError;

        /// <summary>
        /// Starts the message router with vendor authentication
        /// </summary>
        /// <param name="vendorId">Vendor ID for message routing</param>
        /// <param name="authToken">JWT authentication token</param>
        Task<bool> StartAsync(long vendorId, string authToken);

        /// <summary>
        /// Stops the message router and closes connections
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Whether the router is currently running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Current vendor ID
        /// </summary>
        long? CurrentVendorId { get; }

        /// <summary>
        /// Connection health status
        /// </summary>
        bool IsHealthy { get; }
    }

    // ======================== LAYER 4: JOB OFFER MANAGEMENT ========================

    /// <summary>
    /// Interface for job offer lifecycle management
    /// Handles offer tracking, expiry, and state management
    /// </summary>
    public interface IJobOfferManager : IDisposable
    {
        /// <summary>
        /// Adds a new job offer to active tracking
        /// </summary>
        /// <param name="offer">Job offer message</param>
        /// <returns>True if offer was added successfully</returns>
        Task<bool> AddJobOfferAsync(JobOfferMessage offer);

        /// <summary>
        /// Cancels an active job offer
        /// </summary>
        /// <param name="jobId">Job ID to cancel</param>
        /// <param name="reason">Reason for cancellation</param>
        /// <returns>True if offer was cancelled successfully</returns>
        Task<bool> CancelJobOfferAsync(long jobId, string reason);

        /// <summary>
        /// Gets an active job offer by ID
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns>Job offer if found, null otherwise</returns>
        Task<JobOfferMessage?> GetActiveOfferAsync(long jobId);

        /// <summary>
        /// Gets all currently active job offers
        /// </summary>
        /// <returns>List of active job offers</returns>
        Task<List<JobOfferMessage>> GetActiveOffersAsync();

        /// <summary>
        /// Manually triggers cleanup of expired offers
        /// </summary>
        Task CleanupExpiredOffersAsync();

        /// <summary>
        /// Event fired when a job offer expires
        /// </summary>
        event EventHandler<JobOfferCancelledEventArgs> OfferExpired;

        /// <summary>
        /// Number of currently active offers
        /// </summary>
        int ActiveOfferCount { get; }
    }

    // ======================== LAYER 5: MESSAGE VALIDATION ========================

    /// <summary>
    /// Interface for message validation and security
    /// </summary>
    public interface IMessageValidator
    {
        /// <summary>
        /// Validates a job offer message
        /// </summary>
        /// <param name="message">Message to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateJobOffer(JobOfferMessage message);

        /// <summary>
        /// Validates a job cancellation message
        /// </summary>
        /// <param name="message">Message to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateJobCancellation(JobOfferCancelledMessage message);

        /// <summary>
        /// Validates a STOMP frame for security issues
        /// </summary>
        /// <param name="frame">STOMP frame to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateStompFrame(StompFrame frame);
    }

    // ======================== HIGH-LEVEL WEBSOCKET CLIENT ========================

    /// <summary>
    /// High-level WebSocket client interface for Spoolr Station
    /// Combines all layers into a simple, easy-to-use interface
    /// </summary>
    public interface ISpoolrWebSocketClient : IDisposable
    {
        /// <summary>
        /// Event fired when a new job offer is received
        /// </summary>
        event EventHandler<JobOfferReceivedEventArgs> JobOfferReceived;

        /// <summary>
        /// Event fired when a job offer is cancelled
        /// </summary>
        event EventHandler<JobOfferCancelledEventArgs> JobOfferCancelled;

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Connects to Spoolr backend with vendor credentials
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="authToken">JWT authentication token</param>
        Task<bool> ConnectAsync(long vendorId, string authToken);

        /// <summary>
        /// Disconnects from Spoolr backend
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Gets all currently active job offers
        /// </summary>
        /// <returns>List of active job offers</returns>
        Task<List<JobOfferMessage>> GetActiveOffersAsync();

        /// <summary>
        /// Whether the client is connected to the backend
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Current vendor ID
        /// </summary>
        long? VendorId { get; }

        /// <summary>
        /// Connection health status
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Last connection error (if any)
        /// </summary>
        Exception? LastError { get; }
    }

    // ======================== CONFIGURATION INTERFACES ========================

    /// <summary>
    /// Configuration settings for WebSocket connections
    /// </summary>
    public interface IWebSocketConfiguration
    {
        /// <summary>
        /// WebSocket endpoint URL (ws://localhost:8080/ws for dev, wss://api.spoolr.com/ws for prod)
        /// </summary>
        string WebSocketEndpoint { get; }

        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        int ConnectionTimeoutMs { get; }

        /// <summary>
        /// Heartbeat interval in milliseconds
        /// </summary>
        int HeartbeatIntervalMs { get; }

        /// <summary>
        /// Maximum retry attempts for failed connections
        /// </summary>
        int MaxRetryAttempts { get; }

        /// <summary>
        /// Retry delay intervals (exponential backoff)
        /// </summary>
        TimeSpan[] RetryDelays { get; }

        /// <summary>
        /// Maximum message buffer size during disconnection
        /// </summary>
        int MaxBufferSize { get; }

        /// <summary>
        /// Whether to enable detailed logging
        /// </summary>
        bool EnableDetailedLogging { get; }
    }
}