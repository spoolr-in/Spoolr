using Microsoft.Extensions.Logging;
using SpoolrStation.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpoolrStation.Services.Core
{
    /// <summary>
    /// Centralized authentication state manager for handling JWT tokens and refresh logic
    /// Provides automatic token refresh and consistent authentication context across all services
    /// </summary>
    public class AuthenticationStateManager
    {
        private static AuthenticationStateManager? _instance;
        private static readonly object _lock = new object();
        
        private readonly ILogger<AuthenticationStateManager> _logger;
        private readonly AuthService _authService;
        private readonly System.Threading.Timer _refreshTimer;
        private readonly SemaphoreSlim _refreshSemaphore;
        
        private UserSession? _currentSession;
        private bool _isRefreshing = false;
        private const int RefreshBufferMinutes = 5; // Refresh token 5 minutes before expiry
        
        public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

        private AuthenticationStateManager(AuthService authService, ILogger<AuthenticationStateManager> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _refreshSemaphore = new SemaphoreSlim(1, 1);
            
            // Initialize refresh timer (checks every minute)
            _refreshTimer = new System.Threading.Timer(CheckTokenRefreshAsync, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            
            _logger.LogInformation("AuthenticationStateManager initialized");
        }

        public static AuthenticationStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("AuthenticationStateManager has not been initialized. Call Initialize() first.");
                }
                return _instance;
            }
        }

        public static void Initialize(AuthService authService, ILogger<AuthenticationStateManager> logger)
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    throw new InvalidOperationException("AuthenticationStateManager has already been initialized.");
                }
                
                _instance = new AuthenticationStateManager(authService, logger);
            }
        }

        /// <summary>
        /// Gets whether the user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => _currentSession?.IsValid ?? false;

        /// <summary>
        /// Gets the current JWT token if available and valid
        /// </summary>
        public string? JwtToken => IsAuthenticated ? _currentSession?.JwtToken : null;

        /// <summary>
        /// Gets the current user session
        /// </summary>
        public UserSession? CurrentSession => _currentSession;

        /// <summary>
        /// Gets a valid JWT token, refreshing if necessary
        /// </summary>
        public async Task<string?> GetValidTokenAsync()
        {
            if (_currentSession == null)
            {
                _logger.LogWarning("No current session available for token retrieval");
                return null;
            }

            // Check if token needs refresh
            if (ShouldRefreshToken(_currentSession))
            {
                await RefreshTokenIfNeededAsync();
            }

            return _currentSession?.JwtToken;
        }

        /// <summary>
        /// Updates the authentication state with a new session
        /// </summary>
        public async Task UpdateAuthenticationStateAsync(UserSession session)
        {
            var previousState = _currentSession;
            _currentSession = session;
            
            _logger.LogInformation("Authentication state updated for user {Username}", session?.Username);
            
            // Notify subscribers of state change
            var stateChangeArgs = new AuthenticationStateChangedEventArgs
            {
                PreviousSession = previousState,
                CurrentSession = _currentSession,
                ChangeType = previousState == null ? AuthStateChangeType.Login : AuthStateChangeType.TokenRefresh,
                Timestamp = DateTime.UtcNow
            };
            
            AuthenticationStateChanged?.Invoke(this, stateChangeArgs);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears the current authentication state (logout)
        /// </summary>
        public async Task ClearAuthenticationStateAsync()
        {
            var previousSession = _currentSession;
            _currentSession = null;
            
            _logger.LogInformation("Authentication state cleared");
            
            // Notify subscribers of logout
            var stateChangeArgs = new AuthenticationStateChangedEventArgs
            {
                PreviousSession = previousSession,
                CurrentSession = null,
                ChangeType = AuthStateChangeType.Logout,
                Timestamp = DateTime.UtcNow
            };
            
            AuthenticationStateChanged?.Invoke(this, stateChangeArgs);
            
            await Task.CompletedTask;
        }

        private async void CheckTokenRefreshAsync(object? state)
        {
            try
            {
                await RefreshTokenIfNeededAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled token refresh check");
            }
        }

        private async Task RefreshTokenIfNeededAsync()
        {
            if (_currentSession == null || _isRefreshing)
                return;

            if (!ShouldRefreshToken(_currentSession))
                return;

            await _refreshSemaphore.WaitAsync();
            try
            {
                if (_isRefreshing || !ShouldRefreshToken(_currentSession))
                    return;

                _isRefreshing = true;
                _logger.LogInformation("Attempting to refresh authentication token");

                // Attempt token refresh
                var refreshResult = await _authService.RefreshTokenAsync(_currentSession.RefreshToken);
                
                if (refreshResult.Success && refreshResult.Session != null)
                {
                    await UpdateAuthenticationStateAsync(refreshResult.Session);
                    _logger.LogInformation("Authentication token refreshed successfully");
                }
                else
                {
                    _logger.LogWarning("Token refresh failed: {ErrorMessage}", refreshResult.ErrorMessage);
                    
                    // If refresh fails, clear the session to force re-authentication
                    await ClearAuthenticationStateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token refresh");
            }
            finally
            {
                _isRefreshing = false;
                _refreshSemaphore.Release();
            }
        }

        private bool ShouldRefreshToken(UserSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.RefreshToken))
                return false;

            // Refresh if token expires within the buffer time
            var refreshThreshold = DateTime.UtcNow.AddMinutes(RefreshBufferMinutes);
            return session.ExpiresAt <= refreshThreshold;
        }

        public void Dispose()
        {
            _refreshTimer?.Dispose();
            _refreshSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for authentication state changes
    /// </summary>
    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public UserSession? PreviousSession { get; set; }
        public UserSession? CurrentSession { get; set; }
        public AuthStateChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Types of authentication state changes
    /// </summary>
    public enum AuthStateChangeType
    {
        Login,
        Logout,
        TokenRefresh,
        SessionExpired
    }
}