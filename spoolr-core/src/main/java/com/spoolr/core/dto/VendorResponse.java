package com.spoolr.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Data
@NoArgsConstructor
public class VendorResponse {
    
    private Long id;
    private String email;
    private String businessName;
    private String contactPersonName;
    private String phoneNumber;
    private String businessAddress;
    private String city;
    private String state;
    private String zipCode;
    
    // Location
    private Double latitude;
    private Double longitude;
    
    // Pricing
    private BigDecimal pricePerPageBWSingleSided;
    private BigDecimal pricePerPageBWDoubleSided;
    private BigDecimal pricePerPageColorSingleSided;
    private BigDecimal pricePerPageColorDoubleSided;
    
    // Status
    private Boolean emailVerified;
    private Boolean isActive;
    private Boolean isStoreOpen;
    private Boolean stationAppConnected;
    
    // QR Code
    private String storeCode;
    private String qrCodeUrl;
    
    // Registration Status
    private Boolean activationKeySent;
    
    // Timestamps
    private LocalDateTime registeredAt;
    private LocalDateTime lastLoginAt;
    
    // Response message
    private String message;
    
    // Constructor for simple responses with message
    public VendorResponse(String message) {
        this.message = message;
    }
}
