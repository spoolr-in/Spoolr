package com.printwave.core.dto;

import com.printwave.core.entity.Vendor;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class VendorAuthResult {
    private Vendor vendor;
    private String token;
}
