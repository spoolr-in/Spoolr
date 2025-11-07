using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpoolrStation.Configuration;

namespace SpoolrStation.Utilities
{
    /// <summary>
    /// Diagnostic utility for WebSocket connection troubleshooting
    /// </summary>
    public static class WebSocketDiagnostics
    {
        /// <summary>
        /// Run comprehensive WebSocket diagnostics
        /// </summary>
        public static async Task RunDiagnosticsAsync()
        {
            Console.WriteLine("=== WebSocket Connection Diagnostics ===\n");
            
            // Test 1: Configuration check
            await TestConfigurationAsync();
            
            // Test 2: Basic connectivity test
            await TestBasicConnectivityAsync();
            
            // Test 3: WebSocket upgrade test
            await TestWebSocketUpgradeAsync();
            
            // Test 4: Authentication test (if logged in)
            await TestAuthenticatedConnectionAsync();
            
            Console.WriteLine("\n=== Diagnostics Complete ===");
        }
        
        private static async Task TestConfigurationAsync()
        {
            Console.WriteLine("1. Configuration Test:");
            
            try
            {
                var config = SpoolrConfiguration.GetConfigurationSummary();
                Console.WriteLine(config);
                
                var wsEndpoint = SpoolrConfiguration.GetWebSocketEndpoint();
                Console.WriteLine($"✓ WebSocket endpoint resolved: {wsEndpoint}");
                
                var apiUrl = SpoolrConfiguration.GetApiBaseUrl();
                Console.WriteLine($"✓ API base URL resolved: {apiUrl}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration error: {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestBasicConnectivityAsync()
        {
            Console.WriteLine("2. Basic Connectivity Test:");
            
            var wsEndpoint = SpoolrConfiguration.GetWebSocketEndpoint();
            
            try
            {
                // Convert ws:// to http:// for basic connectivity test
                var httpUrl = wsEndpoint.Replace("ws://", "http://").Replace("wss://", "https://");
                
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await httpClient.GetAsync(httpUrl);
                Console.WriteLine($"✓ HTTP connectivity: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("  Note: 403 Forbidden is expected for WebSocket endpoints");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTTP connectivity failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestWebSocketUpgradeAsync()
        {
            Console.WriteLine("3. WebSocket Upgrade Test:");
            
            var wsEndpoint = SpoolrConfiguration.GetWebSocketEndpoint();
            
            using var webSocket = new ClientWebSocket();
            
            try
            {
                // Set a short timeout for this test
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                Console.WriteLine($"Attempting WebSocket connection to: {wsEndpoint}");
                
                await webSocket.ConnectAsync(new Uri(wsEndpoint), cts.Token);
                
                Console.WriteLine($"✓ WebSocket connection established");
                Console.WriteLine($"  State: {webSocket.State}");
                Console.WriteLine($"  Subprotocol: {webSocket.SubProtocol ?? "None"}");
                
                // Test basic message sending
                var testMessage = "{\"type\":\"ping\"}";
                var bytes = Encoding.UTF8.GetBytes(testMessage);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                Console.WriteLine("✓ Test message sent successfully");
                
                // Try to receive a response
                var buffer = new byte[1024];
                var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                var delayTask = Task.Delay(3000, cts.Token);
                
                var completedTask = await Task.WhenAny(receiveTask, delayTask);
                
                if (completedTask == receiveTask)
                {
                    var result = await receiveTask;
                    var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"✓ Received response: {responseMessage}");
                }
                else
                {
                    Console.WriteLine("⚠ No response received within 3 seconds");
                }
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
            catch (WebSocketException wsEx)
            {
                Console.WriteLine($"❌ WebSocket error: {wsEx.WebSocketErrorCode}");
                Console.WriteLine($"   Message: {wsEx.Message}");
                
                // Provide specific guidance based on error type
                switch (wsEx.WebSocketErrorCode)
                {
                    case WebSocketError.NotAWebSocket:
                        Console.WriteLine("   → Server is not accepting WebSocket connections");
                        Console.WriteLine("   → Check if the backend WebSocket server is running correctly");
                        break;
                    case WebSocketError.ConnectionClosedPrematurely:
                        Console.WriteLine("   → Connection was closed by the server");
                        Console.WriteLine("   → Check server logs for authentication or authorization issues");
                        break;
                    default:
                        Console.WriteLine("   → Check network connectivity and firewall settings");
                        break;
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("❌ Connection timeout");
                Console.WriteLine("   → Server may be down or unreachable");
                Console.WriteLine("   → Check if the backend service is running on the expected port");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestAuthenticatedConnectionAsync()
        {
            Console.WriteLine("4. Authentication Test:");
            
            try
            {
                var authService = Services.ServiceProvider.GetAuthService();
                var session = authService.CurrentSession;
                
                if (session == null || !session.IsValid)
                {
                    Console.WriteLine("⚠ No valid authentication session found");
                    Console.WriteLine("   → Please login first to test authenticated WebSocket connection");
                    return;
                }
                
                Console.WriteLine($"✓ Authentication session found");
                Console.WriteLine($"  Vendor ID: {session.VendorId}");
                Console.WriteLine($"  Business Name: {session.BusinessName}");
                Console.WriteLine($"  Token Length: {session.JwtToken.Length} characters");
                Console.WriteLine($"  Token Valid: {session.IsValid}");
                
                // Test token refresh
                var authStateManager = Services.ServiceProvider.GetAuthenticationStateManager();
                var freshToken = await authStateManager.GetValidTokenAsync();
                
                if (!string.IsNullOrEmpty(freshToken))
                {
                    Console.WriteLine("✓ Fresh token retrieved successfully");
                    Console.WriteLine($"  Fresh token length: {freshToken.Length} characters");
                    Console.WriteLine($"  Tokens match: {freshToken == session.JwtToken}");
                }
                else
                {
                    Console.WriteLine("❌ Failed to retrieve fresh token");
                    Console.WriteLine("   → This may cause WebSocket authentication to fail");
                }
                
                // Test authenticated WebSocket connection
                await WebSocketTester.TestAuthenticatedWebSocketAsync(
                    freshToken ?? session.JwtToken, 
                    session.VendorId, 
                    session.BusinessName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// Quick connection test that returns a boolean result
        /// </summary>
        public static async Task<bool> QuickConnectionTestAsync()
        {
            try
            {
                var wsEndpoint = SpoolrConfiguration.GetWebSocketEndpoint();
                
                using var webSocket = new ClientWebSocket();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                await webSocket.ConnectAsync(new Uri(wsEndpoint), cts.Token);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get detailed connection status
        /// </summary>
        public static async Task<string> GetConnectionStatusAsync()
        {
            try
            {
                var wsEndpoint = SpoolrConfiguration.GetWebSocketEndpoint();
                
                using var webSocket = new ClientWebSocket();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                await webSocket.ConnectAsync(new Uri(wsEndpoint), cts.Token);
                var status = $"Connected to {wsEndpoint} - State: {webSocket.State}";
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None);
                
                return status;
            }
            catch (Exception ex)
            {
                return $"Connection failed: {ex.Message}";
            }
        }
    }
}