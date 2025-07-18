package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class VendorLoginRequest {
    
    private String storeCode;  // Store code like "PW0001"
    private String password;   // Password set during first login
}
