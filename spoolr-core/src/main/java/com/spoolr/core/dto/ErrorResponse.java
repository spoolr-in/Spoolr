package com.spoolr.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ErrorResponse {
    
    private boolean success = false;
    private String message;
    private String error;
    
    // Constructor for simple error message
    public ErrorResponse(String message) {
        this.success = false;
        this.message = message;
        this.error = message;
    }
    
    // Static factory method for cleaner creation
    public static ErrorResponse of(String message) {
        return new ErrorResponse(message);
    }
}
