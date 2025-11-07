using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpoolrStation.Views;
using SpoolrStation.Services;
using SpoolrStation.Services.Interfaces;

namespace SpoolrStation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize enhanced dependency injection with authentication state management
            Services.ServiceProvider.Initialize();
            
            // AuthenticationStateManager is now initialized automatically by ServiceProvider
            
            // Start with LoginWindow instead of MainWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
