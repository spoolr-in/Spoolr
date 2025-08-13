package com.spoolr.core.dto;

import com.spoolr.core.entity.Vendor;
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
