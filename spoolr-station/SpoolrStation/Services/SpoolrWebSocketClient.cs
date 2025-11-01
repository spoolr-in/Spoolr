using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpoolrStation.WebSocket.Models;
using SpoolrStation.Models;
using SpoolrStation.Configuration;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Real WebSocket client for connecting to Spoolr Core backend
    /// Handles authentication, message routing, and real-time job offers
    /// </summary>
    public class SpoolrWebSocketClient : IDisposable
    {
        private readonly ILogger<SpoolrWebSocketClient> _logger;
        private readonly UserSession _session;
        private readonly string _baseUrl;
        
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _receiveTask;
        private bool _isConnected;
        private bool _disposed;

        // Events
        public event EventHandler<JobOfferReceivedEventArgs>? JobOfferReceived;
        public event EventHandler<JobOfferCancelledEventArgs>? JobOfferCancelled;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

        public SpoolrWebSocketClient(UserSession session, ILogger<SpoolrWebSocketClient>? logger = null, string? baseUrl = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SpoolrWebSocketClient>.Instance;
            
            // Use environment-specific URL or default
            _baseUrl = baseUrl ?? GetWebSocketUrl();
            
            _logger.LogInformation("SpoolrWebSocketClient initialized for vendor {VendorId} with endpoint {Endpoint}", 
                _session.VendorId, _baseUrl);
        }

        public bool IsConnected => _isConnected;

        /// <summary>
        /// Connect to the Spoolr Core WebSocket endpoint
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_disposed) return false;

            try
            {
                _logger.LogInformation("Attempting to connect to Spoolr Core WebSocket...");
                
                // Notify connection attempt
                OnConnectionStatusChanged(WebSocketConnectionState.Disconnected, 
                    WebSocketConnectionState.Connecting, "Connecting to Spoolr Core...");

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Add authentication headers
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_session.JwtToken}");
                _webSocket.Options.SetRequestHeader("X-Vendor-ID", _session.VendorId.ToString());
                _webSocket.Options.SetRequestHeader("X-Business-Name", _session.BusinessName);
                
                // Set timeouts
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                // Build WebSocket URL
                var wsUri = new Uri(_baseUrl);
                
                _logger.LogDebug("Connecting to WebSocket endpoint: {Endpoint}", wsUri);

                // Connect with timeout
                var connectTask = _webSocket.ConnectAsync(wsUri, _cancellationTokenSource.Token);
                var timeoutSeconds = SpoolrConfiguration.GetConnectionTimeout();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), _cancellationTokenSource.Token);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"WebSocket connection timed out after {timeoutSeconds} seconds");
                }

                await connectTask; // Will throw if connection failed

                _isConnected = true;
                
                // Start receive loop
                _receiveTask = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);

                // Notify successful connection
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Connected, "Connected to Spoolr Core");

                // Send initial vendor status
                await SendVendorStatusAsync(true);

                _logger.LogInformation("Successfully connected to Spoolr Core WebSocket");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Spoolr Core WebSocket");
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Failed, $"Connection failed: {ex.Message}");
                
                await CleanupAsync();
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the WebSocket
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isConnected || _disposed) return;

            try
            {
                _logger.LogInformation("Disconnecting from Spoolr Core WebSocket...");

                // Send offline status before disconnecting
                await SendVendorStatusAsync(false);

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Close WebSocket gracefully
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Client disconnect", CancellationToken.None);
                }

                // Wait for receive task to complete
                if (_receiveTask != null)
                {
                    await _receiveTask;
                }

                _isConnected = false;
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connected, 
                    WebSocketConnectionState.Disconnected, "Disconnected from Spoolr Core");

                _logger.LogInformation("Successfully disconnected from Spoolr Core WebSocket");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during WebSocket disconnect");
            }
            finally
            {
                await CleanupAsync();
            }
        }

        /// <summary>
        /// Send job offer response (accept/decline)
        /// </summary>
        public async Task<bool> RespondToJobOfferAsync(long jobId, string response)
        {
            if (!_isConnected || _webSocket == null)
            {
                _logger.LogWarning("Cannot send job response: not connected");
                return false;
            }

            try
            {
                var responseMessage = new
                {
                    type = "JOB_RESPONSE",
                    jobId = jobId,
                    response = response, // "accept" or "decline"
                    vendorId = _session.VendorId,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(responseMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);

                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, 
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                _logger.LogInformation("Sent job response: JobId={JobId}, Response={Response}", jobId, response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job response: JobId={JobId}, Response={Response}", jobId, response);
                return false;
            }
        }

        /// <summary>
        /// Update vendor availability status
        /// </summary>
        public async Task<bool> UpdateVendorStatusAsync(bool isAvailable)
        {
            return await SendVendorStatusAsync(isAvailable);
        }

        /// <summary>
        /// Get health status of the WebSocket connection
        /// </summary>
        public WebSocketHealthStatus GetHealthStatus()
        {
            return new WebSocketHealthStatus
            {
                IsHealthy = _isConnected && _webSocket?.State == WebSocketState.Open,
                Message = _isConnected ? "Connected to Spoolr Core" : "Not connected",
                Details = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "IsConnected", _isConnected },
                    { "WebSocketState", _webSocket?.State.ToString() ?? "None" },
                    { "VendorId", _session.VendorId },
                    { "BusinessName", _session.BusinessName },
                    { "Endpoint", _baseUrl },
                    { "ServiceType", "Production" }
                }
            };
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            try
            {
                while (_isConnected && _webSocket?.State == WebSocketState.Open && 
                       !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        _cancellationTokenSource?.Token ?? CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket close frame received");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuffer.Append(messageChunk);

                        if (result.EndOfMessage)
                        {
                            var message = messageBuffer.ToString();
                            messageBuffer.Clear();
                            
                            await ProcessMessageAsync(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket receive loop cancelled");
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error in receive loop");
                OnConnectionStatusChanged(WebSocketConnectionState.Connected, 
                    WebSocketConnectionState.Failed, $"Connection lost: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WebSocket receive loop");
                OnConnectionStatusChanged(WebSocketConnectionState.Connected, 
                    WebSocketConnectionState.Failed, $"Unexpected error: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
            }
        }

        private async Task ProcessMessageAsync(string message)
        {
            try
            {
                _logger.LogDebug("Received WebSocket message: {Message}", message);

                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement))
                {
                    _logger.LogWarning("Received message without type field: {Message}", message);
                    return;
                }

                var messageType = typeElement.GetString();

                switch (messageType)
                {
                    case "NEW_JOB_OFFER":
                        await HandleJobOfferAsync(root);
                        break;

                    case "OFFER_CANCELLED":
                        await HandleJobCancellationAsync(root);
                        break;

                    case "PING":
                        await HandlePingAsync();
                        break;

                    case "STATUS_UPDATE":
                        await HandleStatusUpdateAsync(root);
                        break;

                    default:
                        _logger.LogWarning("Unknown message type received: {Type}", messageType);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse WebSocket message as JSON: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
            }
        }

        private async Task HandleJobOfferAsync(JsonElement messageElement)
        {
            try
            {
                var jobOffer = JsonSerializer.Deserialize<JobOfferMessage>(messageElement.GetRawText());
                if (jobOffer != null)
                {
                    _logger.LogInformation("Received job offer: JobId={JobId}, Customer={Customer}, Price={Price}", 
                        jobOffer.JobId, jobOffer.DisplayCustomer, jobOffer.FormattedPrice);

                    JobOfferReceived?.Invoke(this, new JobOfferReceivedEventArgs(jobOffer));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process job offer message");
            }

            await Task.CompletedTask;
        }

        private async Task HandleJobCancellationAsync(JsonElement messageElement)
        {
            try
            {
                if (messageElement.TryGetProperty("jobId", out var jobIdElement) &&
                    messageElement.TryGetProperty("message", out var messageElement2))
                {
                    var jobId = jobIdElement.GetInt64();
                    var reason = messageElement2.GetString() ?? "No reason provided";

                    _logger.LogInformation("Job offer cancelled: JobId={JobId}, Reason={Reason}", jobId, reason);

                    JobOfferCancelled?.Invoke(this, new JobOfferCancelledEventArgs(jobId, reason));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process job cancellation message");
            }

            await Task.CompletedTask;
        }

        private async Task HandlePingAsync()
        {
            try
            {
                // Respond to ping with pong
                var pongMessage = new { type = "PONG", timestamp = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(pongMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);

                if (_webSocket != null && _isConnected)
                {
                    await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, 
                        _cancellationTokenSource?.Token ?? CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send pong response");
            }
        }

        private async Task HandleStatusUpdateAsync(JsonElement messageElement)
        {
            try
            {
                // Handle backend status updates
                _logger.LogDebug("Received status update from backend");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process status update message");
            }

            await Task.CompletedTask;
        }

        private async Task<bool> SendVendorStatusAsync(bool isAvailable)
        {
            if (!_isConnected || _webSocket == null) return false;

            try
            {
                var statusMessage = new
                {
                    type = "VENDOR_STATUS",
                    vendorId = _session.VendorId,
                    isAvailable = isAvailable,
                    businessName = _session.BusinessName,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(statusMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);

                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, 
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                _logger.LogInformation("Sent vendor status: Available={Available}", isAvailable);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vendor status");
                return false;
            }
        }

        private void OnConnectionStatusChanged(WebSocketConnectionState previous, 
            WebSocketConnectionState current, string message)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(previous, current, message));
        }

        private string GetWebSocketUrl()
        {
            return SpoolrConfiguration.GetWebSocketEndpoint();
        }

        private async Task CleanupAsync()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource?.Cancel();
                
                _webSocket?.Dispose();
                _webSocket = null;
                
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                if (_receiveTask != null && !_receiveTask.IsCompleted)
                {
                    try
                    {
                        await _receiveTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation token is used
                    }
                }
                _receiveTask = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during WebSocket cleanup");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await DisconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during dispose");
                    }
                });
                
                _disposed = true;
            }
            
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Health status for WebSocket connections
    /// </summary>
    public class WebSocketHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public System.Collections.Generic.Dictionary<string, object> Details { get; set; } = new();
    }
}