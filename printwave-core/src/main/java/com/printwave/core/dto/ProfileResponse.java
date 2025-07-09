package com.printwave.core.dto;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ProfileResponse {
    private Long id;
    private String email;
    private String name;
    private String phoneNumber;
    private String role;
    private Boolean emailVerified;
    private String message;
}
