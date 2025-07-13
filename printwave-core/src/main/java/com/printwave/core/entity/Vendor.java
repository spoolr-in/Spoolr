package com.printwave.core.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Entity
@Table(name = "vendors")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Vendor {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, unique = true)
    private String email;
    
    @Column(nullable = false)
    private String password;  // Hashed password - never store plain text
    
    @Column(name = "business_name", nullable = false)
    private String businessName;
    
    @Column(name = "contact_person_name", nullable = false)
    private String contactPersonName;
    
    @Column(nullable = false)
    private String address;
    
    @Column(nullable = false)
    private String city;
    
    @Column(name = "phone_number", nullable = false)
    private String phoneNumber;
    
    @Column(name = "business_type", nullable = false)
    private String businessType = "PRINT_SHOP";  // Default to PRINT_SHOP
    
    @Column(name = "color_printing_available", nullable = false)
    private Boolean colorPrintingAvailable = false;  // Default to false
    
    @Column(name = "duplex_printing_available", nullable = false)
    private Boolean duplexPrintingAvailable = false;  // Default to false
    
    @Column(name = "maximum_print_quantity", nullable = false)
    private Integer maximumPrintQuantity = 100;  // Default to 100 pages
    
    @Column(name = "black_white_price_per_page", nullable = false, precision = 10, scale = 2)
    private BigDecimal blackWhitePricePerPage = BigDecimal.ZERO;
    
    @Column(name = "color_price_per_page", nullable = false, precision = 10, scale = 2)
    private BigDecimal colorPricePerPage = BigDecimal.ZERO;
    
    @Column(name = "activation_key", unique = true)
    private String activationKey;  // Unique key for Station app setup
    
    @Column(name = "is_activated", nullable = false)
    private Boolean isActivated = false;  // Default to false
    
    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;  // Default to true
    
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
