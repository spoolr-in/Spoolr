using System.Windows.Input;
using SpoolrStation.Utilities;
using SpoolrStation.Models;
using SpoolrStation.Services;
using SpoolrStation.Views;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Windows;
using SpoolrStation.WebSocket.Models;
using SpoolrStation.ViewModels.JobOffers;
using SpoolrStation.Configuration;
using System.Net.Http;
using SpoolrStation.Services.Interfaces;
using SpoolrStation.Services.Core;

namespace SpoolrStation.ViewModels;

/// <summary>
/// ViewModel for the main window containing application state and commands
/// </summary>
public class MainViewModel : BaseViewModel
{
    #region Private Fields
    private string _statusText = "Ready";
    private bool _isWebSocketConnected = false;
    private bool _isLoggedIn = false;
    private string? _username = null;
    private bool _isVendorAvailable = false;
    private string _versionText = "v1.0.0";
    private int _activePrinters = 0;
    private int _pendingJobs = 0;
    private int _jobsToday = 0;
    private UserSession? _currentSession = null;
    private AuthService? _authService = null;
    private AuthenticationStateManager? _authStateManager = null;
    private PrinterDiscoveryService? _printerDiscoveryService = null;
    private PrinterCapabilitiesService? _printerCapabilitiesService = null;
    private List<LocalPrinter> _discoveredPrinters = new();
    private PrintersViewModel? _printersViewModel = null;
    
    // WebSocket integration (real Spoolr Core connection)
    private StompWebSocketClient? _webSocketClient = null;
    private ILoggerFactory? _loggerFactory = null;
    private SoundNotificationService? _soundService = null;
    
    // Background notification services
    private BackgroundNotificationService? _backgroundNotificationService = null;
    private AudioNotificationService? _audioNotificationService = null;
    private WindowFocusService? _windowFocusService = null;
    private AppSettings? _appSettings = null;
    private Window? _mainWindow = null;
    
    // Job offer management
    private int _activeJobOffers = 0;
    private ObservableCollection<JobOfferDisplayModel> _recentJobOffers = new();
    private ObservableCollection<AcceptedJobModel> _acceptedJobs = new();
    private string _connectionStatus = "Disconnected";
    private DateTime? _lastJobOfferTime = null;
    #endregion

    #region Properties

    /// <summary>
    /// Current status message
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Whether the WebSocket is connected for real-time job offers
    /// </summary>
    public bool IsWebSocketConnected
    {
        get => _isWebSocketConnected;
        set
        {
            if (SetProperty(ref _isWebSocketConnected, value))
            {
                OnPropertyChanged(nameof(WebSocketStatusText));
                OnPropertyChanged(nameof(WebSocketIndicatorColor));
                UpdateOverallStatus();
            }
        }
    }

    /// <summary>
    /// Whether a user is currently logged in
    /// </summary>
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (SetProperty(ref _isLoggedIn, value))
            {
                OnPropertyChanged(nameof(UserStatusText));
                OnPropertyChanged(nameof(LoginButtonText));
                UpdateOverallStatus();
            }
        }
    }

    /// <summary>
    /// Whether vendor is available to accept jobs (manual toggle)
    /// </summary>
    public bool IsVendorAvailable
    {
        get => _isVendorAvailable;
        set
        {
            if (SetProperty(ref _isVendorAvailable, value))
            {
                OnPropertyChanged(nameof(VendorStatusText));
                OnPropertyChanged(nameof(AvailabilityToggleText));
                UpdateOverallStatus();
                
                // When toggling availability, notify the backend
                if (IsLoggedIn)
                {
                    NotifyVendorStatus();
                }
            }
        }
    }

    /// <summary>
    /// Username of the currently logged in user
    /// </summary>
    public string? Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                OnPropertyChanged(nameof(UserStatusText));
            }
        }
    }

    /// <summary>
    /// Application version text
    /// </summary>
    public string VersionText
    {
        get => _versionText;
        set => SetProperty(ref _versionText, value);
    }

    /// <summary>
    /// Number of active printers
    /// </summary>
    public int ActivePrinters
    {
        get => _activePrinters;
        set => SetProperty(ref _activePrinters, value);
    }

    /// <summary>
    /// Number of pending print jobs
    /// </summary>
    public int PendingJobs
    {
        get => _pendingJobs;
        set => SetProperty(ref _pendingJobs, value);
    }

    /// <summary>
    /// Number of jobs processed today
    /// </summary>
    public int JobsToday
    {
        get => _jobsToday;
        set => SetProperty(ref _jobsToday, value);
    }
    
    /// <summary>
    /// Number of active job offers waiting for response
    /// </summary>
    public int ActiveJobOffers
    {
        get => _activeJobOffers;
        set => SetProperty(ref _activeJobOffers, value);
    }
    
    /// <summary>
    /// Recent job offers for display
    /// </summary>
    public ObservableCollection<JobOfferDisplayModel> RecentJobOffers
    {
        get => _recentJobOffers;
        set => SetProperty(ref _recentJobOffers, value);
    }
    
    /// <summary>
    /// Accepted jobs queue for processing
    /// </summary>
    public ObservableCollection<AcceptedJobModel> AcceptedJobs
    {
        get => _acceptedJobs;
        set => SetProperty(ref _acceptedJobs, value);
    }
    
    /// <summary>
    /// Whether there are no accepted jobs (for empty state visibility)
    /// </summary>
    public bool HasNoAcceptedJobs => AcceptedJobs.Count == 0;
    
    /// <summary>
    /// Connection status details
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }
    
    /// <summary>
    /// Last job offer received time
    /// </summary>
    public DateTime? LastJobOfferTime
    {
        get => _lastJobOfferTime;
        set => SetProperty(ref _lastJobOfferTime, value);
    }
    
    /// <summary>
    /// Formatted last job offer time for display
    /// </summary>
    public string LastJobOfferTimeText => LastJobOfferTime?.ToString("HH:mm:ss") ?? "Never";

    #endregion

    #region Computed Properties

    /// <summary>
    /// WebSocket connection status text
    /// </summary>
    public string WebSocketStatusText => IsWebSocketConnected ? "WebSocket Connected" : "WebSocket Disconnected";

    /// <summary>
    /// WebSocket indicator color
    /// </summary>
    public string WebSocketIndicatorColor => IsWebSocketConnected ? "#27AE60" : "#E74C3C";

    /// <summary>
    /// User status text for the header
    /// </summary>
    public string UserStatusText =>
        IsLoggedIn && !string.IsNullOrEmpty(Username) 
            ? $"Logged in as: {Username}" 
            : "Not Logged In";

    /// <summary>
    /// Login button text
    /// </summary>
    public string LoginButtonText => IsLoggedIn ? "Logout" : "Login";

    /// <summary>
    /// Vendor availability status text
    /// </summary>
    public string VendorStatusText
    {
        get
        {
            if (!IsLoggedIn) return "Please log in";
            if (!IsWebSocketConnected) return "WebSocket disconnected";
            return IsVendorAvailable ? "ONLINE - Ready for jobs" : "OFFLINE - Not accepting jobs";
        }
    }

    /// <summary>
    /// Availability toggle button text
    /// </summary>
    public string AvailabilityToggleText => IsVendorAvailable ? "Go Offline" : "Go Online";

    /// <summary>
    /// Whether the vendor can receive jobs (all conditions met)
    /// </summary>
    public bool CanReceiveJobs => IsLoggedIn && IsWebSocketConnected && IsVendorAvailable;
    
    /// <summary>
    /// PrintersViewModel for the Printers tab
    /// </summary>
    public PrintersViewModel? PrintersViewModel
    {
        get => _printersViewModel;
        private set => SetProperty(ref _printersViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand LoginCommand { get; }
    public ICommand ToggleAvailabilityCommand { get; }
    public ICommand DashboardCommand { get; }
    public ICommand PrintersCommand { get; }
    public ICommand JobsCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ScanPrintersCommand { get; }
    public ICommand SendCapabilitiesCommand { get; }
    public ICommand GetStartedCommand { get; }
    public ICommand LearnMoreCommand { get; }
    public ICommand TestWebSocketCommand { get; }
    public ICommand ReconnectWebSocketCommand { get; }
    public ICommand DebugWebSocketCommand { get; }
    public ICommand ComprehensiveDiagnosticsCommand { get; }
    
    // Job queue management commands
    public ICommand StartPrintingCommand { get; }
    public ICommand ViewJobDetailsCommand { get; }
    public ICommand CompleteJobCommand { get; }
    public ICommand PrintJobCommand { get; }
    public ICommand PreviewJobCommand { get; }

    #endregion

    #region Constructor

    public MainViewModel(AuthService? authService = null, ILoggerFactory? loggerFactory = null)
    {
        // Initialize services - use provided AuthService or get from ServiceProvider
        _authService = authService ?? Services.ServiceProvider.GetAuthService();
        _authStateManager = Services.ServiceProvider.GetAuthenticationStateManager();
        _printerDiscoveryService = new PrinterDiscoveryService();
        _printerCapabilitiesService = new PrinterCapabilitiesService(_authService);
        
        // Initialize WebSocket services
        _loggerFactory = loggerFactory ?? CreateDefaultLoggerFactory();
        
        // Initialize settings first
        _appSettings = new AppSettings();
        _ = InitializeSettingsAsync();
        
        // Initialize background notification services
        _backgroundNotificationService = new BackgroundNotificationService(_loggerFactory.CreateLogger<BackgroundNotificationService>());
        _audioNotificationService = new AudioNotificationService(_loggerFactory.CreateLogger<AudioNotificationService>(), _appSettings);
        _windowFocusService = new WindowFocusService(_loggerFactory.CreateLogger<WindowFocusService>(), _appSettings);
        
        // Initialize sound notification service (legacy - will be phased out in favor of AudioNotificationService)
        _soundService = new SoundNotificationService(_loggerFactory.CreateLogger<SoundNotificationService>());
        
        // Initialize PrintersViewModel
        PrintersViewModel = new PrintersViewModel(_authService);
        
        // Initialize commands
        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        ToggleAvailabilityCommand = new RelayCommand(ExecuteToggleAvailability, CanExecuteToggleAvailability);
        DashboardCommand = new RelayCommand(ExecuteDashboard);
        PrintersCommand = new RelayCommand(ExecutePrinters);
        JobsCommand = new RelayCommand(ExecuteJobs);
        SettingsCommand = new RelayCommand(ExecuteSettings);
        ScanPrintersCommand = new RelayCommand(ExecuteScanPrinters);
        SendCapabilitiesCommand = new RelayCommand(ExecuteSendCapabilities, CanExecuteSendCapabilities);
        GetStartedCommand = new RelayCommand(ExecuteGetStarted);
        LearnMoreCommand = new RelayCommand(ExecuteLearnMore);
        TestWebSocketCommand = new RelayCommand(() => ExecuteTestWebSocketAsync(), CanExecuteTestWebSocket);
        ReconnectWebSocketCommand = new RelayCommand(async () => await ExecuteReconnectWebSocketAsync(), CanExecuteReconnectWebSocket);
        DebugWebSocketCommand = new RelayCommand(async () => await ExecuteDebugWebSocketAsync());
        
        // Add comprehensive diagnostics command
        ComprehensiveDiagnosticsCommand = new RelayCommand(async () => await ExecuteComprehensiveDiagnosticsAsync());
        
        // Job queue management commands
        StartPrintingCommand = new RelayCommand<AcceptedJobModel>(ExecuteStartPrinting, CanExecuteJobAction);
        ViewJobDetailsCommand = new RelayCommand<AcceptedJobModel>(ExecuteViewJobDetails, CanExecuteJobAction);
        CompleteJobCommand = new RelayCommand<AcceptedJobModel>(ExecuteCompleteJob, CanExecuteJobAction);
        PrintJobCommand = new RelayCommand<AcceptedJobModel>(ExecutePrintJob, CanExecuteJobAction);
        PreviewJobCommand = new RelayCommand<AcceptedJobModel>(ExecutePreviewJob, CanExecuteJobAction);

        // Initialize with default state
        StatusText = "Station app started. Please log in to begin.";
        IsWebSocketConnected = false;
        IsLoggedIn = false;
        IsVendorAvailable = false;
    }

    #endregion

    #region Command Implementations

    private async void ExecuteLogin()
    {
        if (IsLoggedIn)
        {
            // Logout logic
            _authService?.Logout();
            _currentSession = null;
            IsLoggedIn = false;
            Username = null;
            IsVendorAvailable = false; // Go offline when logging out
            StatusText = "Logged out successfully";
            
            // Disconnect WebSocket and notify backend
            await DisconnectWebSocketAsync();
        }
        else
        {
            // Show login window
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog(); // Show as modal dialog
            
            // Check if login was successful by checking AuthService session
            var session = _authService?.CurrentSession;
            if (session != null && session.IsValid)
            {
                _ = InitializeWithSessionAsync(session);
            }
        }
    }

    private bool CanExecuteLogin()
    {
        return true; // Always allow login/logout
    }

    private void ExecuteToggleAvailability()
    {
        IsVendorAvailable = !IsVendorAvailable;
        
        StatusText = IsVendorAvailable 
            ? "You are now ONLINE and ready to receive job offers"
            : "You are now OFFLINE and will not receive job offers";
    }

    private bool CanExecuteToggleAvailability()
    {
        return IsLoggedIn && IsWebSocketConnected;
    }

    private void ExecuteDashboard()
    {
        StatusText = "Navigating to Dashboard...";
        // Navigation logic will be implemented later
    }

    private void ExecutePrinters()
    {
        StatusText = "Navigating to Printer Management...";
        // Navigation logic will be implemented later
    }

    private void ExecuteJobs()
    {
        StatusText = "Navigating to Job Management...";
        // Navigation logic will be implemented later
    }

    private void ExecuteSettings()
    {
        StatusText = "Opening Settings...";
        // Settings logic will be implemented later
    }

    private void ExecuteScanPrinters()
    {
        // Trigger printer discovery manually (without auto-sending capabilities)
        _ = ScanPrintersOnlyAsync();
    }

    private void ExecuteGetStarted()
    {
        StatusText = "Starting setup wizard...";
        // Setup wizard logic will be implemented later
    }

    private void ExecuteLearnMore()
    {
        StatusText = "Opening documentation...";
        // Documentation logic will be implemented later
    }
    
    private void ExecuteSendCapabilities()
    {
        // Send capabilities to backend manually
        _ = SendCapabilitiesToBackendAsync();
    }
    
    private bool CanExecuteSendCapabilities()
    {
        return IsLoggedIn && _discoveredPrinters.Any();
    }

    private async void ExecutePreviewJob(AcceptedJobModel? job)
    {
        if (job == null) return;
        
        try
        {
            StatusText = $"Requesting document preview for job {job.JobId} - {job.FileName}";
            
            if (_authService?.CurrentSession == null || !_authService.CurrentSession.IsValid)
            {
                throw new InvalidOperationException("You must be logged in to preview documents.");
            }

            // Get properly configured DocumentService with enhanced authentication
            var documentService = Services.ServiceProvider.GetDocumentService();
            
            // Create DocumentPrintJob from AcceptedJobModel with actual print specs
            var printSpecs = ParsePrintSpecs(job.PrintSpecs, job.TotalPrice);
            var documentJob = new DocumentPrintJob
            {
                JobId = job.JobId,
                TrackingCode = job.TrackingCode,
                CustomerName = job.Customer,
                OriginalFileName = job.FileName,
                FileType = "PDF", // Assume PDF for now
                StreamingUrl = "", // Will be populated by DocumentService
                PrintSpecs = printSpecs
            };
            
            // Log debug information
            Console.WriteLine($"=== PREVIEW DEBUG ===");
            Console.WriteLine($"JobId: {job.JobId}");
            Console.WriteLine($"VendorId: {_authService.CurrentSession.VendorId}");
            Console.WriteLine($"Token Length: {_authService.CurrentSession.JwtToken.Length}");
            Console.WriteLine($"Auth Service Valid: {_authService.CurrentSession.IsValid}");
            
            var authToken = _authService?.CurrentSession?.JwtToken;
            var previewResult = await documentService.GetDocumentPreviewAsync(documentJob, authToken);

            if (!previewResult.Success)
            {
                throw new InvalidOperationException(previewResult.ErrorMessage ?? "Failed to prepare document preview");
            }

            StatusText = $"Document downloaded successfully. Opening preview...";

            // Open DocumentPreviewWindow with the downloaded document
            var previewWindow = new DocumentPreviewWindow(documentJob, documentService);
            previewWindow.Show();

            StatusText = $"Document preview opened successfully for job {job.JobId}";
        }
        catch (HttpRequestException httpEx)
        {
            StatusText = $"Network error downloading document for job {job.JobId}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(httpEx, "Network error downloading document for job {JobId}", job.JobId);
            ShowNetworkErrorDialog(job, httpEx);
        }
        catch (TimeoutException timeoutEx)
        {
            StatusText = $"Document download timed out for job {job.JobId}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(timeoutEx, "Document download timeout for job {JobId}", job.JobId);
            ShowTimeoutErrorDialog(job, timeoutEx);
        }
        catch (UnauthorizedAccessException authEx)
        {
            StatusText = $"Access denied for document in job {job.JobId}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(authEx, "Authorization error for job {JobId}", job.JobId);
            ShowAuthorizationErrorDialog(job, authEx);
        }
        catch (Exception ex)
        {
            StatusText = $"Error preparing preview for job {job.JobId}: {ex.Message}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "General error preparing document preview for job {JobId}. Full details: {FullException}", job.JobId, ex.ToString());
            
            // Enhanced error logging
            Console.WriteLine($"=== PREVIEW ERROR DEBUG ===");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            ShowGenericPreviewError(job, ex);
        }
    }

    private void ExecuteStartPrinting(AcceptedJobModel? job)
    {
        if (job == null) return;
        StatusText = $"Starting print job {job.JobId}";
        // Implementation for starting print job
    }

    private void ExecuteViewJobDetails(AcceptedJobModel? job)
    {
        if (job == null) return;
        StatusText = $"Viewing details for job {job.JobId}";
        // Implementation for viewing job details
    }

    private void ExecuteCompleteJob(AcceptedJobModel? job)
    {
        if (job == null) return;
        StatusText = $"Completing job {job.JobId}";
        // Implementation for completing job
    }

    private void ExecutePrintJob(AcceptedJobModel? job)
    {
        if (job == null) return;
        StatusText = $"Printing job {job.JobId}";
        // Implementation for printing job
    }

    private bool CanExecuteJobAction(AcceptedJobModel? job)
    {
        return job != null && IsLoggedIn;
    }

    private void ShowNetworkErrorDialog(AcceptedJobModel job, HttpRequestException ex)
    {
        WpfMessageBox.Show($"Network error for job {job.JobId}: {ex.Message}", 
            "Network Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
    }

    private void ShowTimeoutErrorDialog(AcceptedJobModel job, TimeoutException ex)
    {
        WpfMessageBox.Show($"Timeout error for job {job.JobId}: {ex.Message}", 
            "Timeout Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
    }

    private void ShowAuthorizationErrorDialog(AcceptedJobModel job, UnauthorizedAccessException ex)
    {
        WpfMessageBox.Show($"Authorization error for job {job.JobId}: {ex.Message}", 
            "Authorization Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
    }

    private void ShowGenericPreviewError(AcceptedJobModel job, Exception ex)
    {
        WpfMessageBox.Show($"Preview error for job {job.JobId}: {ex.Message}", 
            "Preview Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
    }

    #endregion

    #region WebSocket & Backend Communication

    private async Task ConnectWebSocketAsync()
    {
        try
        {
            if (_authService?.CurrentSession?.IsValid != true)
            {
                StatusText = "Cannot connect WebSocket: No valid authentication session";
                return;
            }

            StatusText = "Connecting to Spoolr Core...";
            ConnectionStatus = "Connecting...";
            
            // Create WebSocket client if not exists
            if (_webSocketClient == null)
            {
                _webSocketClient = new StompWebSocketClient(_authService.CurrentSession!, 
                    _loggerFactory!.CreateLogger<StompWebSocketClient>());
                
                // Subscribe to WebSocket events
                _webSocketClient.JobOfferReceived += OnJobOfferReceived;
                _webSocketClient.JobOfferCancelled += OnJobOfferCancelled;
                _webSocketClient.ConnectionStatusChanged += OnWebSocketConnectionStatusChanged;
            }
            
            var connected = await _webSocketClient.ConnectAsync();
            
            if (connected)
            {
                IsWebSocketConnected = true;
                ConnectionStatus = "Connected to Spoolr Core";
                StatusText = "Connected to Spoolr Core - Ready to receive job offers!";
            }
            else
            {
                ConnectionStatus = "Failed to connect";
                StatusText = "Failed to connect to Spoolr Core";
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Error: {ex.Message}";
            StatusText = $"WebSocket connection error: {ex.Message}";
        }
    }

    private async Task DisconnectWebSocketAsync()
    {
        try
        {
            if (_webSocketClient != null)
            {
                await _webSocketClient.DisconnectAsync();
            }
            
            IsWebSocketConnected = false;
            ConnectionStatus = "Disconnected";
            StatusText = "Disconnected from job offer service";
        }
        catch (Exception ex)
        {
            StatusText = $"Error disconnecting WebSocket: {ex.Message}";
        }
    }

    private async void NotifyVendorStatus()
    {
        try
        {
            StatusText = $"Updating vendor status: {(IsVendorAvailable ? "ONLINE" : "OFFLINE")}...";
            
            if (_webSocketClient != null)
            {
                var success = await _webSocketClient.UpdateVendorStatusAsync(IsVendorAvailable);
                
                if (success)
                {
                    StatusText = $"Vendor status updated: {(IsVendorAvailable ? "ONLINE - Ready for jobs" : "OFFLINE - Not accepting jobs")}";
                }
                else
                {
                    StatusText = "Failed to update vendor status with Spoolr Core";
                }
            }
            else
            {
                StatusText = "Cannot update vendor status: Not connected to Spoolr Core";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error updating vendor status: {ex.Message}";
        }
    }

    private void UpdateOverallStatus()
    {
        // Update the main status based on current state
        if (!IsLoggedIn)
        {
            StatusText = "Please log in to start receiving job offers";
        }
        else if (!IsWebSocketConnected)
        {
            StatusText = "WebSocket disconnected - unable to receive job offers";
        }
        else if (IsVendorAvailable)
        {
            StatusText = "ONLINE - Ready to receive job offers";
        }
        else
        {
            StatusText = "OFFLINE - Toggle availability to start receiving job offers";
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the WebSocket connection status
    /// </summary>
    public void UpdateWebSocketStatus(bool connected)
    {
        IsWebSocketConnected = connected;
    }

    /// <summary>
    /// Updates statistics displayed on the dashboard
    /// </summary>
    public void UpdateStatistics(int activePrinters, int pendingJobs, int jobsToday)
    {
        ActivePrinters = activePrinters;
        PendingJobs = pendingJobs;
        JobsToday = jobsToday;
    }
    
    /// <summary>
    /// Initialize the main window reference for background notification services
    /// </summary>
    public void InitializeMainWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        
        // Initialize background notification service with main window
        _backgroundNotificationService?.Initialize(mainWindow);
    }
    
    /// <summary>
    /// Initialize settings asynchronously
    /// </summary>
    private async Task InitializeSettingsAsync()
    {
        try
        {
            if (_appSettings != null)
            {
                await _appSettings.LoadAsync(_loggerFactory?.CreateLogger<AppSettings>());
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with defaults
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "Failed to load application settings");
        }
    }

    /// <summary>
    /// Handles incoming job offers from WebSocket
    /// </summary>
    public void OnJobOfferReceived(object jobOffer)
    {
        _ = HandleJobOfferAsync(jobOffer);
    }
    
    /// <summary>
    /// Asynchronously handles incoming job offers with full notification support
    /// </summary>
    private async Task HandleJobOfferAsync(object jobOfferData)
    {
        try
        {
            if (jobOfferData is JobOfferMessage jobOffer)
            {
                // Update status and statistics
                StatusText = $"New job offer from {jobOffer.DisplayCustomer} - {jobOffer.FileName}";
                ActiveJobOffers++;
                LastJobOfferTime = DateTime.Now;
                OnPropertyChanged(nameof(LastJobOfferTimeText));
                
                // Determine if this is a high-priority/urgent offer
                var isHighPriority = DetermineJobPriority(jobOffer);
                
                // Show background notification (system tray)
                if (_backgroundNotificationService != null)
                {
                    await _backgroundNotificationService.ShowJobOfferNotificationAsync(jobOffer);
                }
                
                // Play audio notification
                if (_audioNotificationService != null)
                {
                    await _audioNotificationService.PlayJobOfferSoundAsync();
                }
                
                // Handle window focus for urgent offers
                if (_windowFocusService != null && _mainWindow != null)
                {
                    if (isHighPriority)
                    {
                        await _windowFocusService.FocusForUrgentJobOfferAsync(_mainWindow, true);
                        await _windowFocusService.SetTemporaryTopMostAsync(_mainWindow, TimeSpan.FromSeconds(10));
                    }
                    else
                    {
                        await _windowFocusService.FlashWindowAsync(_mainWindow);
                    }
                }
                
                // Show job offer dialog (existing functionality)
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    var jobOfferWindow = new JobOfferWindow(jobOffer);
                    
                    // Handle job acceptance/rejection through ViewModel events
                    if (jobOfferWindow.DataContext is JobOfferViewModel viewModel)
                    {
                        viewModel.JobAccepted += async (sender, e) =>
                        {
                            var acceptedJob = AcceptedJobModel.FromJobOffer(jobOffer);
                            await OnJobAcceptedAsync(acceptedJob);
                            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
                        };
                        
                        viewModel.JobDeclined += async (sender, e) =>
                        {
                            await OnJobDeclinedAsync(jobOffer.JobId.ToString());
                            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
                        };
                    }
                    
                    // Show the dialog
                    jobOfferWindow.ShowDialog();
                });
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error handling job offer: {ex.Message}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "Failed to handle job offer");
        }
    }
    
    /// <summary>
    /// Determine if a job offer should be treated as high priority
    /// </summary>
    private bool DetermineJobPriority(JobOfferMessage jobOffer)
    {
        // Consider high priority if:
        // - High monetary value (over $50)
        // - Rush job (expires soon)
        // - Repeat customer
        
        var highValueThreshold = 50.0m;
        var urgentExpiryThreshold = TimeSpan.FromMinutes(15);
        
        if (jobOffer.TotalPrice >= highValueThreshold)
            return true;
            
        if ((jobOffer.ExpiresAt - DateTime.UtcNow) <= urgentExpiryThreshold)
            return true;
            
        // Could add repeat customer logic here based on customer history
        
        return false;
    }
    
    /// <summary>
    /// Handle job acceptance with notifications
    /// </summary>
    private async Task OnJobAcceptedAsync(AcceptedJobModel acceptedJob)
    {
        try
        {
            // Add to accepted jobs queue
            AcceptedJobs.Add(acceptedJob);
            OnPropertyChanged(nameof(HasNoAcceptedJobs));
            
            // Play acceptance sound
            if (_audioNotificationService != null)
            {
                await _audioNotificationService.PlayJobActionSoundAsync(true);
            }
            
            // Show acceptance notification
            if (_backgroundNotificationService != null)
            {
                _backgroundNotificationService.ShowNotification(
                    "Job Accepted",
                    $"You've accepted the job from {acceptedJob.DisplayCustomer}. It's now in your print queue.");
            }
            
            StatusText = $"Job accepted from {acceptedJob.DisplayCustomer} - Added to print queue";
        }
        catch (Exception ex)
        {
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "Failed to handle job acceptance");
        }
    }
    
    /// <summary>
    /// Handle job decline with notifications
    /// </summary>
    private async Task OnJobDeclinedAsync(string jobId)
    {
        try
        {
            // Play rejection sound
            if (_audioNotificationService != null)
            {
                await _audioNotificationService.PlayJobActionSoundAsync(false);
            }
            
            // Show decline notification
            if (_backgroundNotificationService != null)
            {
                _backgroundNotificationService.ShowNotification(
                    "Job Declined",
                    "You've declined the job offer.");
            }
            
            StatusText = "Job offer declined";
        }
        catch (Exception ex)
        {
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "Failed to handle job decline");
        }
    }
    
    /// <summary>
    /// Initialize the ViewModel with authenticated session information
    /// </summary>
    /// <param name="session">User session from successful login</param>
    public async Task InitializeWithSessionAsync(UserSession session)
    {
        if (session != null && session.IsValid)
        {
            // THE FIX: Update the central state manager with the new session.
            await _authStateManager.UpdateAuthenticationStateAsync(session);

            _currentSession = session;
            IsLoggedIn = true;
            Username = session.BusinessName; // Use business name as display name
            StatusText = $"Welcome back, {session.BusinessName}! You are now logged in.";

            // Connect to WebSocket service
            await ConnectWebSocketAsync();

            // Update status after setting login state
            UpdateOverallStatus();

            // Start printer discovery and capabilities setup
            _ = InitializePrintersAsync();
        }
    }
    
    /// <summary>
    /// Initializes printer discovery and capabilities after login
    /// </summary>
    private async Task InitializePrintersAsync()
    {
        try
        {
            StatusText = "Scanning for printers...";
            
            // Discover printers
            var discoveryResult = await _printerDiscoveryService!.DiscoverPrintersAsync();
            
            if (discoveryResult.Success && discoveryResult.Printers.Any())
            {
                _discoveredPrinters = discoveryResult.Printers;
                var summary = _printerCapabilitiesService!.GetCapabilitiesSummary(_discoveredPrinters);
                StatusText = $"Printer discovery complete: {summary}";
                
                // Update active printers count
                ActivePrinters = _discoveredPrinters.Count(p => p.IsOnline);
                
                // Don't automatically send capabilities - let user select printers manually
                StatusText = $"Printer discovery complete: {summary}. Use Printers tab to manage.";
                
                // Check if capabilities have been sent before (for informational purposes)
                var capabilitiesUpToDate = await _printerCapabilitiesService.AreCapabilitiesUpToDateAsync();
                if (capabilitiesUpToDate)
                {
                    StatusText += " Capabilities previously sent to backend.";
                }
            }
            else
            {
                StatusText = discoveryResult.Message;
                if (!discoveryResult.Success)
                {
                    // Retry discovery in a few seconds
                    await Task.Delay(5000);
                    _ = InitializePrintersAsync();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Printer initialization error: {ex.Message}";
            Console.WriteLine($"[PRINTER] Initialization error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Sends printer capabilities to the backend
    /// </summary>
    private async Task SendCapabilitiesToBackendAsync()
    {
        try
        {
            if (!_discoveredPrinters.Any())
                return;
                
            StatusText = "Updating printer capabilities with backend...";
            
            var validation = _printerCapabilitiesService!.ValidateCapabilities(_discoveredPrinters);
            if (!validation.IsValid)
            {
                StatusText = $"Cannot send capabilities: {validation.ValidationMessage}";
                return;
            }
            
            var result = await _printerCapabilitiesService.SendCapabilitiesToBackendAsync(_discoveredPrinters);
            
            if (result.Success)
            {
                await _printerCapabilitiesService.MarkCapabilitiesAsSentAsync();
                StatusText = result.Message;
            }
            else
            {
                StatusText = $"Failed to update capabilities: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error sending capabilities: {ex.Message}";
            Console.WriteLine($"[PRINTER] Capabilities send error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Scans for printers without automatically sending capabilities to backend
    /// Used for manual "Scan for Printers" button
    /// </summary>
    private async Task ScanPrintersOnlyAsync()
    {
        try
        {
            StatusText = "Scanning for printers...";
            
            // Discover printers
            var discoveryResult = await _printerDiscoveryService!.DiscoverPrintersAsync();
            
            if (discoveryResult.Success && discoveryResult.Printers.Any())
            {
                _discoveredPrinters = discoveryResult.Printers;
                var summary = _printerCapabilitiesService!.GetCapabilitiesSummary(_discoveredPrinters);
                StatusText = $"Printer scan complete: {summary}";
                
                // Update active printers count
                ActivePrinters = _discoveredPrinters.Count(p => p.IsOnline);
            }
            else
            {
                StatusText = discoveryResult.Message.StartsWith("Found 0") ? "No printers found on this system" : discoveryResult.Message;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Printer scan error: {ex.Message}";
            Console.WriteLine($"[PRINTER] Scan error: {ex.Message}");
        }
    }

    private void ExecuteTestWebSocketAsync()
    {
        try
        {
            if (_authService?.CurrentSession?.IsValid != true)
            {
                StatusText = "Cannot test WebSocket: Not authenticated";
                return;
            }

            StatusText = "Testing WebSocket connection to Spoolr Core...";
            
            if (_webSocketClient != null)
            {
                var healthStatus = _webSocketClient.GetHealthStatus();
                
                if (healthStatus.IsHealthy)
                {
                    ConnectionStatus = "Test Passed";
                    StatusText = $"WebSocket connection test successful - {healthStatus.Message}";
                }
                else
                {
                    ConnectionStatus = "Test Failed";
                    StatusText = $"WebSocket connection test failed - {healthStatus.Message}";
                }
            }
            else
            {
                ConnectionStatus = "Test Failed";
                StatusText = "No WebSocket client available to test. Please login first.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"WebSocket test error: {ex.Message}";
        }
    }
    
    private bool CanExecuteTestWebSocket()
    {
        return IsLoggedIn;
    }
    
    private async Task ExecuteReconnectWebSocketAsync()
    {
        try
        {
            StatusText = "Reconnecting WebSocket...";
            await DisconnectWebSocketAsync();
            await Task.Delay(1000); // Brief delay
            await ConnectWebSocketAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Reconnection error: {ex.Message}";
        }
    }
    
    private async Task ExecuteDebugWebSocketAsync()
    {
        try
        {
            StatusText = "Running WebSocket diagnostic tests...";
            
            // Test basic WebSocket connectivity
            await SpoolrStation.Utilities.WebSocketTester.TestWebSocketConnectionAsync();
            
            // If logged in, test authenticated connection
            if (_authService?.CurrentSession?.IsValid == true)
            {
                var session = _authService.CurrentSession;
                await SpoolrStation.Utilities.WebSocketTester.TestAuthenticatedWebSocketAsync(
                    session.JwtToken, session.VendorId, session.BusinessName);
            }
            
            StatusText = "WebSocket diagnostic tests completed. Check console output for details.";
        }
        catch (Exception ex)
        {
            StatusText = $"Debug test error: {ex.Message}";
        }
    }
    
    private async Task ExecuteComprehensiveDiagnosticsAsync()
    {
        try
        {
            StatusText = "Running comprehensive WebSocket diagnostics...";
            
            // Run the new comprehensive diagnostics
            await SpoolrStation.Utilities.WebSocketDiagnostics.RunDiagnosticsAsync();
            
            StatusText = "Comprehensive diagnostics completed. Check console output for detailed results.";
        }
        catch (Exception ex)
        {
            StatusText = $"Diagnostics error: {ex.Message}";
        }
    }
    
    private bool CanExecuteReconnectWebSocket()
    {
        return IsLoggedIn;
    }
    
    private void OnJobOfferReceived(object? sender, JobOfferReceivedEventArgs e)
    {
        try
        {
            System.Console.WriteLine($"=== OnJobOfferReceived START - JobId: {e.Offer?.JobId} ===");
            var offer = e.Offer;
            System.Console.WriteLine($"Got offer object: {offer != null}");
            ActiveJobOffers++;
            LastJobOfferTime = DateTime.Now;
            System.Console.WriteLine($"Updated ActiveJobOffers and LastJobOfferTime");
            
            // Add to recent offers (keep only last 10)
            System.Console.WriteLine($"Creating JobOfferDisplayModel...");
            var displayOffer = new JobOfferDisplayModel
            {
                JobId = offer.JobId,
                CustomerName = offer.DisplayCustomer,
                FileName = offer.FileName,
                TotalPrice = offer.TotalPrice,
                Earnings = offer.Earnings,
                ReceivedAt = DateTime.Now,
                ExpiresAt = offer.ExpiresAt,
                Status = "Active"
            };
            System.Console.WriteLine($"JobOfferDisplayModel created successfully");
            
            System.Console.WriteLine($"About to invoke Dispatcher for RecentJobOffers update...");
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                System.Console.WriteLine($"Inside Dispatcher.Invoke for RecentJobOffers");
                RecentJobOffers.Insert(0, displayOffer);
                while (RecentJobOffers.Count > 10)
                {
                    RecentJobOffers.RemoveAt(RecentJobOffers.Count - 1);
                }
                System.Console.WriteLine($"RecentJobOffers updated successfully");
            });
            System.Console.WriteLine($"Dispatcher.Invoke for RecentJobOffers completed");
            
            StatusText = $"New job offer received! JobId: {offer.JobId}, Price: {offer.FormattedPrice}";
            
            // Play notification sound
            _ = Task.Run(async () =>
            {
                try
                {
                    await (_soundService?.PlayJobOfferNotificationAsync() ?? Task.CompletedTask);
                }
                catch (Exception ex)
                {
                    _loggerFactory?.CreateLogger<MainViewModel>().LogWarning(ex, "Failed to play job offer notification sound");
                }
            });
            
            // Show job offer popup
            System.Console.WriteLine($"=== About to show job offer popup for JobId: {offer.JobId} ===");
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                System.Console.WriteLine($"=== Inside Dispatcher.Invoke, calling ShowJobOfferPopup ===");
                try
                {
                    ShowJobOfferPopup(offer);
                    System.Console.WriteLine($"=== ShowJobOfferPopup completed successfully ===");
                }
                catch (Exception popupEx)
                {
                    System.Console.WriteLine($"=== EXCEPTION in ShowJobOfferPopup ===");
                    System.Console.WriteLine($"Exception: {popupEx.Message}");
                    System.Console.WriteLine($"Stack trace: {popupEx.StackTrace}");
                    StatusText = $"Error showing job offer popup: {popupEx.Message}";
                }
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"=== EXCEPTION in OnJobOfferReceived ===");
            System.Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            System.Console.WriteLine($"Exception Message: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Console.WriteLine($"========================================");
            StatusText = $"Error handling job offer: {ex.Message}";
        }
    }
    
    private void OnJobOfferCancelled(object? sender, JobOfferCancelledEventArgs e)
    {
        try
        {
            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
            StatusText = $"Job offer {e.JobId} was cancelled: {e.Reason}";
            
            // Update the display model
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                var existingOffer = RecentJobOffers.FirstOrDefault(o => o.JobId == e.JobId);
                if (existingOffer != null)
                {
                    existingOffer.Status = "Cancelled";
                }
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Error handling job cancellation: {ex.Message}";
        }
    }
    
    private async void OnWebSocketConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        try
        {
            var isConnected = e.CurrentState == WebSocketConnectionState.Connected;
            IsWebSocketConnected = isConnected;
            ConnectionStatus = e.CurrentState.ToString();
            
            if (!string.IsNullOrEmpty(e.Message))
            {
                StatusText = e.Message;
            }
            
            // Handle connection notifications
            if (_backgroundNotificationService != null)
            {
                var title = isConnected ? "Connected to Spoolr" : "Disconnected from Spoolr";
                var message = isConnected ? "Ready to receive job offers" : "No longer receiving job offers";
                
                _backgroundNotificationService.ShowConnectionStatusNotification(title, message, !isConnected);
            }
            
            // Play connection sound
            if (_audioNotificationService != null)
            {
                await _audioNotificationService.PlayConnectionSoundAsync(isConnected);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error handling connection status: {ex.Message}";
            
            // Show error notification
            if (_backgroundNotificationService != null)
            {
                _backgroundNotificationService.ShowNotification("Connection Error", ex.Message, true);
            }
            
            // Play error sound
            if (_audioNotificationService != null)
            {
                _ = _audioNotificationService.PlayErrorSoundAsync();
            }
        }
    }
    
    private void ShowJobOfferPopup(JobOfferMessage offer)
    {
        try
        {
            System.Console.WriteLine($"=== ShowJobOfferPopup - JobId: {offer.JobId} ===");
            
            var popup = new Views.JobOfferWindow(offer);
            
            System.Console.WriteLine($"JobOfferWindow created, DataContext type: {popup.DataContext?.GetType().Name}");
            
            // Get the ViewModel to handle events properly
            if (popup.DataContext is JobOfferViewModel viewModel)
            {
                System.Console.WriteLine($"Subscribing to JobOfferViewModel events...");
                viewModel.JobAccepted += (sender, e) => {
                    System.Console.WriteLine($"=== MainViewModel received JobAccepted event - JobId: {e.JobId}, Response: {e.Response} ===");
                    OnJobOfferAccepted(offer, e.Response);
                };
                viewModel.JobDeclined += (sender, e) => {
                    System.Console.WriteLine($"=== MainViewModel received JobDeclined event - JobId: {e.JobId} ===");
                    OnJobOfferDeclined(e.JobId, e.Response);
                };
                viewModel.JobExpired += (sender, e) => {
                    System.Console.WriteLine($"=== MainViewModel received JobExpired event - JobId: {e.JobId} ===");
                    OnJobOfferExpired(e.JobId);
                };
                System.Console.WriteLine($"Events subscribed successfully in MainViewModel");
            }
            else
            {
                System.Console.WriteLine($"ERROR: popup.DataContext is not JobOfferViewModel!");
            }
            
            System.Console.WriteLine($"Showing popup window...");
            popup.Show();
            System.Console.WriteLine($"Popup window shown successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"=== EXCEPTION in ShowJobOfferPopup ===");
            System.Console.WriteLine($"Exception: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            StatusText = $"Error showing job offer popup: {ex.Message}";
        }
    }
    
    private async void OnJobOfferAccepted(JobOfferMessage jobOffer, string response)
    {
        try
        {
            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
            StatusText = $"Job {jobOffer.JobId} accepted! Notifying Spoolr Core...";
            
            // Send response to backend
            var responseSuccess = _webSocketClient != null 
                ? await _webSocketClient.RespondToJobOfferAsync(jobOffer.JobId, response)
                : false;
            
            if (responseSuccess)
            {
                if (responseSuccess)
        {
            PendingJobs++;
            JobsToday++;
            StatusText = $"Job {jobOffer.JobId} accepted! Spoolr Core notified. Added to job queue.";

            // Play success sound
            _ = Task.Run(async () => await (_soundService?.PlaySuccessNotificationAsync() ?? Task.CompletedTask));

            // Add to accepted jobs queue
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                // Update recent offers display model
                var existingOffer = RecentJobOffers.FirstOrDefault(o => o.JobId == jobOffer.JobId);
                if (existingOffer != null)
                {
                    existingOffer.Status = "Accepted";
                }

                // Add to accepted jobs queue
                var acceptedJob = AcceptedJobModel.FromJobOffer(jobOffer);
                AcceptedJobs.Add(acceptedJob);

                // Update computed property
                OnPropertyChanged(nameof(HasNoAcceptedJobs));
            });

            // TODO: Start document download and print process
        }
            }
            else
            {
                StatusText = $"Failed to notify Spoolr Core about job {jobOffer.JobId} acceptance";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error accepting job offer: {ex.Message}";
            _loggerFactory?.CreateLogger<MainViewModel>().LogError(ex, "Error accepting job {JobId}", jobOffer.JobId);
        }
    }
    
    private async void OnJobOfferDeclined(long jobId, string response)
    {
        try
        {
            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
            StatusText = $"Job {jobId} declined. Notifying Spoolr Core...";
            
            // Send response to backend
            var responseSuccess = await (_webSocketClient?.RespondToJobOfferAsync(jobId, response) ?? Task.FromResult(false));
            
            if (responseSuccess)
            {
                StatusText = $"Job {jobId} declined and Spoolr Core notified";
                
                // Update display model
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    var existingOffer = RecentJobOffers.FirstOrDefault(o => o.JobId == jobId);
                    if (existingOffer != null)
                    {
                        existingOffer.Status = "Declined";
                    }
                });
            }
            else
            {
                StatusText = $"Failed to notify Spoolr Core about job {jobId} decline";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error declining job offer: {ex.Message}";
        }
    }
    
    private void OnJobOfferExpired(long jobId)
    {
        try
        {
            ActiveJobOffers = Math.Max(0, ActiveJobOffers - 1);
            StatusText = $"Job {jobId} has expired";
            
            // Update display model
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                var existingOffer = RecentJobOffers.FirstOrDefault(o => o.JobId == jobId);
                if (existingOffer != null)
                {
                    existingOffer.Status = "Expired";
                }
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Error handling job expiration: {ex.Message}";
        }
    }
    
    protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        
        // Update related computed properties
        if (propertyName == nameof(LastJobOfferTime))
        {
            OnPropertyChanged(nameof(LastJobOfferTimeText));
        }
    }
    
    #endregion

    #region IDisposable
    
    private bool _disposed = false;
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _webSocketClient?.Dispose();
                _soundService?.Dispose();
                _loggerFactory?.Dispose();
            }
            _disposed = true;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    #endregion

    #region Private Methods

    private ILoggerFactory CreateDefaultLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
    
    /// <summary>
    /// Parses print specifications string into LockedPrintSpecifications object
    /// Expected format: "A4, Color, Single-sided, 1 copy" or "A4 PORTRAIT, Black & White, Double-sided, 2 copies"
    /// </summary>
    private LockedPrintSpecifications ParsePrintSpecs(string printSpecsString, decimal totalCost)
    {
        try
        {
            var parts = printSpecsString.Split(',').Select(p => p.Trim()).ToArray();
            
            // Parse paper size (e.g., "A4" or "A4 PORTRAIT")
            var paperSizePart = parts.Length > 0 ? parts[0] : "A4";
            var paperSizeParts = paperSizePart.Split(' ');
            var paperSize = paperSizeParts[0];
            var orientation = paperSizeParts.Length > 1 ? paperSizeParts[1] : "PORTRAIT";
            
            // Parse color ("Color" or "Black & White")
            var isColor = parts.Length > 1 && parts[1].Contains("Color", StringComparison.OrdinalIgnoreCase);
            
            // Parse sides ("Single-sided" or "Double-sided")
            var isDoubleSided = parts.Length > 2 && parts[2].Contains("Double", StringComparison.OrdinalIgnoreCase);
            
            // Parse copies ("1 copy" or "2 copies")
            var copies = 1;
            if (parts.Length > 3)
            {
                var copiesPart = parts[3].Split(' ')[0];
                int.TryParse(copiesPart, out copies);
            }
            
            return new LockedPrintSpecifications
            {
                PaperSize = paperSize,
                Orientation = orientation,
                IsColor = isColor,
                IsDoubleSided = isDoubleSided,
                Copies = copies,
                TotalCost = totalCost
            };
        }
        catch (Exception ex)
        {
            _loggerFactory?.CreateLogger<MainViewModel>().LogWarning(ex, "Failed to parse print specs: {PrintSpecs}", printSpecsString);
            
            // Return default specs on parse failure
            return new LockedPrintSpecifications
            {
                PaperSize = "A4",
                Orientation = "PORTRAIT",
                IsColor = false,
                IsDoubleSided = false,
                Copies = 1,
                TotalCost = totalCost
            };
        }
    }

    #endregion
}
