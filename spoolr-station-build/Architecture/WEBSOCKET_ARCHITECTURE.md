# 🔗 **SPOOLR STATION WEBSOCKET CLIENT ARCHITECTURE**

## 📋 **EXECUTIVE SUMMARY**

This document defines a **production-grade WebSocket client architecture** for the Spoolr Station desktop application that integrates with the Spoolr Core backend. The architecture prioritizes **robustness**, **reliability**, and **testability** while maintaining compatibility with the existing Spring Boot STOMP WebSocket implementation.

---

## 🎯 **BACKEND WEBSOCKET ANALYSIS**

### **Core Backend Configuration:**
```yaml
Connection Endpoint: ws://localhost:8080/ws (dev) | wss://api.spoolr.com/ws (prod)
Protocol: STOMP over WebSocket
Authentication: JWT tokens (connection is public, subscription is vendor-specific)
Message Broker: Spring Simple Broker (in-memory)
Connection Timeout: 60 seconds
Heartbeat: 10-second intervals
```

### **Message Channels:**
```
Vendor Private Channels:
├── /queue/job-offers-{vendorId}     # Incoming job offers (90-second expiry)
└── /queue/job-offers-{vendorId}     # Job offer cancellations

Customer Public Channels:
└── /topic/job-status/{trackingCode} # Job status updates (for reference)

Application Channels:
└── /app/*                          # Future bidirectional messaging
```

### **Critical Message Formats:**

#### **Job Offer Message:**
```json
{
  "type": "NEW_JOB_OFFER",
  "jobId": 123,
  "trackingCode": "PJ123456", 
  "fileName": "document.pdf",
  "customer": "John Smith",
  "printSpecs": "A4, Color, Double-sided, 2 copies",
  "totalPrice": 1.50,
  "earnings": 1.50,
  "createdAt": "2025-01-20T14:45:00",
  "isAnonymous": false,
  "offerExpiresInSeconds": 90
}
```

#### **Offer Cancellation Message:**
```json
{
  "type": "OFFER_CANCELLED",
  "jobId": 123,
  "message": "This job offer has been accepted by another vendor or cancelled."
}
```

---

## 🏗️ **STATION APP WEBSOCKET ARCHITECTURE**

### **🎨 ARCHITECTURAL PRINCIPLES**

1. **Enterprise Resilience** - Handle all failure modes gracefully
2. **Zero Message Loss** - Critical messages must not be lost during network issues
3. **Automatic Recovery** - Self-healing connections without user intervention
4. **Thread Safety** - Concurrent access from UI and background threads
5. **Comprehensive Monitoring** - Full observability for production debugging
6. **Testability First** - Mock-friendly design for comprehensive testing
7. **Performance Optimized** - Minimal CPU and memory overhead
8. **Security Conscious** - Protect vendor credentials and validate all messages

### **🔧 TECHNOLOGY STACK**

#### **Primary Dependencies:**
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="System.Net.WebSockets.Client" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="System.Reactive" Version="6.0.0" />
<PackageReference Include="Polly" Version="8.2.0" />
```

#### **Testing Dependencies:**
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

---

## 📊 **DETAILED COMPONENT ARCHITECTURE**

### **Layer 1: WebSocket Transport Manager**

#### **Responsibilities:**
- Raw WebSocket connection establishment and maintenance
- Network failure detection and reporting  
- Connection pooling and resource management
- SSL/TLS certificate validation
- Transport-level security and encryption

#### **Key Classes:**
```csharp
public interface IWebSocketTransport
{
    Task<bool> ConnectAsync(Uri endpoint, CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task SendAsync(string message);
    IObservable<string> MessageReceived { get; }
    IObservable<WebSocketState> ConnectionStateChanged { get; }
    WebSocketState CurrentState { get; }
}

public class ResilientWebSocketTransport : IWebSocketTransport
{
    private readonly ClientWebSocket webSocket;
    private readonly IRetryPolicy retryPolicy;
    private readonly ILogger<ResilientWebSocketTransport> logger;
    private readonly CancellationTokenSource cancellationTokenSource;
}
```

#### **Resilience Features:**
- **Exponential Backoff Reconnection**: 1s, 2s, 4s, 8s, 16s, 30s (max)
- **Connection Health Monitoring**: Ping/pong heartbeat every 10 seconds  
- **Automatic Recovery**: Seamless reconnection without data loss
- **Circuit Breaker Pattern**: Prevent cascading failures during server outages
- **Resource Cleanup**: Proper disposal of connections and background threads

---

### **Layer 2: STOMP Protocol Handler**

#### **Responsibilities:**
- STOMP frame parsing and generation
- Protocol handshake and negotiation
- Subscription management and routing
- Message acknowledgment and delivery confirmation
- Heart-beat mechanism implementation

#### **Key Classes:**
```csharp
public interface IStompProtocolHandler
{
    Task ConnectAsync(string host, string login, string passcode);
    Task SubscribeAsync(string destination, Action<StompFrame> messageHandler);
    Task UnsubscribeAsync(string destination);
    Task SendAsync(string destination, string body, Dictionary<string, string> headers = null);
    Task DisconnectAsync();
}

public class StompProtocolHandler : IStompProtocolHandler
{
    private readonly IWebSocketTransport transport;
    private readonly Dictionary<string, Subscription> subscriptions;
    private readonly ILogger<StompProtocolHandler> logger;
    private int messageId = 0;
}
```

#### **STOMP Implementation Details:**
- **Frame Types Supported**: CONNECT, CONNECTED, SEND, SUBSCRIBE, UNSUBSCRIBE, DISCONNECT, MESSAGE, ERROR
- **Header Handling**: Content-type, content-length, destination, message-id, subscription
- **Error Recovery**: Automatic resubscription after connection loss
- **Flow Control**: Backpressure handling for high-volume message streams

---

### **Layer 3: Spoolr Message Router**

#### **Responsibilities:**
- Message deserialization and validation
- Vendor-specific message routing
- Job offer lifecycle management (90-second expiry)
- Duplicate message detection and filtering
- Event aggregation and transformation

#### **Key Classes:**
```csharp
public interface ISpoolrMessageRouter
{
    event EventHandler<JobOfferReceivedEventArgs> JobOfferReceived;
    event EventHandler<JobOfferCancelledEventArgs> JobOfferCancelled;
    event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
    event EventHandler<MessageErrorEventArgs> MessageError;
    
    Task StartAsync(long vendorId, string authToken);
    Task StopAsync();
}

public class SpoolrMessageRouter : ISpoolrMessageRouter
{
    private readonly IStompProtocolHandler stompHandler;
    private readonly IJobOfferManager offerManager;
    private readonly IMessageValidator messageValidator;
    private readonly ILogger<SpoolrMessageRouter> logger;
    private long currentVendorId;
}
```

#### **Message Validation:**
```csharp
public class MessageValidator : IMessageValidator
{
    public ValidationResult ValidateJobOffer(JobOfferMessage message)
    {
        var errors = new List<string>();
        
        if (message.JobId <= 0) errors.Add("Invalid job ID");
        if (string.IsNullOrEmpty(message.TrackingCode)) errors.Add("Missing tracking code");
        if (string.IsNullOrEmpty(message.FileName)) errors.Add("Missing file name");
        if (message.TotalPrice < 0) errors.Add("Invalid price");
        if (message.OfferExpiresInSeconds <= 0) errors.Add("Invalid expiry time");
        
        return new ValidationResult(errors.Count == 0, errors);
    }
}
```

---

### **Layer 4: Job Offer Management**

#### **Responsibilities:**
- Job offer state tracking and expiry management
- Timer-based offer cleanup (90-second countdown)
- Offer acceptance/rejection coordination with backend
- Duplicate offer detection and handling
- Local offer history and analytics

#### **Key Classes:**
```csharp
public interface IJobOfferManager
{
    Task<bool> AddJobOfferAsync(JobOfferMessage offer);
    Task<bool> CancelJobOfferAsync(long jobId, string reason);
    Task<JobOfferMessage> GetActiveOfferAsync(long jobId);
    Task<List<JobOfferMessage>> GetActiveOffersAsync();
    Task CleanupExpiredOffersAsync();
    
    event EventHandler<JobOfferExpiredEventArgs> OfferExpired;
}

public class JobOfferManager : IJobOfferManager
{
    private readonly ConcurrentDictionary<long, JobOfferState> activeOffers;
    private readonly Timer cleanupTimer;
    private readonly ILogger<JobOfferManager> logger;
}

public class JobOfferState
{
    public JobOfferMessage Offer { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public JobOfferStatus Status { get; set; }
    public CancellationTokenSource CancellationToken { get; set; }
}
```

#### **Expiry Management:**
- **Precise Timing**: High-resolution timers for accurate 90-second countdown
- **Automatic Cleanup**: Background service removes expired offers
- **UI Notifications**: Real-time countdown updates for active offers
- **Grace Period**: 5-second buffer for network latency compensation

---

### **Layer 5: Event Aggregation & UI Integration**

#### **Responsibilities:**
- Thread marshaling to UI thread
- Event filtering and aggregation
- Performance metrics collection
- Error logging and diagnostics
- UI state synchronization

#### **Key Classes:**
```csharp
public interface IWebSocketEventAggregator
{
    IObservable<JobOfferReceivedEvent> JobOfferReceived { get; }
    IObservable<JobOfferCancelledEvent> JobOfferCancelled { get; }
    IObservable<JobOfferExpiredEvent> JobOfferExpired { get; }
    IObservable<ConnectionStatusChangedEvent> ConnectionStatusChanged { get; }
    IObservable<WebSocketErrorEvent> ErrorOccurred { get; }
    
    Task StartAsync();
    Task StopAsync();
    Task<HealthCheckResult> GetHealthAsync();
}

public class WebSocketEventAggregator : IWebSocketEventAggregator
{
    private readonly ISpoolrMessageRouter messageRouter;
    private readonly IJobOfferManager offerManager;
    private readonly Subject<JobOfferReceivedEvent> jobOfferSubject;
    private readonly Subject<ConnectionStatusChangedEvent> connectionSubject;
    private readonly IScheduler uiScheduler;
}
```

#### **UI Thread Integration:**
```csharp
// Example UI integration in WPF
public partial class MainWindow : Window
{
    private readonly IWebSocketEventAggregator eventAggregator;
    private readonly CompositeDisposable subscriptions;
    
    private void SubscribeToWebSocketEvents()
    {
        eventAggregator.JobOfferReceived
            .ObserveOn(Scheduler.Dispatcher) // Marshal to UI thread
            .Subscribe(offer => ShowJobOfferModal(offer))
            .DisposeWith(subscriptions);
            
        eventAggregator.ConnectionStatusChanged
            .ObserveOn(Scheduler.Dispatcher)
            .Subscribe(status => UpdateConnectionIndicator(status))
            .DisposeWith(subscriptions);
    }
}
```

---

## 🛡️ **ROBUSTNESS & FAULT TOLERANCE**

### **Network Failure Handling**

#### **Connection Loss Recovery:**
```csharp
public class ConnectionRecoveryStrategy
{
    private static readonly TimeSpan[] RetryDelays = {
        TimeSpan.FromSeconds(1),    // Immediate retry
        TimeSpan.FromSeconds(2),    // Quick retry
        TimeSpan.FromSeconds(5),    // Short delay
        TimeSpan.FromSeconds(10),   // Medium delay
        TimeSpan.FromSeconds(20),   // Longer delay
        TimeSpan.FromSeconds(30),   // Maximum delay
        TimeSpan.FromSeconds(60)    // Extended failure
    };
    
    public async Task<bool> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        var policy = Policy
            .Handle<WebSocketException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                RetryDelays,
                (result, timespan, retryCount, context) => 
                {
                    logger.LogWarning("Retry {RetryCount} in {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
                
        return await policy.ExecuteAsync(operation);
    }
}
```

#### **Message Buffer During Outages:**
```csharp
public class MessageBuffer
{
    private readonly Queue<BufferedMessage> pendingMessages;
    private readonly int maxBufferSize = 1000;
    
    public void BufferMessage(string destination, string message, DateTime timestamp)
    {
        if (pendingMessages.Count >= maxBufferSize)
        {
            pendingMessages.Dequeue(); // Remove oldest message
        }
        
        pendingMessages.Enqueue(new BufferedMessage
        {
            Destination = destination,
            Content = message,
            Timestamp = timestamp,
            RetryCount = 0
        });
    }
    
    public async Task FlushBufferAsync()
    {
        while (pendingMessages.Count > 0)
        {
            var message = pendingMessages.Dequeue();
            await SendBufferedMessageAsync(message);
        }
    }
}
```

### **Error Recovery Mechanisms**

#### **Circuit Breaker Implementation:**
```csharp
public class WebSocketCircuitBreaker
{
    private CircuitBreakerState state = CircuitBreakerState.Closed;
    private int failureCount = 0;
    private DateTime lastFailureTime;
    private readonly int failureThreshold = 5;
    private readonly TimeSpan resetTimeout = TimeSpan.FromMinutes(1);
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (state == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - lastFailureTime < resetTimeout)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            state = CircuitBreakerState.HalfOpen;
        }
        
        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }
}
```

#### **State Synchronization After Reconnection:**
```csharp
public class StateSynchronizationService
{
    public async Task SynchronizeStateAsync(long vendorId)
    {
        // Get current job queue from REST API
        var currentJobs = await apiClient.GetJobQueueAsync(vendorId);
        
        // Compare with local state
        var localOffers = await offerManager.GetActiveOffersAsync();
        
        // Remove offers that are no longer valid
        foreach (var localOffer in localOffers)
        {
            if (!currentJobs.Any(j => j.Id == localOffer.JobId))
            {
                await offerManager.CancelJobOfferAsync(localOffer.JobId, "No longer available");
            }
        }
        
        // Add any new offers we missed during disconnection
        foreach (var remoteJob in currentJobs)
        {
            if (!localOffers.Any(o => o.JobId == remoteJob.Id))
            {
                // This would typically come via WebSocket, but we missed it
                logger.LogInformation("Recovered missed job offer: {JobId}", remoteJob.Id);
            }
        }
    }
}
```

---

## 🧪 **COMPREHENSIVE TESTING STRATEGY**

### **Unit Testing Framework**

#### **Message Processing Tests:**
```csharp
[Fact]
public async Task JobOfferMessage_ValidMessage_ShouldParseCorrectly()
{
    // Arrange
    var json = """
        {
            "type": "NEW_JOB_OFFER",
            "jobId": 123,
            "trackingCode": "PJ123456",
            "fileName": "test.pdf",
            "customer": "John Doe",
            "printSpecs": "A4, Color, 2 copies",
            "totalPrice": 1.50,
            "earnings": 1.50,
            "createdAt": "2025-01-20T14:45:00",
            "isAnonymous": false,
            "offerExpiresInSeconds": 90
        }
        """;
    
    var validator = new MessageValidator();
    
    // Act
    var message = JsonConvert.DeserializeObject<JobOfferMessage>(json);
    var result = validator.ValidateJobOffer(message);
    
    // Assert
    result.IsValid.Should().BeTrue();
    message.JobId.Should().Be(123);
    message.TrackingCode.Should().Be("PJ123456");
    message.OfferExpiresInSeconds.Should().Be(90);
}
```

#### **Connection Resilience Tests:**
```csharp
[Fact]
public async Task WebSocketTransport_NetworkFailure_ShouldReconnectAutomatically()
{
    // Arrange
    var mockTransport = new Mock<IWebSocketTransport>();
    var transport = new ResilientWebSocketTransport(mockTransport.Object);
    
    mockTransport.SetupSequence(t => t.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new WebSocketException("Network failure"))
        .ThrowsAsync(new WebSocketException("Network failure"))
        .ReturnsAsync(true);
    
    // Act
    var result = await transport.ConnectWithRetryAsync(new Uri("ws://test"), CancellationToken.None);
    
    // Assert
    result.Should().BeTrue();
    mockTransport.Verify(t => t.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
}
```

### **Integration Testing Infrastructure**

#### **Mock WebSocket Server:**
```csharp
public class MockSpoolrWebSocketServer : IDisposable
{
    private readonly WebSocketServer server;
    private readonly List<WebSocketConnection> connections;
    
    public void SimulateJobOffer(JobOfferMessage offer)
    {
        var json = JsonConvert.SerializeObject(offer);
        var frame = new StompFrame("MESSAGE", json, new Dictionary<string, string>
        {
            ["destination"] = $"/queue/job-offers-{offer.VendorId}",
            ["message-id"] = Guid.NewGuid().ToString()
        });
        
        BroadcastFrame(frame);
    }
    
    public void SimulateNetworkPartition(TimeSpan duration)
    {
        foreach (var connection in connections)
        {
            connection.Disconnect();
        }
        
        Task.Delay(duration).ContinueWith(_ => AcceptConnections());
    }
    
    public void SimulateServerRestart()
    {
        server.Stop();
        Task.Delay(1000).ContinueWith(_ => server.Start());
    }
}
```

#### **End-to-End Test Scenarios:**
```csharp
[Fact]
public async Task EndToEnd_JobOfferFlow_ShouldCompleteSuccessfully()
{
    // Arrange
    using var mockServer = new MockSpoolrWebSocketServer();
    var client = new SpoolrWebSocketClient(mockServer.ConnectionString);
    var offerReceived = false;
    
    client.JobOfferReceived += (sender, args) => {
        offerReceived = true;
    };
    
    // Act
    await client.ConnectAsync(vendorId: 123, authToken: "test-token");
    
    mockServer.SimulateJobOffer(new JobOfferMessage
    {
        JobId = 456,
        VendorId = 123,
        TrackingCode = "PJ789",
        FileName = "test.pdf"
    });
    
    await Task.Delay(100); // Allow message processing
    
    // Assert
    offerReceived.Should().BeTrue();
    client.IsConnected.Should().BeTrue();
}
```

### **Performance Testing Framework**

#### **Load Testing:**
```csharp
[Fact]
public async Task Performance_HighVolumeMessages_ShouldMaintainPerformance()
{
    // Arrange
    var client = new SpoolrWebSocketClient();
    var messageCount = 10000;
    var receivedCount = 0;
    var stopwatch = Stopwatch.StartNew();
    
    client.JobOfferReceived += (sender, args) => {
        Interlocked.Increment(ref receivedCount);
    };
    
    // Act
    for (int i = 0; i < messageCount; i++)
    {
        mockServer.SimulateJobOffer(CreateTestJobOffer(i));
        
        if (i % 1000 == 0)
        {
            await Task.Delay(10); // Brief pause to prevent overwhelming
        }
    }
    
    // Wait for processing to complete
    await Task.Delay(1000);
    stopwatch.Stop();
    
    // Assert
    receivedCount.Should().Be(messageCount);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
    
    // Performance targets:
    var messagesPerSecond = messageCount / stopwatch.Elapsed.TotalSeconds;
    messagesPerSecond.Should().BeGreaterThan(2000); // >2000 msg/sec
}
```

#### **Memory Usage Testing:**
```csharp
[Fact]
public async Task Performance_LongRunning_ShouldNotLeakMemory()
{
    // Arrange
    var client = new SpoolrWebSocketClient();
    var initialMemory = GC.GetTotalMemory(true);
    
    // Act - Run for extended period
    for (int hour = 0; hour < 24; hour++)
    {
        for (int minute = 0; minute < 60; minute++)
        {
            mockServer.SimulateJobOffer(CreateTestJobOffer());
            await Task.Delay(100); // Simulate 10 offers per minute
        }
        
        // Force garbage collection periodically
        if (hour % 4 == 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
    
    // Assert
    var finalMemory = GC.GetTotalMemory(true);
    var memoryGrowth = finalMemory - initialMemory;
    
    // Should not grow more than 50MB over 24 hours
    memoryGrowth.Should().BeLessThan(50 * 1024 * 1024);
}
```

---

## 🔐 **SECURITY CONSIDERATIONS**

### **Authentication Integration**

#### **JWT Token Management:**
```csharp
public class JwtTokenManager
{
    private string currentToken;
    private DateTime tokenExpiry;
    private readonly object tokenLock = new object();
    
    public async Task<string> GetValidTokenAsync()
    {
        lock (tokenLock)
        {
            if (string.IsNullOrEmpty(currentToken) || DateTime.UtcNow.AddMinutes(5) > tokenExpiry)
            {
                return RefreshTokenAsync();
            }
            return currentToken;
        }
    }
    
    private async Task<string> RefreshTokenAsync()
    {
        var loginRequest = new VendorLoginRequest
        {
            StoreCode = secureStorage.GetStoreCode(),
            Password = secureStorage.GetPassword()
        };
        
        var response = await apiClient.LoginAsync(loginRequest);
        
        currentToken = response.Token;
        tokenExpiry = DateTime.UtcNow.AddHours(23); // Refresh 1 hour before expiry
        
        return currentToken;
    }
}
```

#### **Secure Message Validation:**
```csharp
public class SecureMessageValidator
{
    public ValidationResult ValidateMessage(JobOfferMessage message, long expectedVendorId)
    {
        var errors = new List<string>();
        
        // Basic validation
        if (!IsValidJobOffer(message, errors)) return new ValidationResult(false, errors);
        
        // Security validation
        if (message.JobId <= 0) errors.Add("Invalid job ID format");
        if (message.TotalPrice < 0 || message.TotalPrice > 1000) errors.Add("Suspicious price amount");
        if (message.OfferExpiresInSeconds > 300 || message.OfferExpiresInSeconds < 30) 
            errors.Add("Invalid expiry duration");
        
        // Prevent injection attacks
        if (ContainsSuspiciousContent(message.FileName)) errors.Add("Invalid file name");
        if (ContainsSuspiciousContent(message.Customer)) errors.Add("Invalid customer name");
        
        return new ValidationResult(errors.Count == 0, errors);
    }
    
    private bool ContainsSuspiciousContent(string input)
    {
        var suspiciousPatterns = new[] { "<script>", "javascript:", "data:", "vbscript:" };
        return suspiciousPatterns.Any(pattern => 
            input?.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
```

### **Network Security**

#### **Certificate Validation:**
```csharp
public class SecureWebSocketTransport : IWebSocketTransport
{
    private ClientWebSocket ConfigureSecureWebSocket()
    {
        var webSocket = new ClientWebSocket();
        
        // Configure TLS settings
        webSocket.Options.RemoteCertificateValidationCallback = ValidateServerCertificate;
        
        // Set security headers
        webSocket.Options.SetRequestHeader("User-Agent", "SpoolrStation/1.0");
        webSocket.Options.SetRequestHeader("Origin", "spoolr-station");
        
        // Configure timeouts
        webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        
        return webSocket;
    }
    
    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // In production, implement proper certificate validation
        if (IsProductionEnvironment())
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }
        
        // In development, allow self-signed certificates
        return true;
    }
}
```

---

## 📊 **MONITORING & OBSERVABILITY**

### **Performance Metrics**

#### **Connection Metrics:**
```csharp
public class WebSocketMetrics
{
    private readonly Counter connectionAttempts;
    private readonly Counter connectionFailures; 
    private readonly Histogram connectionDuration;
    private readonly Counter messagesReceived;
    private readonly Counter messagesProcessed;
    private readonly Histogram messageProcessingTime;
    
    public void RecordConnectionAttempt() => connectionAttempts.Inc();
    public void RecordConnectionFailure() => connectionFailures.Inc();
    public void RecordMessageReceived() => messagesReceived.Inc();
    public void RecordMessageProcessingTime(TimeSpan duration) => 
        messageProcessingTime.Observe(duration.TotalMilliseconds);
}
```

#### **Health Check Implementation:**
```csharp
public class WebSocketHealthCheck : IHealthCheck
{
    private readonly IWebSocketEventAggregator eventAggregator;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var health = await eventAggregator.GetHealthAsync();
        
        var data = new Dictionary<string, object>
        {
            ["connection_state"] = health.ConnectionState.ToString(),
            ["last_message_received"] = health.LastMessageReceived,
            ["message_processing_errors"] = health.ProcessingErrors,
            ["reconnection_count"] = health.ReconnectionCount
        };
        
        if (health.IsHealthy)
        {
            return HealthCheckResult.Healthy("WebSocket connection is healthy", data);
        }
        else
        {
            return HealthCheckResult.Unhealthy("WebSocket connection issues detected", data);
        }
    }
}
```

### **Comprehensive Logging**

#### **Structured Logging Implementation:**
```csharp
public class WebSocketLogger
{
    private readonly ILogger<WebSocketLogger> logger;
    
    public void LogConnectionAttempt(Uri endpoint)
    {
        logger.LogInformation("Attempting WebSocket connection to {Endpoint}", endpoint);
    }
    
    public void LogConnectionSuccess(Uri endpoint, TimeSpan duration)
    {
        logger.LogInformation("WebSocket connection established to {Endpoint} in {Duration}ms", 
            endpoint, duration.TotalMilliseconds);
    }
    
    public void LogConnectionFailure(Uri endpoint, Exception exception)
    {
        logger.LogError(exception, "WebSocket connection failed to {Endpoint}", endpoint);
    }
    
    public void LogMessageReceived(string messageType, long jobId)
    {
        logger.LogDebug("Received message {MessageType} for job {JobId}", messageType, jobId);
    }
    
    public void LogMessageProcessingError(string message, Exception exception)
    {
        logger.LogError(exception, "Error processing message: {Message}", message);
    }
}
```

---

## 🚀 **IMPLEMENTATION ROADMAP**

### **Phase 1: Core Infrastructure (Week 1)**

#### **Deliverables:**
- Basic WebSocket transport layer
- STOMP protocol implementation  
- Message parsing and validation
- Simple connection management
- Unit test foundation (50+ tests)

#### **Success Criteria:**
- Connect to Spoolr Core WebSocket endpoint
- Successfully receive and parse job offer messages  
- Handle basic connection failures with retry
- 95%+ test coverage for core components

### **Phase 2: Resilience & Recovery (Week 2)**

#### **Deliverables:**
- Exponential backoff reconnection strategy
- Circuit breaker implementation
- Message buffering during outages
- State synchronization after reconnection
- Integration test suite (20+ scenarios)

#### **Success Criteria:**
- Survive 99% of network failures without data loss
- Automatic recovery within 30 seconds of server restart
- Handle message bursts of 1000+ messages/minute
- Complete integration testing with mock server

### **Phase 3: Production Features (Week 3)**

#### **Deliverables:**
- Comprehensive error handling and logging
- Performance monitoring and metrics
- Security enhancements and validation
- Memory leak prevention and cleanup
- Load testing framework

#### **Success Criteria:**
- Support 24/7 operation without memory leaks
- Process 10,000+ messages without performance degradation
- Comprehensive security validation for all messages
- Production-ready logging and monitoring

### **Phase 4: Integration & Testing (Week 4)**

#### **Deliverables:**  
- WPF UI integration layer
- End-to-end testing with real Spoolr Core
- Performance benchmarking and optimization
- Production deployment documentation
- Comprehensive test suite (200+ tests)

#### **Success Criteria:**
- Seamless integration with Station app UI
- Full compatibility with Spoolr Core WebSocket API
- Performance targets met (>2000 msg/sec, <50MB memory)
- Ready for vendor deployment and production use

---

## 📋 **CONCLUSION**

This architecture provides a **enterprise-grade WebSocket client** that meets all robustness, reliability, and testability requirements for the Spoolr Station app. The design leverages proven patterns and technologies to ensure seamless integration with the existing Spoolr Core backend while maintaining high performance and fault tolerance.

### **Key Architectural Strengths:**

1. **Production Ready** - Handles all failure modes gracefully
2. **Highly Testable** - Comprehensive test coverage with mock infrastructure  
3. **Performance Optimized** - Efficient message processing and memory management
4. **Security Conscious** - Proper authentication and message validation
5. **Maintainable** - Clear separation of concerns and well-documented interfaces
6. **Observable** - Full monitoring and logging for production debugging

### **Integration Benefits:**

- **Seamless Backend Compatibility** - Native STOMP protocol implementation
- **Leverage Existing Code** - Works with current .NET printing services
- **Fast Implementation** - Clear roadmap with defined milestones  
- **Future Proof** - Extensible design for additional features

This architecture ensures the Station app will have a **reliable, robust WebSocket connection** that never misses job offers and provides vendors with a professional, enterprise-grade experience.

---

*Architecture Version: 1.0*  
*Last Updated: January 2025*  
*Status: Ready for Implementation*
