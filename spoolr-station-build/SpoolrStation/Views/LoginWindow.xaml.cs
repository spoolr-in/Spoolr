using System;
using System.Windows;
using SpoolrStation.ViewModels;
using SpoolrStation.Services;

namespace SpoolrStation.Views
{
    /// <summary>
    /// Login Window for both first-time setup and regular vendor login
    /// </summary>
    public partial class LoginWindow : Window
    {
        private AuthViewModel _viewModel;
        private AuthService _authService;
        
        public LoginWindow()
        {
            InitializeComponent();
            
            // Initialize services and ViewModel
            _authService = new AuthService();
            _viewModel = new AuthViewModel(_authService);
            
            DataContext = _viewModel;
            
            // Subscribe to login success event
            _viewModel.LoginSuccess += OnLoginSuccess;
            
            // Handle password box binding since it can't be bound directly in XAML
            SetupPasswordBoxHandlers();
        }

        /// <summary>
        /// Sets up password box event handlers for binding to ViewModel properties
        /// </summary>
        private void SetupPasswordBoxHandlers()
        {
            // First-time login password boxes
            NewPasswordBox.PasswordChanged += (sender, e) =>
            {
                _viewModel.NewPassword = NewPasswordBox.Password;
            };
            
            ConfirmPasswordBox.PasswordChanged += (sender, e) =>
            {
                _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            };
            
            // Regular login password box
            PasswordBox.PasswordChanged += (sender, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
            };
            
            // Clear password boxes and handle UI visibility when switching modes
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(AuthViewModel.IsFirstTimeSetup))
                {
                    // Clear all password boxes when switching between login modes
                    NewPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                    PasswordBox.Clear();
                    
                    // Handle UI panel visibility since we can't use complex converters
                    UpdateUIVisibility();
                }
            };
            
            // Initial UI visibility update
            UpdateUIVisibility();
        }

        /// <summary>
        /// Handles successful login - transitions to main application window
        /// </summary>
        private void OnLoginSuccess()
        {
            try
            {
                // Get current session information
                var session = _viewModel.GetCurrentSession();
                if (session == null || !session.IsValid)
                {
                    WpfMessageBox.Show("Login session is invalid. Please try again.", "Login Error", 
                                  WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                    return;
                }

                // Create and show main window
                var mainWindow = new MainWindow(_authService);
                
                // Pass session information to main window
                mainWindow.InitializeWithSession(session);
                
                mainWindow.Show();
                
                // Close login window
                this.Close();
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error opening main application: {ex.Message}", "Application Error", 
                              WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Window loaded event - focus appropriate input field
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Update UI visibility first
            UpdateUIVisibility();
            
            // Focus the appropriate input field based on the current mode
            if (_viewModel.IsFirstTimeSetup)
            {
                // Focus activation key textbox for first-time setup
                ActivationKeyTextBox?.Focus();
            }
            else
            {
                // Focus password box for regular login (store code is pre-filled)
                PasswordBox.Focus();
            }
        }

        /// <summary>
        /// Handle Enter key press to submit forms
        /// </summary>
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_viewModel.IsFirstTimeSetup)
                {
                    if (_viewModel.FirstTimeLoginCommand.CanExecute(null))
                    {
                        _viewModel.FirstTimeLoginCommand.Execute(null);
                    }
                }
                else
                {
                    if (_viewModel.RegularLoginCommand.CanExecute(null))
                    {
                        _viewModel.RegularLoginCommand.Execute(null);
                    }
                }
            }
        }

        /// <summary>
        /// Handle window closing - cleanup resources
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.LoginSuccess -= OnLoginSuccess;
            }
            
            // Cleanup AuthService resources
            // Note: AuthService should implement IDisposable
        }

        /// <summary>
        /// Force show first-time setup (for testing/demo purposes)
        /// </summary>
        public void ShowFirstTimeSetup()
        {
            _viewModel.ForceFirstTimeSetup();
        }
        
        /// <summary>
        /// Updates UI panel visibility based on current setup state
        /// </summary>
        private void UpdateUIVisibility()
        {
            if (_viewModel != null)
            {
                FirstTimeSetupPanel.Visibility = _viewModel.IsFirstTimeSetup ? Visibility.Visible : Visibility.Collapsed;
                RegularLoginPanel.Visibility = _viewModel.IsFirstTimeSetup ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
