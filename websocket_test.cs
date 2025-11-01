using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class WebSocketTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Spoolr WebSocket Connection Test ===");
        Console.WriteLine();
        
        // Your JWT token from the login
        var jwtToken = "eyJhbGciOiJIUzI1NiJ9.eyJyb2xlIjoiVkVORE9SIiwidXNlcklkIjoxOSwic3ViIjoiaGFja2VyaGVhZDY4QGdtYWlsLmNvbSIsImlhdCI6MTc1NzY2OTc4MywiZXhwIjoxNzU3NzU2MTgzfQ.PgV6wMJXSdx3WFIMXcEFSzYhOdetQHObm2M1Mg_HGaU";
        var vendorId = 19;
        
        var endpoints = new[]
        {
            "ws://localhost:8080/ws/vendors",
            "ws://localhost:8080/websocket/vendors", 
            "ws://localhost:8080/ws",
            "ws://localhost:8080/websocket",
            "ws://localhost:8080/api/ws/vendors",
            "ws://localhost:8080/api/websocket"
        };

        foreach (var endpoint in endpoints)
        {
            Console.WriteLine($"Testing: {endpoint}");
            await TestEndpoint(endpoint, jwtToken, vendorId);
            Console.WriteLine();
            await Task.Delay(1000); // Brief delay between tests
        }

        Console.WriteLine("Tests completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task TestEndpoint(string endpoint, string jwtToken, int vendorId)
    {
        using var webSocket = new ClientWebSocket();
        
        try
        {
            // Add auth headers
            webSocket.Options.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            webSocket.Options.SetRequestHeader("X-Vendor-ID", vendorId.ToString());
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            Console.WriteLine($"  Connecting...");
            await webSocket.ConnectAsync(new Uri(endpoint), cts.Token);
            
            Console.WriteLine($"  ✅ SUCCESS: Connected!");
            Console.WriteLine($"  State: {webSocket.State}");
            
            // Send a test message
            var message = $"{{\"type\":\"VENDOR_STATUS\",\"vendorId\":{vendorId},\"isAvailable\":true}}";
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
            
            Console.WriteLine($"  ✅ Message sent successfully");
            
            // Try to receive a message (with timeout)
            var buffer = new byte[2048];
            var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            var delayTask = Task.Delay(3000, cts.Token);
            
            var completedTask = await Task.WhenAny(receiveTask, delayTask);
            
            if (completedTask == receiveTask)
            {
                var result = await receiveTask;
                var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"  📨 Received: {responseMessage}");
            }
            else
            {
                Console.WriteLine($"  ⏰ No immediate response received (timeout after 3s)");
            }
            
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"  ❌ WEBSOCKET ERROR: {ex.Message}");
            Console.WriteLine($"  WebSocketErrorCode: {ex.WebSocketErrorCode}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ ERROR: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
        }
    }
}