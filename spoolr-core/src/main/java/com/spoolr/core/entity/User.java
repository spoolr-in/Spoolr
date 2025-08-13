package com.spoolr.core.entity;

import com.spoolr.core.enums.UserRole;
import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "users")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class User {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, unique = true)
    private String email;
    
    @Column(nullable = false)
    private String name;
    
    @Column(nullable = false)
    private String password;  // Hashed password - never store plain text
    
    @Column(name = "phone_number")
    private String phoneNumber;
    
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private UserRole role = UserRole.CUSTOMER;  // Default to CUSTOMER
    
    @Column(name = "email_verified", nullable = false)
    private Boolean emailVerified = false;  // Default to false
    
    @Column(name = "verification_token")
    private String verificationToken;  // Token for email verification
    
    @Column(name = "password_reset_token")
    private String passwordResetToken;  // Token for password reset
    
    @Column(name = "password_reset_expiry")
    private LocalDateTime passwordResetExpiry;  // Expiry time for password reset token
    
    @CreationTimestamp
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;
    
    @UpdateTimestamp
    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;
}
