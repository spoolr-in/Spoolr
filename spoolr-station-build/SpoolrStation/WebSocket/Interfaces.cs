using System;
using System.Threading.Tasks;
using SpoolrStation.WebSocket.Models;

namespace SpoolrStation.WebSocket.Services
{
    // Simplified interfaces for WebSocket functionality
    
    public interface IWebSocketConfiguration
    {
        string WebSocketEndpoint { get; }
        bool EnableDetailedLogging { get; }
        TimeSpan[] RetryDelays { get; }
    }

    public interface IWebSocketTransport
    {
        Task<bool> ConnectAsync(Uri endpoint, System.Threading.CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        Task SendAsync(string message);
        WebSocketConnectionState CurrentState { get; }
        bool IsConnected { get; }
    }

    public interface IStompProtocolHandler
    {
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task SubscribeAsync(string destination);
        Task SendAsync(string destination, string message);
    }

    public interface IMessageValidator
    {
        ValidationResult ValidateMessage(string message);
    }

    public interface ISpoolrWebSocketClient : IDisposable
    {
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        event EventHandler<JobOfferReceivedEventArgs> JobOfferReceived;
        event EventHandler<JobOfferCancelledEventArgs> JobOfferCancelled;
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
    }
}