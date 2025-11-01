using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpoolrStation.Services.Interfaces;
using SpoolrStation.Services.Core;
using System;
using System.Net.Http;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Enhanced service provider with centralized authentication state management
    /// Now supports consistent authentication context across all services
    /// </summary>
    public static class ServiceProvider
    {
        private static IServiceProvider? _serviceProvider;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("ServiceProvider already initialized");
            }

            var services = new ServiceCollection();

            // Register core services first
            services.AddSingleton<AuthService>();
            services.AddLogging(builder => builder.AddConsole());
            
            // Build temporary provider for AuthenticationStateManager initialization
            var tempProvider = services.BuildServiceProvider();
            var authService = tempProvider.GetRequiredService<AuthService>();
            var logger = tempProvider.GetRequiredService<ILogger<AuthenticationStateManager>>();
            
            // Initialize AuthenticationStateManager singleton
            AuthenticationStateManager.Initialize(authService, logger);

            // Configure HttpClient with authentication context factory
            services.AddHttpClient<IDocumentService, DocumentService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:8080/api/");
                client.Timeout = TimeSpan.FromMinutes(5);
            }).ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                // Configure authentication headers if available
                try
                {
                    var authManager = AuthenticationStateManager.Instance;
                    if (authManager.IsAuthenticated && !string.IsNullOrEmpty(authManager.JwtToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authManager.JwtToken);
                    }
                }
                catch (InvalidOperationException)
                {
                    // AuthenticationStateManager not yet initialized, skip for now
                }
            });

            services.AddHttpClient<DocumentPreviewService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:8080/api/");
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            // Register enhanced services with authentication context
            services.AddSingleton<DocumentAccessMonitoringService>();
            services.AddTransient<IPdfDocumentRenderer, PdfDocumentRenderer>();
            services.AddTransient<PrinterDiscoveryService>();

            _serviceProvider = services.BuildServiceProvider();
            _initialized = true;
        }

        public static AuthService GetAuthService()
            => _serviceProvider?.GetRequiredService<AuthService>() ?? throw new InvalidOperationException("ServiceProvider not initialized");

        public static AuthenticationStateManager GetAuthenticationStateManager()
            => AuthenticationStateManager.Instance;

        public static IDocumentService GetDocumentService()
        {
            var docService = _serviceProvider?.GetRequiredService<IDocumentService>() ?? throw new InvalidOperationException("ServiceProvider not initialized");
            
            // Always ensure DocumentService has current authentication context
            if (docService is DocumentService documentService)
            {
                try
                {
                    var authManager = AuthenticationStateManager.Instance;
                    if (authManager.IsAuthenticated)
                    {
                        // Use the centralized auth context
                        documentService.SetAuthService(GetAuthService());
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Log warning but continue - AuthenticationStateManager not initialized yet
                    var logger = _serviceProvider.GetService<ILogger<DocumentService>>();
                    logger?.LogWarning("AuthenticationStateManager not available during DocumentService creation: {Error}", ex.Message);
                }
            }
            return docService;
        }

        public static DocumentPreviewService GetDocumentPreviewService()
            => _serviceProvider?.GetRequiredService<DocumentPreviewService>() ?? throw new InvalidOperationException("ServiceProvider not initialized");

        public static IPdfDocumentRenderer GetPdfDocumentRenderer()
            => _serviceProvider?.GetRequiredService<IPdfDocumentRenderer>() ?? throw new InvalidOperationException("ServiceProvider not initialized");

        // New: expose PrinterDiscoveryService
        public static PrinterDiscoveryService GetPrinterDiscoveryService()
            => _serviceProvider?.GetRequiredService<PrinterDiscoveryService>() ?? throw new InvalidOperationException("ServiceProvider not initialized");

        public static DocumentAccessMonitoringService GetDocumentAccessMonitoringService()
            => _serviceProvider?.GetRequiredService<DocumentAccessMonitoringService>() ?? throw new InvalidOperationException("ServiceProvider not initialized");

        /// <summary>
        /// Get WebSocket client from MainViewModel instance for status updates
        /// </summary>
        public static StompWebSocketClient? GetWebSocketClient()
            => ViewModels.MainViewModel.Instance?.WebSocketClient;
    }
}
