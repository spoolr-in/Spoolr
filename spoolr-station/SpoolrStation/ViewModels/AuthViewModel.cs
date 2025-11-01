using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SpoolrStation.Models;
using SpoolrStation.Services;
using SpoolrStation.Utilities;

namespace SpoolrStation.ViewModels
{
    /// <summary>
    /// ViewModel for authentication screens handling both first-time and regular login
    /// Manages UI state and coordinates with AuthService
    /// </summary>
    public class AuthViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        // ======================== UI STATE PROPERTIES ========================

        private bool _isFirstTimeSetup;
        public bool IsFirstTimeSetup
        {
            get => _isFirstTimeSetup;
            set => SetProperty(ref _isFirstTimeSetup, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        // ======================== FIRST-TIME LOGIN PROPERTIES ========================

        private string _activationKey = string.Empty;
        public string ActivationKey
        {
            get => _activationKey;
            set => SetProperty(ref _activationKey, value);
        }

        private string _newPassword = string.Empty;
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        // ======================== REGULAR LOGIN PROPERTIES ========================

        private string _storeCode = string.Empty;
        public string StoreCode
        {
            get => _storeCode;
            set => SetProperty(ref _storeCode, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // ======================== DISPLAY PROPERTIES ========================

        private string _businessName = string.Empty;
        public string BusinessName
        {
            get => _businessName;
            set => SetProperty(ref _businessName, value);
        }

        // ======================== COMMANDS ========================

        public ICommand FirstTimeLoginCommand { get; }
        public ICommand RegularLoginCommand { get; }
        public ICommand SwitchToRegularLoginCommand { get; }
        public ICommand SwitchToFirstTimeSetupCommand { get; }
        public ICommand ClearErrorCommand { get; }

        // ======================== EVENTS ========================

        public event Action? LoginSuccess;

        // ======================== CONSTRUCTOR ========================

        public AuthViewModel(AuthService authService)
        {
            _authService = authService;

            FirstTimeLoginCommand = new RelayCommand(async () => await ExecuteFirstTimeLoginAsync(), CanExecuteFirstTimeLogin);
            RegularLoginCommand = new RelayCommand(async () => await ExecuteRegularLoginAsync(), CanExecuteRegularLogin);
            SwitchToRegularLoginCommand = new RelayCommand(() => IsFirstTimeSetup = false);
            SwitchToFirstTimeSetupCommand = new RelayCommand(() => IsFirstTimeSetup = true);
            ClearErrorCommand = new RelayCommand(ClearError);

            // Initialize the view state
            _ = InitializeAsync();
        }

        // ======================== INITIALIZATION ========================

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // Check if first-time setup is needed
                var isSetupComplete = await _authService.IsFirstTimeSetupCompletedAsync();
                IsFirstTimeSetup = !isSetupComplete;

                // If setup is complete, pre-load store code
                if (isSetupComplete)
                {
                    StoreCode = await _authService.GetSavedStoreCodeAsync();
                    var settings = await _authService.LoadSettingsAsync();
                    BusinessName = settings.BusinessName;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Initialization error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ======================== FIRST-TIME LOGIN LOGIC ========================

        private bool CanExecuteFirstTimeLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(ActivationKey) && 
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   NewPassword.Length >= 6 &&
                   NewPassword == ConfirmPassword;
        }

        private async Task ExecuteFirstTimeLoginAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                // Validate password match
                if (NewPassword != ConfirmPassword)
                {
                    ShowError("Passwords do not match.");
                    return;
                }

                // Validate password strength (basic)
                if (NewPassword.Length < 6)
                {
                    ShowError("Password must be at least 6 characters long.");
                    return;
                }

                // Perform first-time login
                var result = await _authService.FirstTimeLoginAsync(ActivationKey, NewPassword);

                if (result.Success)
                {
                    // Update UI state
                    BusinessName = result.BusinessName;
                    IsFirstTimeSetup = false;

                    // Clear sensitive data
                    ClearFirstTimeLoginFields();

                    // Notify success
                    LoginSuccess?.Invoke();
                }
                else
                {
                    ShowError(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ======================== REGULAR LOGIN LOGIC ========================

        private bool CanExecuteRegularLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(StoreCode) && 
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async Task ExecuteRegularLoginAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                // Perform regular login
                var result = await _authService.LoginAsync(StoreCode, Password);

                if (result.Success)
                {
                    // Update UI state
                    BusinessName = result.BusinessName;

                    // Clear sensitive data
                    Password = string.Empty;

                    // Notify success
                    LoginSuccess?.Invoke();
                }
                else
                {
                    ShowError(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ======================== HELPER METHODS ========================

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        private void ClearFirstTimeLoginFields()
        {
            ActivationKey = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        /// <summary>
        /// Forces switch to first-time setup mode (for testing or reset scenarios)
        /// </summary>
        public void ForceFirstTimeSetup()
        {
            IsFirstTimeSetup = true;
            StoreCode = string.Empty;
            BusinessName = string.Empty;
        }

        /// <summary>
        /// Gets current session information
        /// </summary>
        public UserSession? GetCurrentSession()
        {
            return _authService.CurrentSession;
        }

        /// <summary>
        /// Logs out current user
        /// </summary>
        public void Logout()
        {
            _authService.Logout();
            
            // Reset UI state
            Password = string.Empty;
            ClearError();
            
            // Re-initialize to check setup status
            _ = InitializeAsync();
        }
    }
}
