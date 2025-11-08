package com.spoolr.core.entity;

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

    // Core Identity
    @Column(nullable = false, unique = true)
    private String email;

    @Column(name = "activation_key", unique = true)
    private String activationKey;  // "PW-ABC123-XYZ789" for Station app login

    // Password Authentication (for subsequent logins)
    @Column(name = "password_hash")
    private String passwordHash;  // BCrypt hashed password

    @Column(name = "password_set")
    private Boolean passwordSet = false;  // Has vendor set up password?

    // Business Details (Portal Registration)
    @Column(name = "business_name", nullable = false)
    private String businessName;

    @Column(name = "contact_person_name", nullable = false)
    private String contactPersonName;

    @Column(name = "phone_number", nullable = false)
    private String phoneNumber;

    @Column(name = "business_address", nullable = false)
    private String businessAddress;

    @Column(nullable = false)
    private String city;

    @Column(nullable = false)
    private String state;

    @Column(name = "zip_code", nullable = false)
    private String zipCode;

    // Location (For Distance Calculations)
    @Column(nullable = false)
    private Double latitude;

    @Column(nullable = false)
    private Double longitude;

    // Pricing Structure (Portal Registration)
    @Column(name = "price_per_page_bw_single_sided", nullable = false, precision = 10, scale = 2)
    private BigDecimal pricePerPageBWSingleSided;

    @Column(name = "price_per_page_bw_double_sided", nullable = false, precision = 10, scale = 2)
    private BigDecimal pricePerPageBWDoubleSided;

    @Column(name = "price_per_page_color_single_sided", nullable = false, precision = 10, scale = 2)
    private BigDecimal pricePerPageColorSingleSided;

    @Column(name = "price_per_page_color_double_sided", nullable = false, precision = 10, scale = 2)
    private BigDecimal pricePerPageColorDoubleSided;

    // Email Verification (Two-Step Process)
    @Column(name = "email_verified", nullable = false)
    private Boolean emailVerified = false;

    @Column(name = "verification_token")
    private String verificationToken;

    @Column(name = "activation_key_sent", nullable = false)
    private Boolean activationKeySent = false;

    // QR Code & Direct Orders
    @Column(name = "store_code", unique = true)
    private String storeCode;  // "PW0001" - unique identifier for QR codes

    @Column(name = "qr_code_url")
    private String qrCodeUrl;  // "https://spoolr.tech/store/PW0001"

    @Column(name = "enable_direct_orders", nullable = false)
    private Boolean enableDirectOrders = true;  // Allow QR code orders

    // Store Status (Station App Control)
    @Column(name = "is_store_open", nullable = false)
    private Boolean isStoreOpen = false;  // Manual toggle by vendor

    @Column(name = "store_status_updated_at")
    private LocalDateTime storeStatusUpdatedAt;

    @Column(name = "station_app_connected", nullable = false)
    private Boolean stationAppConnected = false;  // Is Station app online

    // Printer Capabilities (Station App Updates)
    @Column(name = "printer_capabilities", columnDefinition = "TEXT")
    private String printerCapabilities;  // JSON string from Station app

    // Account Management
    @Column(name = "is_active", nullable = false)
    private Boolean isActive = true;  // Account enabled

    @CreationTimestamp
    @Column(name = "registered_at", nullable = false, updatable = false)
    private LocalDateTime registeredAt;

    @UpdateTimestamp
    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    @Column(name = "last_login_at")
    private LocalDateTime lastLoginAt;  // Last Station app login

    // Essential Helper Methods

    /**
     * Check if vendor is ready to receive orders
     * Used by job matching system
     */
    public boolean isReadyForOrders() {
        return isActive &&
                emailVerified &&
                isStoreOpen &&
                stationAppConnected;
    }

    /**
     * Check if vendor has completed registration process
     * Used by email service and admin dashboard
     */
    public boolean isRegistrationComplete() {
        return emailVerified &&
                activationKeySent &&
                activationKey != null;
    }

    /**
     * Generate QR code URL based on store code
     * Called during registration process
     */
    public void generateQRCodeUrl() {
        if (storeCode != null) {
            this.qrCodeUrl = "https://spoolr.tech/store/" + storeCode;
        }
    }

    /**
     * Update store status and timestamp together
     * Called by Station app when vendor toggles store
     */
    public void updateStoreStatus(boolean isOpen) {
        this.isStoreOpen = isOpen;
        this.storeStatusUpdatedAt = LocalDateTime.now();
    }
}