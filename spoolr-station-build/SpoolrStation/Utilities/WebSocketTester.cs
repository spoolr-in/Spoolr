using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpoolrStation.Utilities
{
    /// <summary>
    /// Utility to test WebSocket connections for debugging
    /// </summary>
    public static class WebSocketTester
    {
        /// <summary>
        /// Test WebSocket connection to various endpoints
        /// </summary>
        public static async Task TestWebSocketConnectionAsync()
        {
            var endpoints = new[]
            {
                "ws://localhost:8080/ws/vendors",
                "ws://localhost:8080/websocket/vendors", 
                "ws://localhost:8080/ws",
                "ws://localhost:8080/websocket",
                "ws://localhost:8080/socket.io",
                "ws://localhost:8080/api/ws/vendors"
            };

            Console.WriteLine("=== WebSocket Connection Test ===\n");

            foreach (var endpoint in endpoints)
            {
                Console.WriteLine($"Testing: {endpoint}");
                await TestSingleEndpoint(endpoint);
                Console.WriteLine();
            }
        }

        private static async Task TestSingleEndpoint(string endpoint)
        {
            using var webSocket = new ClientWebSocket();
            
            try
            {
                // Set timeout
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                // Test connection
                await webSocket.ConnectAsync(new Uri(endpoint), cts.Token);
                
                Console.WriteLine($"✅ SUCCESS: Connected to {endpoint}");
                Console.WriteLine($"   State: {webSocket.State}");
                
                // Try to send a simple message
                var message = "{\"type\":\"ping\"}";
                var bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                
                Console.WriteLine($"   Message sent successfully");
                
                // Try to receive a message (with short timeout)
                var buffer = new byte[1024];
                var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                var delayTask = Task.Delay(2000, cts.Token);
                
                var completedTask = await Task.WhenAny(receiveTask, delayTask);
                
                if (completedTask == receiveTask)
                {
                    var result = await receiveTask;
                    var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"   Received: {responseMessage}");
                }
                else
                {
                    Console.WriteLine($"   No immediate response received");
                }
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"❌ WEBSOCKET ERROR: {ex.Message}");
                Console.WriteLine($"   WebSocketErrorCode: {ex.WebSocketErrorCode}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ HTTP ERROR: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"❌ TIMEOUT: Connection timed out after 5 seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Test WebSocket connection with authentication headers
        /// </summary>
        public static async Task TestAuthenticatedWebSocketAsync(string jwtToken, long vendorId, string businessName)
        {
            var endpoint = "ws://localhost:8080/ws/vendors";
            
            Console.WriteLine("=== Authenticated WebSocket Test ===\n");
            Console.WriteLine($"Testing: {endpoint}");
            Console.WriteLine($"VendorId: {vendorId}");
            Console.WriteLine($"BusinessName: {businessName}");
            Console.WriteLine($"JWT Token: {(string.IsNullOrEmpty(jwtToken) ? "MISSING" : $"{jwtToken[..Math.Min(20, jwtToken.Length)]}...")}\n");

            using var webSocket = new ClientWebSocket();
            
            try
            {
                // Add authentication headers
                webSocket.Options.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                webSocket.Options.SetRequestHeader("X-Vendor-ID", vendorId.ToString());
                webSocket.Options.SetRequestHeader("X-Business-Name", businessName);
                
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                await webSocket.ConnectAsync(new Uri(endpoint), cts.Token);
                
                Console.WriteLine($"✅ SUCCESS: Authenticated connection established");
                Console.WriteLine($"   State: {webSocket.State}");
                
                // Send vendor status message
                var statusMessage = $@"{{
                    ""type"": ""VENDOR_STATUS"",
                    ""vendorId"": {vendorId},
                    ""isAvailable"": true,
                    ""businessName"": ""{businessName}"",
                    ""timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
                
                var bytes = Encoding.UTF8.GetBytes(statusMessage);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                
                Console.WriteLine($"✅ Vendor status message sent");
                
                // Listen for messages for 10 seconds
                Console.WriteLine("Listening for messages for 10 seconds...");
                
                var listenTask = ListenForMessages(webSocket, cts.Token);
                await Task.Delay(10000, cts.Token);
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                Console.WriteLine("✅ Connection closed gracefully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        private static async Task ListenForMessages(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server closed the connection");
                        break;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"📨 Received: {message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listening for messages: {ex.Message}");
            }
        }
    }
}