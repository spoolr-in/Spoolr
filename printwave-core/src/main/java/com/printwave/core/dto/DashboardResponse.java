package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class DashboardResponse {
    private String message;
    private String email;
    private Long userId;
    private String details;
    private String welcomeMessage;
    private Integer totalOrders;
    private String accountStatus;
    
    // Constructor for basic response (backward compatibility)
    public DashboardResponse(String message, String email, Long userId, String details) {
        this.message = message;
        this.email = email;
        this.userId = userId;
        this.details = details;
        this.welcomeMessage = "Welcome to PrintWave!";
        this.totalOrders = 0;
        this.accountStatus = "Active";
    }
}
