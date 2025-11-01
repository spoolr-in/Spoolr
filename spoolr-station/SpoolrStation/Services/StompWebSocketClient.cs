using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpoolrStation.WebSocket.Models;
using SpoolrStation.Models;
using SpoolrStation.Configuration;
using SpoolrStation.Services.Core;

namespace SpoolrStation.Services
{
    /// <summary>
    /// STOMP-based WebSocket client for connecting to Spoolr Core backend
    /// Implements STOMP protocol over WebSocket as expected by the backend
    /// </summary>
    /// <summary>
    /// Connection states for robust connection management
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed,
        Disposed
    }

    public class StompWebSocketClient : IDisposable
    {
        private readonly ILogger<StompWebSocketClient> _logger;
        private readonly UserSession _session;
        private readonly string _baseUrl;
        private readonly AuthenticationStateManager _authStateManager;
        
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _receiveTask;
        private Task? _heartbeatTask;
        private bool _isConnected;
        private bool _disposed;
        private string? _sessionId;
        private readonly Dictionary<string, string> _subscriptions = new();
        
        // Connection resilience fields
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 5;
        private readonly System.Timers.Timer _heartbeatTimer;
        private DateTime _lastHeartbeat = DateTime.UtcNow;
        private readonly object _connectionLock = new object();
        private bool _isReconnecting = false;

        // Events
        public event EventHandler<JobOfferReceivedEventArgs>? JobOfferReceived;
        public event EventHandler<JobOfferCancelledEventArgs>? JobOfferCancelled;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

        public StompWebSocketClient(UserSession session, ILogger<StompWebSocketClient>? logger = null, string? baseUrl = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StompWebSocketClient>.Instance;
            _authStateManager = Services.ServiceProvider.GetAuthenticationStateManager();
            
            // Use environment-specific URL or default
            _baseUrl = baseUrl ?? GetWebSocketUrl();
            
            // Initialize heartbeat timer (30 second intervals)
            _heartbeatTimer = new System.Timers.Timer(30000);
            _heartbeatTimer.Elapsed += OnHeartbeatTimer;
            _heartbeatTimer.AutoReset = true;
            
            _logger.LogInformation("StompWebSocketClient initialized for vendor {VendorId} with endpoint {Endpoint}", 
                _session.VendorId, _baseUrl);
        }

        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Current connection state
        /// </summary>
        public ConnectionState CurrentConnectionState => _connectionState;
        
        /// <summary>
        /// Number of reconnection attempts made
        /// </summary>
        public int ReconnectAttempts => _reconnectAttempts;
        
        /// <summary>
        /// Heartbeat timer event handler - monitors connection health
        /// </summary>
        private async void OnHeartbeatTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_connectionState == ConnectionState.Connected)
                {
                    // Check if connection is still alive
                    var timeSinceLastHeartbeat = DateTime.UtcNow - _lastHeartbeat;
                    
                    if (timeSinceLastHeartbeat.TotalMinutes > 2) // No activity for 2 minutes
                    {
                        _logger.LogWarning("Connection appears dead - no heartbeat for {Minutes} minutes", timeSinceLastHeartbeat.TotalMinutes);
                        await TriggerReconnectionAsync("Heartbeat timeout");
                    }
                    else
                    {
                        // Send ping to keep connection alive
                        await SendHeartbeatPingAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat timer");
            }
        }

        /// <summary>
        /// Connect to the Spoolr Core WebSocket endpoint using STOMP protocol
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_disposed) return false;

            lock (_connectionLock)
            {
                if (_connectionState == ConnectionState.Connecting || _connectionState == ConnectionState.Connected)
                {
                    _logger.LogInformation("Connection already in progress or established");
                    return _connectionState == ConnectionState.Connected;
                }
                _connectionState = ConnectionState.Connecting;
            }

            try
            {
                _logger.LogInformation("Attempting STOMP connection to Spoolr Core WebSocket (Attempt {Attempt})...", _reconnectAttempts + 1);
                
                // Notify connection attempt
                OnConnectionStatusChanged(WebSocketConnectionState.Disconnected, 
                    WebSocketConnectionState.Connecting, "Connecting to Spoolr Core...");

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Set timeouts
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                // THE FIX: Add the JWT token to the initial handshake request header
                var freshToken = await _authStateManager.GetValidTokenAsync();
                if (!string.IsNullOrEmpty(freshToken))
                {
                    _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {freshToken}");
                    _logger.LogInformation("Authorization header set for WebSocket handshake.");
                }
                else
                {
                    _logger.LogError("Cannot connect WebSocket: No valid token available for handshake.");
                    throw new InvalidOperationException("Authentication token is required for WebSocket connection");
                }

                // Build WebSocket URL
                var wsUri = new Uri(_baseUrl);
                
                _logger.LogInformation("Attempting WebSocket connection to: {Endpoint}", wsUri);
                _logger.LogInformation("WebSocket configuration: Environment={Environment}, UseMock={UseMock}", 
                    Environment.GetEnvironmentVariable("SPOOLR_ENVIRONMENT") ?? "development",
                    Environment.GetEnvironmentVariable("SPOOLR_USE_MOCK_WEBSOCKET"));

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

                // Start receive loop first
                _receiveTask = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);

                // Send STOMP CONNECT frame
                await SendStompConnectAsync();

                // Wait for connection confirmation (CONNECTED frame)
                var connectionWait = Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token);
                while (!_isConnected && !connectionWait.IsCompleted)
                {
                    await Task.Delay(100);
                }

                if (!_isConnected)
                {
                    throw new TimeoutException("STOMP connection handshake timed out");
                }

                // Subscribe to vendor-specific job offer queue
                await SubscribeToJobOffers();
                
                // Update connection state and reset reconnection attempts
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Connected;
                    _reconnectAttempts = 0;
                }
                
                // Start heartbeat monitoring
                _lastHeartbeat = DateTime.UtcNow;
                _heartbeatTimer.Start();

                _logger.LogInformation("Successfully connected to Spoolr Core via STOMP");
                return true;
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "WebSocket connection failed (Attempt {Attempt}): {Error}", _reconnectAttempts + 1, wsEx.WebSocketErrorCode);
                
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Failed;
                    _reconnectAttempts++;
                }
                
                var errorMessage = wsEx.WebSocketErrorCode switch
                {
                    WebSocketError.ConnectionClosedPrematurely => "Connection closed unexpectedly by server",
                    WebSocketError.InvalidState => "WebSocket in invalid state",
                    WebSocketError.NotAWebSocket => "Server did not accept WebSocket upgrade",
                    WebSocketError.UnsupportedProtocol => "WebSocket protocol version not supported",
                    WebSocketError.UnsupportedVersion => "WebSocket version not supported",
                    _ => $"WebSocket error: {wsEx.Message}"
                };
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Failed, errorMessage);
                
                await CleanupAsync();
                
                // Attempt automatic reconnection if not at max attempts
                if (_reconnectAttempts < MaxReconnectAttempts && !_disposed)
                {
                    _ = Task.Run(async () => await DelayedReconnectAsync());
                }
                
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                _logger.LogError(timeoutEx, "WebSocket connection timeout (Attempt {Attempt})", _reconnectAttempts + 1);
                
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Failed;
                    _reconnectAttempts++;
                }
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Failed, "Connection timeout - server may be unreachable");
                
                await CleanupAsync();
                
                if (_reconnectAttempts < MaxReconnectAttempts && !_disposed)
                {
                    _ = Task.Run(async () => await DelayedReconnectAsync());
                }
                
                return false;
            }
            catch (InvalidOperationException authEx) when (authEx.Message.Contains("Authentication"))
            {
                _logger.LogError(authEx, "Authentication failed for WebSocket connection (Attempt {Attempt})", _reconnectAttempts + 1);
                
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Failed;
                    _reconnectAttempts++;
                }
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Failed, "Authentication failed - please re-login");
                
                await CleanupAsync();
                
                // Don't auto-retry authentication errors
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error connecting to WebSocket (Attempt {Attempt}): {ErrorType}", _reconnectAttempts + 1, ex.GetType().Name);
                
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Failed;
                    _reconnectAttempts++;
                }
                
                OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                    WebSocketConnectionState.Failed, $"Unexpected error: {ex.Message}");
                
                await CleanupAsync();
                
                // Attempt automatic reconnection if not at max attempts
                if (_reconnectAttempts < MaxReconnectAttempts && !_disposed)
                {
                    _ = Task.Run(async () => await DelayedReconnectAsync());
                }
                
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
                
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Disconnected;
                }
                
                // Stop heartbeat timer
                _heartbeatTimer.Stop();

                // Send STOMP DISCONNECT frame
                await SendStompDisconnectAsync();

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Close WebSocket gracefully
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Client disconnect", CancellationToken.None);
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
        /// Update vendor availability status
        /// </summary>
        public async Task<bool> UpdateVendorStatusAsync(bool isAvailable)
        {
            // Send vendor status message to backend via STOMP
            return await SendVendorStatusMessage(isAvailable);
        }
        
        /// <summary>
        /// Trigger reconnection due to connection failure
        /// </summary>
        public async Task TriggerReconnectionAsync(string reason)
        {
            if (_isReconnecting || _disposed) return;
            
            lock (_connectionLock)
            {
                if (_isReconnecting) return;
                _isReconnecting = true;
                _connectionState = ConnectionState.Reconnecting;
            }
            
            _logger.LogWarning("Connection lost ({Reason}), attempting reconnection...", reason);
            
            OnConnectionStatusChanged(WebSocketConnectionState.Connected, 
                WebSocketConnectionState.Disconnected, $"Connection lost: {reason}");
            
            // Stop heartbeat timer during reconnection
            _heartbeatTimer.Stop();
            
            // Clean up current connection
            await CleanupAsync();
            
            // Attempt reconnection
            await DelayedReconnectAsync();
            
            _isReconnecting = false;
        }
        
        /// <summary>
        /// Perform delayed reconnection with exponential backoff
        /// </summary>
        private async Task DelayedReconnectAsync()
        {
            if (_disposed) return;
            
            // Exponential backoff: 2, 4, 8, 16, 32 seconds
            var delay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(_reconnectAttempts, 5)));
            
            _logger.LogInformation("Waiting {Delay} seconds before reconnection attempt {Attempt}", 
                delay.TotalSeconds, _reconnectAttempts + 1);
            
            await Task.Delay(delay);
            
            if (!_disposed)
            {
                await ConnectAsync();
            }
        }
        
        /// <summary>
        /// Send heartbeat ping to keep connection alive
        /// </summary>
        private async Task SendHeartbeatPingAsync()
        {
            if (_webSocket?.State != WebSocketState.Open) return;
            
            try
            {
                var pingFrame = new StringBuilder();
                pingFrame.AppendLine("SEND");
                pingFrame.AppendLine("destination:/app/ping");
                pingFrame.AppendLine("content-type:application/json");
                pingFrame.AppendLine();
                pingFrame.Append($"{{\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}");
                pingFrame.Append('\0');
                
                await SendRawFrameAsync(pingFrame.ToString());
                _lastHeartbeat = DateTime.UtcNow;
                
                _logger.LogDebug("Heartbeat ping sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send heartbeat ping");
                await TriggerReconnectionAsync("Heartbeat ping failed");
            }
        }

        /// <summary>
        /// Send job offer response (accept/decline) using REST API
        /// </summary>
        public async Task<bool> RespondToJobOfferAsync(long jobId, string response)
        {
            try
            {
                System.Console.WriteLine($"=== REST API JOB RESPONSE ===");
                
                // Add null checks to prevent InvalidOperationException
                if (_session == null)
                {
                    _logger.LogError("Session is null - cannot respond to job offer");
                    System.Console.WriteLine($"ERROR: Session is null!");
                    return false;
                }
                
                if (string.IsNullOrEmpty(_session.JwtToken))
                {
                    _logger.LogError("Session JWT token is null or empty - cannot respond to job offer");
                    System.Console.WriteLine($"ERROR: JWT token is null or empty!");
                    return false;
                }
                
                System.Console.WriteLine($"JobId: {jobId}, Response: {response}, VendorId: {_session.VendorId}");
                
                // Use REST API endpoints as per PROJECT_DESCRIPTION.md
                string endpoint;
                if (response.ToLower() == "accept")
                {
                    endpoint = $"/api/jobs/{jobId}/accept";
                }
                else if (response.ToLower() == "decline" || response.ToLower() == "reject")
                {
                    endpoint = $"/api/jobs/{jobId}/reject";
                }
                else
                {
                    _logger.LogError("Invalid job response: {Response}. Must be 'accept' or 'decline/reject'", response);
                    return false;
                }
                
                System.Console.WriteLine($"Using REST endpoint: {endpoint}");
                
                // Create HTTP client for API call
                using var httpClient = new HttpClient();
                
                // Get base URL from configuration (remove /ws suffix for REST API)
                var baseUrl = GetApiBaseUrl();
                var fullUrl = $"{baseUrl}{endpoint}";
                
                System.Console.WriteLine($"Full URL: {fullUrl}");
                
                // Get fresh token and add JWT authorization header
                try
                {
                    var freshToken = await _authStateManager.GetValidTokenAsync();
                    if (string.IsNullOrEmpty(freshToken))
                    {
                        _logger.LogError("No valid authentication token available for job response");
                        System.Console.WriteLine($"ERROR: No valid authentication token available");
                        return false;
                    }
                    
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", freshToken);
                    System.Console.WriteLine($"Authorization header set successfully with fresh token");
                }
                catch (Exception authEx)
                {
                    _logger.LogError(authEx, "Failed to set authorization header");
                    System.Console.WriteLine($"ERROR setting auth header: {authEx.Message}");
                    return false;
                }
                
                // Make POST request
                var httpResponse = await httpClient.PostAsync(fullUrl, null);
                
                System.Console.WriteLine($"HTTP Response Status: {httpResponse.StatusCode}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"Response Content: {responseContent}");
                    
                    _logger.LogInformation("Successfully sent job response via REST API: JobId={JobId}, Response={Response}", jobId, response);
                    System.Console.WriteLine($"=== JOB RESPONSE SUCCESS ===");
                    return true;
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send job response via REST API: Status={Status}, Content={Content}", httpResponse.StatusCode, errorContent);
                    System.Console.WriteLine($"ERROR: {httpResponse.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during job response API call: JobId={JobId}, Response={Response}", jobId, response);
                System.Console.WriteLine($"EXCEPTION: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update job status to PRINTING using REST API
        /// POST /api/jobs/{jobId}/print
        /// </summary>
        public async Task<bool> UpdateJobStatusToPrintingAsync(long jobId)
        {
            return await UpdateJobStatusAsync(jobId, "print", "PRINTING");
        }

        /// <summary>
        /// Update job status to READY using REST API
        /// POST /api/jobs/{jobId}/ready
        /// </summary>
        public async Task<bool> UpdateJobStatusToReadyAsync(long jobId)
        {
            return await UpdateJobStatusAsync(jobId, "ready", "READY");
        }

        /// <summary>
        /// Update job status to COMPLETED using REST API
        /// POST /api/jobs/{jobId}/complete
        /// </summary>
        public async Task<bool> UpdateJobStatusToCompletedAsync(long jobId)
        {
            return await UpdateJobStatusAsync(jobId, "complete", "COMPLETED");
        }

        /// <summary>
        /// Generic method to update job status via REST API
        /// </summary>
        private async Task<bool> UpdateJobStatusAsync(long jobId, string action, string statusName)
        {
            try
            {
                _logger.LogInformation("Updating job {JobId} status to {Status}", jobId, statusName);

                if (_session == null || string.IsNullOrEmpty(_session.JwtToken))
                {
                    _logger.LogError("Session or JWT token is null - cannot update job status");
                    return false;
                }

                using var httpClient = new HttpClient();
                var baseUrl = GetApiBaseUrl();
                var endpoint = $"/api/jobs/{jobId}/{action}";
                var fullUrl = $"{baseUrl}{endpoint}";

                _logger.LogInformation("Calling status update endpoint: {Endpoint}", fullUrl);

                // Get fresh token and add JWT authorization header
                var freshToken = await _authStateManager.GetValidTokenAsync();
                if (string.IsNullOrEmpty(freshToken))
                {
                    _logger.LogError("No valid authentication token available for status update");
                    return false;
                }

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", freshToken);

                // Make POST request
                var httpResponse = await httpClient.PostAsync(fullUrl, null);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully updated job {JobId} to {Status}: {Response}", jobId, statusName, responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update job {JobId} to {Status}: {StatusCode} - {Error}", 
                        jobId, statusName, httpResponse.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating job {JobId} to {Status}", jobId, statusName);
                return false;
            }
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
                Details = new Dictionary<string, object>
                {
                    { "IsConnected", _isConnected },
                    { "WebSocketState", _webSocket?.State.ToString() ?? "None" },
                    { "VendorId", _session.VendorId },
                    { "BusinessName", _session.BusinessName },
                    { "Endpoint", _baseUrl },
                    { "Protocol", "STOMP" },
                    { "SessionId", _sessionId ?? "None" },
                    { "ServiceType", "Production" }
                }
            };
        }

        private async Task SendStompConnectAsync()
        {
            _logger.LogInformation("Starting STOMP authentication process...");
            
            // Get fresh token from AuthenticationStateManager
            var freshToken = await _authStateManager.GetValidTokenAsync();
            
            _logger.LogInformation("Authentication token retrieved: {TokenExists}", !string.IsNullOrEmpty(freshToken));
            
            if (string.IsNullOrEmpty(freshToken))
            {
                _logger.LogError("No valid authentication token available for WebSocket connection");
                
                // Try to use session token directly as fallback
                if (!string.IsNullOrEmpty(_session.JwtToken))
                {
                    _logger.LogWarning("Using session JWT token as fallback");
                    freshToken = _session.JwtToken;
                }
                else
                {
                    throw new InvalidOperationException("Authentication token is required for WebSocket connection");
                }
            }
            
            var connectFrame = new StringBuilder();
            connectFrame.AppendLine("CONNECT");
            connectFrame.AppendLine("accept-version:1.2");
            connectFrame.AppendLine("host:localhost");
            connectFrame.AppendLine($"login:{_session.VendorId}");
            connectFrame.AppendLine($"Authorization:Bearer {freshToken}");
            connectFrame.AppendLine($"X-Vendor-ID:{_session.VendorId}");
            connectFrame.AppendLine($"X-Business-Name:{_session.BusinessName}");
            connectFrame.AppendLine();
            connectFrame.Append('\0'); // STOMP null terminator

            await SendRawFrameAsync(connectFrame.ToString());
            _logger.LogDebug("Sent STOMP CONNECT frame with fresh authentication token");
        }

        private async Task SendStompDisconnectAsync()
        {
            var disconnectFrame = new StringBuilder();
            disconnectFrame.AppendLine("DISCONNECT");
            disconnectFrame.AppendLine();
            disconnectFrame.Append('\0');

            await SendRawFrameAsync(disconnectFrame.ToString());
            _logger.LogDebug("Sent STOMP DISCONNECT frame");
        }

        private async Task SubscribeToJobOffers()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var destination = $"/queue/job-offers-{_session.VendorId}";
            
            var subscribeFrame = new StringBuilder();
            subscribeFrame.AppendLine("SUBSCRIBE");
            subscribeFrame.AppendLine($"id:{subscriptionId}");
            subscribeFrame.AppendLine($"destination:{destination}");
            subscribeFrame.AppendLine();
            subscribeFrame.Append('\0');

            _subscriptions[subscriptionId] = destination;
            
            await SendRawFrameAsync(subscribeFrame.ToString());
            _logger.LogInformation("Subscribed to job offers: {Destination}", destination);
        }

        private async Task SendRawFrameAsync(string frame)
        {
            if (_webSocket?.State != WebSocketState.Open) return;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(frame);
                var buffer = new ArraySegment<byte>(bytes);
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, 
                    _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send STOMP frame");
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            try
            {
                while (_webSocket?.State == WebSocketState.Open && 
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
                            
                            await ProcessStompFrameAsync(message);
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
                _ = Task.Run(async () => await TriggerReconnectionAsync($"WebSocket error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WebSocket receive loop");
                _ = Task.Run(async () => await TriggerReconnectionAsync($"Unexpected error: {ex.Message}"));
            }
            finally
            {
                _isConnected = false;
            }
        }

        private async Task ProcessStompFrameAsync(string frame)
        {
            try
            {
                // Update heartbeat - we received a message
                _lastHeartbeat = DateTime.UtcNow;
                
                _logger.LogDebug("Received STOMP frame: {Frame}", frame.Length > 200 ? frame.Substring(0, 200) + "..." : frame);

                var lines = frame.Split('\n');
                if (lines.Length == 0) return;

                var command = lines[0].Trim();
                var headers = new Dictionary<string, string>();
                var bodyStartIndex = -1;

                // Parse headers
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        bodyStartIndex = i + 1;
                        break;
                    }

                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex);
                        var value = line.Substring(colonIndex + 1);
                        headers[key] = value;
                    }
                }

                // Extract body
                var body = string.Empty;
                if (bodyStartIndex >= 0 && bodyStartIndex < lines.Length)
                {
                    var bodyLines = new string[lines.Length - bodyStartIndex];
                    Array.Copy(lines, bodyStartIndex, bodyLines, 0, bodyLines.Length);
                    body = string.Join("\n", bodyLines).TrimEnd('\0'); // Remove STOMP null terminator
                }

                await HandleStompCommandAsync(command, headers, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing STOMP frame");
            }
        }

        private async Task HandleStompCommandAsync(string command, Dictionary<string, string> headers, string body)
        {
            switch (command.ToUpper())
            {
                case "CONNECTED":
                    await HandleStompConnectedAsync(headers);
                    break;

                case "MESSAGE":
                    await HandleStompMessageAsync(headers, body);
                    break;

                case "ERROR":
                    await HandleStompErrorAsync(headers, body);
                    break;

                case "RECEIPT":
                    _logger.LogDebug("Received STOMP RECEIPT");
                    break;

                default:
                    _logger.LogWarning("Unknown STOMP command: {Command}", command);
                    break;
            }
        }

        private async Task HandleStompConnectedAsync(Dictionary<string, string> headers)
        {
            _sessionId = headers.GetValueOrDefault("session");
            _isConnected = true;
            
            OnConnectionStatusChanged(WebSocketConnectionState.Connecting, 
                WebSocketConnectionState.Connected, "Connected to Spoolr Core via STOMP");
            
            _logger.LogInformation("STOMP connection established. Session ID: {SessionId}", _sessionId);
            
            await Task.CompletedTask;
        }

        private async Task HandleStompMessageAsync(Dictionary<string, string> headers, string body)
        {
            try
            {
                var destination = headers.GetValueOrDefault("destination", "");
                _logger.LogDebug("Received STOMP message from {Destination}: {Body}", destination, body);

                if (destination.StartsWith("/queue/job-offers-"))
                {
                    await ProcessJobOfferMessage(body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process STOMP message");
            }
        }

        private async Task HandleStompErrorAsync(Dictionary<string, string> headers, string body)
        {
            var message = headers.GetValueOrDefault("message", "Unknown error");
            _logger.LogError("STOMP error received: {Message}. Body: {Body}", message, body);
            
            OnConnectionStatusChanged(WebSocketConnectionState.Connected, 
                WebSocketConnectionState.Failed, $"STOMP error: {message}");
            
            await Task.CompletedTask;
        }

        private async Task ProcessJobOfferMessage(string messageBody)
        {
            try
            {
                using var document = JsonDocument.Parse(messageBody);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement))
                {
                    _logger.LogWarning("Received job message without type field: {Message}", messageBody);
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

                    default:
                        _logger.LogWarning("Unknown job message type: {Type}", messageType);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse job message as JSON: {Message}", messageBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job message: {Message}", messageBody);
            }
        }

    private async Task HandleJobOfferAsync(JsonElement messageElement)
    {
        try
        {
            var rawJson = messageElement.GetRawText();
            _logger.LogInformation("Raw job offer JSON: {Json}", rawJson);
            
            var jobOffer = JsonSerializer.Deserialize<JobOfferMessage>(rawJson);
            if (jobOffer != null)
            {
                _logger.LogInformation("Received job offer via STOMP: JobId={JobId}, Customer={Customer}, Price={Price}", 
                    jobOffer.JobId, jobOffer.DisplayCustomer, jobOffer.FormattedPrice);
                
                _logger.LogInformation("Job offer details: FileName={FileName}, PrintSpecs={PrintSpecs}, TrackingCode={TrackingCode}",
                    jobOffer.FileName, jobOffer.PrintSpecs, jobOffer.TrackingCode);

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

                    _logger.LogInformation("Job offer cancelled via STOMP: JobId={JobId}, Reason={Reason}", jobId, reason);

                    JobOfferCancelled?.Invoke(this, new JobOfferCancelledEventArgs(jobId, reason));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process job cancellation message");
            }

            await Task.CompletedTask;
        }

        private async Task<bool> SendVendorStatusMessage(bool isAvailable)
        {
            if (!_isConnected || _webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send vendor status: not connected");
                return false;
            }

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

                var messageBody = JsonSerializer.Serialize(statusMessage);
                
                // Send as STOMP message to /app/vendor-status destination
                var sendFrame = new StringBuilder();
                sendFrame.AppendLine("SEND");
                sendFrame.AppendLine("destination:/app/vendor-status");
                sendFrame.AppendLine($"content-type:application/json");
                sendFrame.AppendLine();
                sendFrame.Append(messageBody);
                sendFrame.Append('\0');

                await SendRawFrameAsync(sendFrame.ToString());
                
                _logger.LogInformation("Sent vendor status via STOMP: Available={Available}", isAvailable);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vendor status: {Error}", ex.Message);
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
        
        private string GetApiBaseUrl()
        {
            // Convert WebSocket URL to HTTP API URL
            // e.g., "ws://localhost:8080/ws" -> "http://localhost:8080"
            var wsUrl = SpoolrConfiguration.GetWebSocketEndpoint();
            
            if (wsUrl.StartsWith("ws://"))
            {
                return wsUrl.Replace("ws://", "http://").Replace("/ws", "");
            }
            else if (wsUrl.StartsWith("wss://"))
            {
                return wsUrl.Replace("wss://", "https://").Replace("/ws", "");
            }
            
            // Fallback to default
            return "http://localhost:8080";
        }

        private async Task CleanupAsync()
        {
            try
            {
                _isConnected = false;
                _sessionId = null;
                _subscriptions.Clear();
                
                // Stop heartbeat timer
                _heartbeatTimer.Stop();
                
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
                
                if (_heartbeatTask != null && !_heartbeatTask.IsCompleted)
                {
                    try
                    {
                        await _heartbeatTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation token is used
                    }
                }
                _heartbeatTask = null;
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
                lock (_connectionLock)
                {
                    _connectionState = ConnectionState.Disposed;
                    _disposed = true;
                }
                
                // Stop and dispose timer
                _heartbeatTimer?.Stop();
                _heartbeatTimer?.Dispose();
                
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
            }
            
            GC.SuppressFinalize(this);
        }
    }
}