// Global using aliases to resolve conflicts between WPF and Windows Forms namespaces

// WPF aliases (preferred for UI)
global using WpfApplication = System.Windows.Application;
global using WpfMessageBox = System.Windows.MessageBox;
global using WpfMessageBoxButton = System.Windows.MessageBoxButton;
global using WpfMessageBoxResult = System.Windows.MessageBoxResult;
global using WpfMessageBoxImage = System.Windows.MessageBoxImage;
global using WpfUserControl = System.Windows.Controls.UserControl;
global using WpfColor = System.Windows.Media.Color;
global using WpfColorConverter = System.Windows.Media.ColorConverter;
global using WpfCheckBox = System.Windows.Controls.CheckBox;

// Windows Forms aliases (for system tray functionality)
global using WinFormsApplication = System.Windows.Forms.Application;
global using WinFormsMessageBox = System.Windows.Forms.MessageBox;
global using WinFormsTimer = System.Windows.Forms.Timer;
global using WinFormsNotifyIcon = System.Windows.Forms.NotifyIcon;
global using WinFormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
global using WinFormsToolTipIcon = System.Windows.Forms.ToolTipIcon;
global using WinFormsMouseEventArgs = System.Windows.Forms.MouseEventArgs;

// System.Drawing aliases (for icon handling)
global using DrawingColor = System.Drawing.Color;
global using DrawingColorConverter = System.Drawing.ColorConverter;

// Document and WebView2 usings
global using Microsoft.Web.WebView2.Wpf;
global using Microsoft.Web.WebView2.Core;

// Services Namespace
global using SpoolrStation.Services;
global using SpoolrStation.Services.Interfaces;
global using SpoolrStation.Models;
global using SpoolrStation.ViewModels;
