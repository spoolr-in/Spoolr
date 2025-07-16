package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PrinterCapabilitiesRequest {
    
    private String capabilities; // JSON string of printer capabilities
}
