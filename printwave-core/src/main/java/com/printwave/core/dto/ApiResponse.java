package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ApiResponse {
    
    private String message;
    private boolean success;
    
    // Constructor for success messages
    public ApiResponse(String message) {
        this.message = message;
        this.success = true;
    }
    
    // Static factory methods
    public static ApiResponse success(String message) {
        return new ApiResponse(message, true);
    }
    
    public static ApiResponse error(String message) {
        return new ApiResponse(message, false);
    }
}
