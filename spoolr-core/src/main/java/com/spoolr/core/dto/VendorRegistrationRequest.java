package com.spoolr.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class VendorRegistrationRequest {
    
    // Core Identity
    private String email;
    
    // Business Details
    private String businessName;
    private String contactPersonName;
    private String phoneNumber;
    private String businessAddress;
    private String city;
    private String state;
    private String zipCode;
    
    // Location (from maps integration)
    private Double latitude;
    private Double longitude;
    
    // Pricing Structure
    private BigDecimal pricePerPageBWSingleSided;
    private BigDecimal pricePerPageBWDoubleSided;
    private BigDecimal pricePerPageColorSingleSided;
    private BigDecimal pricePerPageColorDoubleSided;
    
    // Optional settings
    private Boolean enableDirectOrders; // defaults to true if not provided
    private Boolean isActive; // defaults to true if not provided
    private Boolean isStoreOpen; // defaults to false if not provided
}
