using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class StompWebSocketTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Spoolr STOMP WebSocket Connection Test ===");
        Console.WriteLine();
        
        var endpoint = "ws://localhost:8080/ws";
        var vendorId = 19;
        var jwtToken = "eyJhbGciOiJIUzI1NiJ9.eyJyb2xlIjoiVkVORE9SIiwidXNlcklkIjoxOSwic3ViIjoiaGFja2VyaGVhZDY4QGdtYWlsLmNvbSIsImlhdCI6MTc1NzY3MDc0NCwiZXhwIjoxNzU3NzU3MTQ0fQ._6ar_4kUp8QC0b_iRpYGP3TKnAj_UQLzXWnrtk1jUrQ";
        
        using var webSocket = new ClientWebSocket();
        
        try
        {
            Console.WriteLine($"Connecting to: {endpoint}");
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            await webSocket.ConnectAsync(new Uri(endpoint), cts.Token);
            Console.WriteLine("✅ WebSocket connected successfully!");
            
            // Send STOMP CONNECT frame
            var connectFrame = new StringBuilder();
            connectFrame.AppendLine("CONNECT");
            connectFrame.AppendLine("accept-version:1.2");
            connectFrame.AppendLine("host:localhost");
            connectFrame.AppendLine($"login:{vendorId}");
            connectFrame.AppendLine($"Authorization:Bearer {jwtToken}");
            connectFrame.AppendLine($"X-Vendor-ID:{vendorId}");
            connectFrame.AppendLine($"X-Business-Name:Guruprasad Zerox");
            connectFrame.AppendLine();
            connectFrame.Append('\0'); // STOMP null terminator

            var connectBytes = Encoding.UTF8.GetBytes(connectFrame.ToString());
            await webSocket.SendAsync(new ArraySegment<byte>(connectBytes), WebSocketMessageType.Text, true, cts.Token);
            
            Console.WriteLine("📤 Sent STOMP CONNECT frame");
            
            // Listen for CONNECTED frame
            var buffer = new byte[2048];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            Console.WriteLine($"📥 Received STOMP frame:");
            Console.WriteLine(response);
            
            if (response.StartsWith("CONNECTED"))
            {
                Console.WriteLine("✅ STOMP connection established!");
                
                // Subscribe to job offers
                var subscribeFrame = new StringBuilder();
                subscribeFrame.AppendLine("SUBSCRIBE");
                subscribeFrame.AppendLine($"id:{Guid.NewGuid()}");
                subscribeFrame.AppendLine($"destination:/queue/job-offers-{vendorId}");
                subscribeFrame.AppendLine();
                subscribeFrame.Append('\0');

                var subscribeBytes = Encoding.UTF8.GetBytes(subscribeFrame.ToString());
                await webSocket.SendAsync(new ArraySegment<byte>(subscribeBytes), WebSocketMessageType.Text, true, cts.Token);
                
                Console.WriteLine($"📤 Subscribed to /queue/job-offers-{vendorId}");
                Console.WriteLine("✅ STOMP WebSocket connection successful!");
                Console.WriteLine("Ready to receive job offers...");
                
                // Listen for messages for a few seconds
                var listenTask = Task.Run(async () =>
                {
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var msgResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                            if (msgResult.MessageType == WebSocketMessageType.Text)
                            {
                                var message = Encoding.UTF8.GetString(buffer, 0, msgResult.Count);
                                Console.WriteLine($"📨 Received: {message}");
                            }
                        }
                    }
                    catch (OperationCanceledException) { /* Expected */ }
                });
                
                await Task.Delay(5000); // Listen for 5 seconds
                cts.Cancel();
            }
            else if (response.StartsWith("ERROR"))
            {
                Console.WriteLine("❌ STOMP ERROR received:");
                Console.WriteLine(response);
            }
            
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}