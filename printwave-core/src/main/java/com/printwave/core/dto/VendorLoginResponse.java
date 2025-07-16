package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class VendorLoginResponse {
    
    // Essential vendor info
    private Long vendorId;
    private String businessName;
    private String email;
    private String contactPersonName;
    private String phoneNumber;
    
    // Store status
    private Boolean isStoreOpen;
    private Boolean stationAppConnected;
    private LocalDateTime storeStatusUpdatedAt;
    
    // QR Code info
    private String storeCode;
    private String qrCodeUrl;
    
    // Pricing info (for Station app display)
    private BigDecimal pricePerPageBWSingleSided;
    private BigDecimal pricePerPageBWDoubleSided;
    private BigDecimal pricePerPageColorSingleSided;
    private BigDecimal pricePerPageColorDoubleSided;
    
    // Printer capabilities
    private String printerCapabilities;
    
    // Login info
    private String message;
    private LocalDateTime lastLoginAt;
    
    // Constructor for successful login
    public VendorLoginResponse(String message) {
        this.message = message;
    }
}
