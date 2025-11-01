using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using SpoolrStation.Configuration;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service to manage window focus, activation, and attention-getting for urgent notifications
    /// </summary>
    public class WindowFocusService
    {
        private readonly ILogger<WindowFocusService> _logger;
        private readonly AppSettings _settings;

        // Windows API constants
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMER = 4;
        private const uint FLASHW_TIMERNOFG = 12;

        public WindowFocusService(ILogger<WindowFocusService>? logger = null, AppSettings? settings = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowFocusService>.Instance;
            _settings = settings ?? new AppSettings();
        }

        /// <summary>
        /// Bring window to front and focus for urgent job offers
        /// </summary>
        public async Task FocusForUrgentJobOfferAsync(Window window, bool isHighPriority = false)
        {
            if (!_settings.NotificationSettings.AutoFocusOnUrgentOffers)
            {
                _logger.LogDebug("Auto-focus on urgent offers is disabled");
                return;
            }

            try
            {
                await window.Dispatcher.InvokeAsync(() =>
                {
                    var windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

                    // First, flash the window to get attention
                    FlashWindow(windowHandle, isHighPriority);

                    // Then try to bring it to front and focus
                    BringWindowToFront(window, windowHandle);

                    _logger.LogInformation("Window focused for urgent job offer (High Priority: {IsHighPriority})", isHighPriority);
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to focus window for urgent job offer");
            }
        }

        /// <summary>
        /// Flash window in taskbar to get user's attention
        /// </summary>
        public async Task FlashWindowAsync(Window window, int flashCount = 3)
        {
            if (!_settings.NotificationSettings.FlashTaskbarOnNewOffer)
            {
                _logger.LogDebug("Taskbar flashing is disabled");
                return;
            }

            try
            {
                await window.Dispatcher.InvokeAsync(() =>
                {
                    var windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                    FlashWindow(windowHandle, false, flashCount);

                    _logger.LogDebug("Window flashed {FlashCount} times", flashCount);
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flash window");
            }
        }

        /// <summary>
        /// Ensure window is visible and restored if minimized
        /// </summary>
        public async Task EnsureWindowVisibleAsync(Window window)
        {
            try
            {
                await window.Dispatcher.InvokeAsync(() =>
                {
                    // If window is minimized, restore it
                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }

                    // Make sure it's visible
                    if (!window.IsVisible)
                    {
                        window.Show();
                    }

                    _logger.LogDebug("Window visibility ensured");
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure window visibility");
            }
        }

        /// <summary>
        /// Check if the current application window has focus
        /// </summary>
        public bool IsApplicationInFocus()
        {
            try
            {
                var foregroundWindow = GetForegroundWindow();
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                
                GetWindowThreadProcessId(foregroundWindow, out uint foregroundProcessId);
                
                bool hasFocus = currentProcess.Id == foregroundProcessId;
                _logger.LogDebug("Application has focus: {HasFocus}", hasFocus);
                
                return hasFocus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if application has focus");
                return false;
            }
        }

        /// <summary>
        /// Set window to stay on top temporarily
        /// </summary>
        public async Task SetTemporaryTopMostAsync(Window window, TimeSpan duration)
        {
            if (!_settings.NotificationSettings.TemporaryTopMostForUrgentOffers)
            {
                _logger.LogDebug("Temporary top-most is disabled");
                return;
            }

            try
            {
                await window.Dispatcher.InvokeAsync(async () =>
                {
                    var originalTopMost = window.Topmost;
                    
                    // Set to top-most
                    window.Topmost = true;
                    _logger.LogDebug("Window set to top-most for {Duration}", duration);

                    // Wait for the specified duration
                    await Task.Delay(duration);

                    // Restore original top-most setting
                    window.Topmost = originalTopMost;
                    _logger.LogDebug("Window top-most restored to original setting: {OriginalTopMost}", originalTopMost);
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set temporary top-most");
            }
        }

        #region Private Methods

        /// <summary>
        /// Flash the window in the taskbar
        /// </summary>
        private void FlashWindow(IntPtr windowHandle, bool isUrgent, int flashCount = 0)
        {
            try
            {
                var flashInfo = new FLASHWINFO
                {
                    cbSize = Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = windowHandle,
                    dwFlags = isUrgent ? FLASHW_ALL | FLASHW_TIMER : FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = (uint)flashCount,
                    dwTimeout = 0
                };

                FlashWindowEx(ref flashInfo);
                _logger.LogDebug("Window flashed via Windows API (Urgent: {IsUrgent}, Count: {FlashCount})", isUrgent, flashCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flash window via Windows API");
            }
        }

        /// <summary>
        /// Bring window to front and activate it
        /// </summary>
        private void BringWindowToFront(Window window, IntPtr windowHandle)
        {
            try
            {
                // Show window if hidden
                ShowWindow(windowHandle, SW_SHOW);

                // Restore if minimized
                if (window.WindowState == WindowState.Minimized)
                {
                    ShowWindow(windowHandle, SW_RESTORE);
                    window.WindowState = WindowState.Normal;
                }

                // Bring to front
                SetForegroundWindow(windowHandle);
                
                // Activate within WPF
                window.Activate();
                window.Focus();

                _logger.LogDebug("Window brought to front and activated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bring window to front");
            }
        }

        #endregion

        #region Windows API

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public int cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        #endregion
    }
}