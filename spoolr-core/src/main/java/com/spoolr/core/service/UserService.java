package com.spoolr.core.service;

import com.spoolr.core.entity.User;
import com.spoolr.core.enums.UserRole;
import com.spoolr.core.repository.UserRepository;
import com.spoolr.core.repository.PrintJobRepository;
import com.spoolr.core.util.JwtUtil;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.UUID;

@Service
public class UserService {
    
    @Autowired
    private UserRepository userRepository;
    
    @Autowired
    private PrintJobRepository printJobRepository;
    
    @Autowired
    private EmailService emailService;
    
    @Autowired
    private JwtUtil jwtUtil;
    
    private BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();
    
    @Transactional
    public User registerUser(User user) {
        // Check if user already exists
        if (userRepository.existsByEmail(user.getEmail())) {
            throw new RuntimeException("User with email " + user.getEmail() + " already exists");
        }
        
        // Hash password and set additional fields
        user.setPassword(passwordEncoder.encode(user.getPassword()));
        user.setRole(UserRole.CUSTOMER);  // Force CUSTOMER role for all registrations
        user.setEmailVerified(false);
        user.setVerificationToken(UUID.randomUUID().toString());
        
    // Save user to database
        User savedUser = userRepository.save(user);
        
        // Send verification email
        emailService.sendVerificationEmail(savedUser);
        
        return savedUser;
    }
    
    @Transactional
    public User verifyEmail(String token) {
        // Find user by verification token
        User user = userRepository.findByVerificationToken(token)
            .orElseThrow(() -> new RuntimeException("Invalid verification token"));
        
        // Mark user as verified and clear token
        user.setEmailVerified(true);
        user.setVerificationToken(null);
        
        return userRepository.save(user);
    }
    
    public String loginUser(String email, String password) {
        // Find user by email
        User user = userRepository.findByEmail(email)
            .orElseThrow(() -> new RuntimeException("User not found with email: " + email));
        
        // Check if email is verified
        if (!user.getEmailVerified()) {
            throw new RuntimeException("Please verify your email before logging in");
        }
        
        // Check password
        if (!passwordEncoder.matches(password, user.getPassword())) {
            throw new RuntimeException("Invalid password");
        }
        
        // Generate JWT token
        return jwtUtil.generateToken(user.getEmail(), user.getRole().toString(), user.getId());
    }
    
    @Transactional
    public User requestPasswordReset(String email) {
        // Find user by email
        User user = userRepository.findByEmail(email)
            .orElseThrow(() -> new RuntimeException("User not found with email: " + email));
        
        // Generate reset token and set expiry (15 minutes from now)
        user.setPasswordResetToken(UUID.randomUUID().toString());
        user.setPasswordResetExpiry(LocalDateTime.now().plusMinutes(15));
        
        User savedUser = userRepository.save(user);
        
        // Send password reset email
        emailService.sendPasswordResetEmail(savedUser);
        
        return savedUser;
    }
    
    @Transactional
    public User resetPassword(String token, String newPassword) {
        // Find user by reset token
        User user = userRepository.findByPasswordResetToken(token)
            .orElseThrow(() -> new RuntimeException("Invalid reset token"));
        
        // Check if token is expired
        if (user.getPasswordResetExpiry().isBefore(LocalDateTime.now())) {
            throw new RuntimeException("Reset token has expired");
        }
        
        // Update password and clear reset token
        user.setPassword(passwordEncoder.encode(newPassword));
        user.setPasswordResetToken(null);
        user.setPasswordResetExpiry(null);
        
        return userRepository.save(user);
    }
    
    public User getByEmail(String email) {
        return userRepository.findByEmail(email)
            .orElseThrow(() -> new RuntimeException("User not found with email: " + email));
    }
    
    /**
     * ✅ IMPLEMENTED: Get total number of orders placed by user
     * This calculates the complete order history count for dashboard display
     */
    public Integer getUserTotalOrders(Long userId) {
        try {
            User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("User not found"));
            
            long orderCount = printJobRepository.countByCustomer(user);
            return Math.toIntExact(orderCount);
        } catch (Exception e) {
            // Return 0 if there's any error counting orders
            return 0;
        }
    }
}
