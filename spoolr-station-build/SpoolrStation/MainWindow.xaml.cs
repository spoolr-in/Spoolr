using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpoolrStation.ViewModels;
using SpoolrStation.Models;
using SpoolrStation.Services;
using System.Threading.Tasks;
using System.IO;

namespace SpoolrStation;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    
    public MainWindow(AuthService? authService = null)
    {
        InitializeComponent();
        
        // Initialize the ViewModel and set as DataContext
        ViewModel = new MainViewModel(authService);
        DataContext = ViewModel;
        
        // Initialize background notification services with this window
        ViewModel.InitializeMainWindow(this);
        
        // Initialize tab selection
        MainTabControl.SelectedIndex = 3; // Start with Welcome tab (now index 3)
    }
    
    // Navigation event handlers
    private void NavigateToDashboard(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 0; // Dashboard tab
    }
    
    private void NavigateToPrinters(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 2; // Printers tab (index 2 - after Dashboard and Jobs)
    }
    
    private void NavigateToJobs(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1; // Jobs tab (index 1 - after Dashboard)
        ViewModel.StatusText = "Viewing accepted jobs queue";
    }
    
    private void NavigateToSettings(object sender, RoutedEventArgs e)
    {
        // Settings will be added later
        ViewModel.StatusText = "Settings coming in Phase 3!";
    }
    
    /// <summary>
    /// Initializes the main window with authenticated session information
    /// </summary>
    /// <param name="session">User session from successful login</param>
    public void InitializeWithSession(UserSession session)
    {
        if (session != null && session.IsValid)
        {
            // Update ViewModel with session information
            _ = ViewModel.InitializeWithSessionAsync(session);
            
            // Navigate to Dashboard after successful login
            MainTabControl.SelectedIndex = 0; // Dashboard tab
        }
    }
    
    /// <summary>
    /// Handle Scan for Printers button click
    /// </summary>
    private async void ScanPrinters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.PrintersViewModel != null)
            {
                await ViewModel.PrintersViewModel.ScanForPrintersAsync();
                UpdatePrinterListUI();
            }
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Error scanning printers: {ex.Message}", "Scan Error", 
                          WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
        }
    }
    
    /// <summary>
    /// Handle Send Capabilities button click
    /// </summary>
    private async void SendCapabilities_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.PrintersViewModel != null)
            {
                var selectedCount = ViewModel.PrintersViewModel.SelectedPrintersCount;
                if (selectedCount == 0)
                {
                    WpfMessageBox.Show("Please select at least one printer to send capabilities.", 
                                  "No Printers Selected", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
                    return;
                }
                
                // Show confirmation dialog
                var confirmResult = WpfMessageBox.Show(
                    $"Send capabilities for {selectedCount} selected printer(s) to the backend?\n\n" +
                    "This will update your available printer options for customers.",
                    "Confirm Send Capabilities", 
                    WpfMessageBoxButton.YesNo, 
                    WpfMessageBoxImage.Question);
                
                if (confirmResult == WpfMessageBoxResult.Yes)
                {
                    // Send capabilities
                    await ViewModel.PrintersViewModel.SendSelectedCapabilitiesAsync();
                    
                    // Check if it was successful by looking at the status message
                    var statusMessage = ViewModel.PrintersViewModel.StatusMessage;
                    if (statusMessage.StartsWith("✅"))
                    {
                        // Success! Show celebration popup
                        ShowSuccessPopup(selectedCount);
                        
                        // Refresh the printer list to show "Sent" indicators
                        UpdatePrinterListUI();
                    }
                    else if (statusMessage.StartsWith("❌"))
                    {
                        // Error occurred
                        WpfMessageBox.Show(statusMessage.Replace("❌ ", ""), "Send Failed", 
                                      WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Error sending capabilities: {ex.Message}", "Send Error", 
                          WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
        }
    }
    
    /// <summary>
    /// Update the printer list UI after scanning
    /// </summary>
    private void UpdatePrinterListUI()
    {
        var printersViewModel = ViewModel.PrintersViewModel;
        if (printersViewModel == null) return;
        
        if (printersViewModel.HasPrinters)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            PrinterList.Visibility = Visibility.Visible;
            CreatePrinterListItems();
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            PrinterList.Visibility = Visibility.Collapsed;
        }
    }
    
    /// <summary>
    /// Create UI elements for each discovered printer
    /// </summary>
    private void CreatePrinterListItems()
    {
        var printersViewModel = ViewModel.PrintersViewModel;
        if (printersViewModel?.Printers == null) return;
        
        var stackPanel = new StackPanel();
        
        foreach (var printer in printersViewModel.Printers)
        {
            var printerItem = CreatePrinterItemUI(printer);
            stackPanel.Children.Add(printerItem);
        }
        
        PrinterList.Content = stackPanel;
    }
    
    /// <summary>
    /// Create UI for a single printer item with checkbox and details
    /// </summary>
    private Border CreatePrinterItemUI(PrinterViewModel printer)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(248, 249, 250)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Selection checkbox
        var checkbox = new WpfCheckBox
        {
            IsChecked = printer.IsSelected,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 15, 0)
        };
        checkbox.Checked += (s, e) => printer.IsSelected = true;
        checkbox.Unchecked += (s, e) => printer.IsSelected = false;
        Grid.SetColumn(checkbox, 0);
        
        // Printer information
        var infoPanel = new StackPanel();
        
        var nameText = new TextBlock
        {
            Text = printer.Name + (printer.IsDefault ? " (Default)" : ""),
            FontWeight = FontWeights.Bold,
            FontSize = 16
        };
        
        var statusText = new TextBlock
        {
            Text = $"{printer.StatusText} | {printer.DriverName}",
            FontSize = 12,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(127, 140, 141)),
            Margin = new Thickness(0, 3, 0, 0)
        };
        
        var capabilitiesText = new TextBlock
        {
            Text = printer.CapabilitiesSummary,
            FontSize = 11,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(149, 165, 166)),
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(statusText);
        infoPanel.Children.Add(capabilitiesText);
        Grid.SetColumn(infoPanel, 1);
        
        // Status indicator (green = online, red = offline)
        var statusIndicator = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = printer.IsOnline ? 
                   new SolidColorBrush(WpfColor.FromRgb(39, 174, 96)) : 
                   new SolidColorBrush(WpfColor.FromRgb(231, 76, 60)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(statusIndicator, 2);
        
        grid.Children.Add(checkbox);
        grid.Children.Add(infoPanel);
        grid.Children.Add(statusIndicator);
        
        border.Child = grid;
        return border;
    }
    
    /// <summary>
    /// Show a success popup when capabilities are successfully sent
    /// </summary>
    private void ShowSuccessPopup(int printerCount)
    {
        var message = $"🎉 Printer Information Successfully Stored! 🎉\n\n" +
                     $"✅ {printerCount} printer(s) capabilities sent to backend\n" +
                     $"✅ Your print shop is now ready to receive orders\n" +
                     $"✅ Customers can now see your available printer options\n\n" +
                     $"Your printers are now visible on the Spoolr platform!";
        
        WpfMessageBox.Show(message, "🖨️ Printer Setup Complete!", 
                       WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
        
        // Update main status to show success
        if (ViewModel != null)
        {
            ViewModel.StatusText = $"🎉 Success! {printerCount} printer(s) capabilities stored. Ready for orders!";
        }
    }
    
}
