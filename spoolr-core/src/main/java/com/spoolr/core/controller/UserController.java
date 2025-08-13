package com.spoolr.core.controller;

import com.spoolr.core.dto.DashboardResponse;
import com.spoolr.core.dto.LoginRequest;
import com.spoolr.core.dto.LoginResponse;
import com.spoolr.core.dto.PasswordResetEmailRequest;
import com.spoolr.core.dto.PasswordResetRequest;
import com.spoolr.core.dto.ProfileResponse;
import com.spoolr.core.entity.User;
import com.spoolr.core.service.UserService;
import com.spoolr.core.util.JwtUtil;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.*;

import jakarta.servlet.http.HttpServletRequest;

@RestController
@RequestMapping("/api/users")
public class UserController {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private JwtUtil jwtUtil;
    
    @PostMapping("/register")
    public ResponseEntity<?> registerUser(@RequestBody User user) {
        try {
            User registeredUser = userService.registerUser(user);
            return ResponseEntity.ok("User registered successfully. Please check your email for verification.");
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @GetMapping("/verify")
    public ResponseEntity<?> verifyEmail(@RequestParam String token) {
        try {
            User verifiedUser = userService.verifyEmail(token);
            return ResponseEntity.ok("Email verified successfully! You can now login.");
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    @PostMapping("/login")
    public ResponseEntity<LoginResponse> loginUser(@RequestBody LoginRequest loginRequest) {
        try {
            String token = userService.loginUser(loginRequest.getEmail(), loginRequest.getPassword());
            User user = userService.getByEmail(loginRequest.getEmail());
            LoginResponse response = new LoginResponse(token, user.getEmail(), user.getName(), user.getRole().toString(), user.getId(), "Login successful!");
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(new LoginResponse(null, null, null, null, null, e.getMessage()));
        }
    }
    
    @PostMapping("/request-password-reset")
    public ResponseEntity<?> requestPasswordReset(@RequestBody PasswordResetEmailRequest emailRequest) {
        try {
            userService.requestPasswordReset(emailRequest.getEmail());
            return ResponseEntity.ok("Password reset email sent. Please check your email.");
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @GetMapping("/reset-password")
    public ResponseEntity<String> resetPasswordForm(@RequestParam String token) {
        // This endpoint handles email link clicks and returns HTML form
        try {
            // Validate token exists (optional - just check if user exists with this token)
            // We don't validate expiry here, will be done on form submission
            
            String htmlForm = """
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Reset Password - PrintWave</title>
                    <style>
                        body { font-family: Arial, sans-serif; max-width: 400px; margin: 100px auto; padding: 20px; }
                        .form-group { margin-bottom: 15px; }
                        label { display: block; margin-bottom: 5px; font-weight: bold; }
                        input[type="password"] { width: 100%%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
                        button { background-color: #007bff; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; width: 100%%; }
                        button:hover { background-color: #0056b3; }
                        .header { text-align: center; color: #333; }
                    </style>
                </head>
                <body>
                    <h2 class="header">Reset Your Password</h2>
                    <p>Please enter your new password below:</p>
                    <form action="/api/users/reset-password" method="post" onsubmit="return submitForm(event)">
                        <input type="hidden" name="token" value="%s">
                        <div class="form-group">
                            <label for="newPassword">New Password:</label>
                            <input type="password" id="newPassword" name="newPassword" required minlength="6" placeholder="Enter new password">
                        </div>
                        <button type="submit">Reset Password</button>
                    </form>
                    
                    <script>
                        function submitForm(event) {
                            event.preventDefault();
                            const form = event.target;
                            const formData = new FormData(form);
                            
                            fetch('/api/users/reset-password', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json',
                                },
                                body: JSON.stringify({
                                    token: formData.get('token'),
                                    newPassword: formData.get('newPassword')
                                })
                            })
                            .then(response => response.text())
                            .then(data => {
                                alert(data);
                                if (data.includes('successful')) {
                                    window.location.href = '/api/users/login-page';
                                }
                            })
                            .catch(error => {
                                alert('Error: ' + error.message);
                            });
                        }
                    </script>
                </body>
                </html>
                """.formatted(token);
            
            return ResponseEntity.ok()
                .header("Content-Type", "text/html")
                .body(htmlForm);
                
        } catch (Exception e) {
            return ResponseEntity.badRequest()
                .header("Content-Type", "text/html")
                .body("<h2>Invalid or expired reset link</h2><p>" + e.getMessage() + "</p>");
        }
    }
    
    @PostMapping("/reset-password")
    public ResponseEntity<?> resetPassword(@RequestBody PasswordResetRequest resetRequest) {
        try {
            userService.resetPassword(resetRequest.getToken(), resetRequest.getNewPassword());
            return ResponseEntity.ok("Password reset successful! You can now login with your new password.");
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    // Protected endpoint - requires JWT authentication
    @GetMapping("/profile")
    @PreAuthorize("hasRole('CUSTOMER')")
    public ResponseEntity<ProfileResponse> getCurrentUserProfile(Authentication authentication, HttpServletRequest request) {
        try {
            // Get user info from JWT token (set by our filter)
            String userEmail = (String) request.getAttribute("userEmail");
            Long userId = (Long) request.getAttribute("userId");
            String userRole = (String) request.getAttribute("userRole");
            
            // Alternative: get from Authentication object
            String emailFromAuth = authentication.getName();
            
            // Get full user details from database
            User user = userService.getByEmail(userEmail);
            
            // Return user profile (excluding sensitive data)
            ProfileResponse response = new ProfileResponse(
                user.getId(),
                user.getEmail(),
                user.getName(),
                user.getPhoneNumber(),
                user.getRole().toString(),
                user.getEmailVerified(),
                "Profile retrieved successfully!"
            );
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(new ProfileResponse(
                null, null, null, null, null, null, e.getMessage()
            ));
        }
    }
    
    // User dashboard endpoint
    @GetMapping("/dashboard")
    @PreAuthorize("hasRole('CUSTOMER')")
    public ResponseEntity<DashboardResponse> getUserDashboard(Authentication authentication, HttpServletRequest request) {
        try {
            String userEmail = (String) request.getAttribute("userEmail");
            Long userId = (Long) request.getAttribute("userId");
            
            // Get user details for personalized dashboard
            User user = userService.getByEmail(userEmail);
            
            DashboardResponse response = new DashboardResponse(
                "Welcome to your dashboard!",
                userEmail,
                userId,
                "You have successfully accessed a protected endpoint!"
            );
            
            // Set additional dashboard info
            response.setWelcomeMessage("Welcome back, " + user.getName() + "!");
            response.setAccountStatus(user.getEmailVerified() ? "Verified" : "Pending Verification");
            
            // ✅ IMPLEMENTED: Calculate actual order count from PrintJob repository
            response.setTotalOrders(userService.getUserTotalOrders(userId));
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(new DashboardResponse(
                "Error loading dashboard", null, null, e.getMessage()
            ));
        }
    }
}
