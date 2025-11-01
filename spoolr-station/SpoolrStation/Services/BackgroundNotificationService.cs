using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SpoolrStation.WebSocket.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Handles background notifications, system tray integration, and Windows toast notifications
    /// </summary>
    public class BackgroundNotificationService : IDisposable
    {
        private readonly ILogger<BackgroundNotificationService> _logger;
        private WinFormsNotifyIcon? _notifyIcon;
        private WinFormsContextMenuStrip? _contextMenu;
        private bool _disposed;
        private Window? _mainWindow;

        public BackgroundNotificationService(ILogger<BackgroundNotificationService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BackgroundNotificationService>.Instance;
        }

        /// <summary>
        /// Initialize system tray icon and notifications
        /// </summary>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            SetupSystemTrayIcon();
            SetupMainWindowHandlers();
            
            _logger.LogInformation("Background notification service initialized");
        }

        /// <summary>
        /// Show a job offer notification
        /// </summary>
        public async Task ShowJobOfferNotificationAsync(JobOfferMessage jobOffer)
        {
            try
            {
                // System tray balloon notification
                ShowBalloonNotification(
                    "New Print Job Available!",
                    $"From: {jobOffer.DisplayCustomer}\nFile: {jobOffer.FileName}\nValue: {jobOffer.FormattedPrice}",
                    WinFormsToolTipIcon.Info,
                    5000 // 5 seconds
                );

                // Windows toast notification (if supported)
                await ShowWindowsToastNotificationAsync(jobOffer);

                _logger.LogInformation("Job offer notification shown for {JobId}", jobOffer.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show job offer notification for {JobId}", jobOffer.JobId);
            }
        }

        /// <summary>
        /// Show a connection status notification
        /// </summary>
        public void ShowConnectionStatusNotification(string title, string message, bool isError = false)
        {
            try
            {
                var icon = isError ? WinFormsToolTipIcon.Error : WinFormsToolTipIcon.Info;
                ShowBalloonNotification(title, message, icon, 3000);
                
                _logger.LogInformation("Connection status notification shown: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show connection status notification");
            }
        }

        /// <summary>
        /// Show a general notification
        /// </summary>
        public void ShowNotification(string title, string message, bool isError = false)
        {
            try
            {
                var icon = isError ? WinFormsToolTipIcon.Error : WinFormsToolTipIcon.Info;
                ShowBalloonNotification(title, message, icon, 3000);
                
                _logger.LogDebug("General notification shown: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show general notification");
            }
        }

        /// <summary>
        /// Minimize application to system tray
        /// </summary>
        public void MinimizeToTray()
        {
            if (_mainWindow != null && _notifyIcon != null)
            {
                _mainWindow.Hide();
                _notifyIcon.Visible = true;
                
                ShowBalloonNotification(
                    "Spoolr Station Minimized",
                    "The application is still running in the background and will notify you of new job offers.",
                    WinFormsToolTipIcon.Info,
                    3000
                );
                
                _logger.LogInformation("Application minimized to system tray");
            }
        }

        /// <summary>
        /// Restore application from system tray
        /// </summary>
        public void RestoreFromTray()
        {
            if (_mainWindow != null && _notifyIcon != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _notifyIcon.Visible = false;
                
                _logger.LogInformation("Application restored from system tray");
            }
        }

        /// <summary>
        /// Setup system tray icon and context menu
        /// </summary>
        private void SetupSystemTrayIcon()
        {
            try
            {
                // Create context menu
                _contextMenu = new WinFormsContextMenuStrip();
                _contextMenu.Items.Add("Show Spoolr Station", null, OnRestoreClick);
                _contextMenu.Items.Add("-"); // Separator
                _contextMenu.Items.Add("Connection Status", null, OnConnectionStatusClick);
                _contextMenu.Items.Add("-"); // Separator
                _contextMenu.Items.Add("Exit", null, OnExitClick);

                // Create system tray icon
                _notifyIcon = new WinFormsNotifyIcon
                {
                    Text = "Spoolr Station - Print Job Management",
                    ContextMenuStrip = _contextMenu,
                    Visible = false // Hidden by default
                };

                // Set icon (try to load from resources, fallback to default)
                SetTrayIcon();

                // Handle double-click to restore window
                _notifyIcon.MouseDoubleClick += OnTrayIconDoubleClick;

                _logger.LogDebug("System tray icon setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup system tray icon");
            }
        }

        /// <summary>
        /// Setup main window event handlers for minimize behavior
        /// </summary>
        private void SetupMainWindowHandlers()
        {
            if (_mainWindow != null)
            {
                _mainWindow.StateChanged += OnMainWindowStateChanged;
                _mainWindow.Closing += OnMainWindowClosing;
            }
        }

        /// <summary>
        /// Set the system tray icon
        /// </summary>
        private void SetTrayIcon()
        {
            try
            {
                // Try to load icon from application resources
                var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/spoolr-icon.ico"));
                
                if (iconStream != null && _notifyIcon != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream.Stream);
                }
                else
                {
                    // Fallback to default system icon
                    _notifyIcon!.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load custom tray icon, using default");
                if (_notifyIcon != null)
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
        }

        /// <summary>
        /// Show balloon notification in system tray
        /// </summary>
        private void ShowBalloonNotification(string title, string text, WinFormsToolTipIcon icon, int timeout)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
            }
        }

        /// <summary>
        /// Show Windows toast notification (modern notification system)
        /// </summary>
        private async Task ShowWindowsToastNotificationAsync(JobOfferMessage jobOffer)
        {
            try
            {
                // For now, we'll use the basic balloon notification
                // In a production app, you might want to use the Windows 10/11 toast notification API
                // or a library like Microsoft.Toolkit.Win32.UI.Controls
                
                await Task.CompletedTask; // Placeholder for future toast notification implementation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show Windows toast notification");
            }
        }

        #region Event Handlers

        private void OnMainWindowStateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow?.WindowState == WindowState.Minimized)
            {
                // Don't minimize to tray automatically - let user choose
                // MinimizeToTray();
            }
        }

        private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Prevent closing - minimize to tray instead
            if (WpfMessageBox.Show("Minimize to system tray instead of closing?\n\nSpoolr Station will continue running in the background and notify you of new job offers.", 
                               "Close Spoolr Station", 
                               WpfMessageBoxButton.YesNo, 
                               WpfMessageBoxImage.Question) == WpfMessageBoxResult.Yes)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
        }

        private void OnTrayIconDoubleClick(object? sender, WinFormsMouseEventArgs e)
        {
            RestoreFromTray();
        }

        private void OnRestoreClick(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void OnConnectionStatusClick(object? sender, EventArgs e)
        {
            try
            {
                RestoreFromTray();
                
                // Show connection status - this would typically show a status dialog
                // For now, just show current connection state
                var statusMessage = "Connection status information would be shown here.";
                WpfMessageBox.Show(statusMessage, "Spoolr Station - Connection Status", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing connection status");
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            // Confirm exit
            if (WpfMessageBox.Show("Are you sure you want to exit Spoolr Station?\n\nYou will no longer receive job offer notifications.", 
                               "Exit Spoolr Station", 
                               WpfMessageBoxButton.YesNo, 
                               WpfMessageBoxImage.Question) == WpfMessageBoxResult.Yes)
            {
                WpfApplication.Current.Shutdown();
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                try
                {
                    // Clean up system tray
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }

                    // Clean up context menu
                    _contextMenu?.Dispose();
                    _contextMenu = null;

                    // Remove event handlers
                    if (_mainWindow != null)
                    {
                        _mainWindow.StateChanged -= OnMainWindowStateChanged;
                        _mainWindow.Closing -= OnMainWindowClosing;
                    }

                    _logger.LogInformation("Background notification service disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing background notification service");
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
