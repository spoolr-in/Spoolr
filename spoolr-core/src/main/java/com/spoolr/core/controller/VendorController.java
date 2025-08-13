package com.spoolr.core.controller;

import com.spoolr.core.dto.*;
import com.spoolr.core.entity.Vendor;
import com.spoolr.core.service.VendorService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import jakarta.servlet.http.HttpServletRequest;

@RestController
@RequestMapping("/api/vendors")
public class VendorController {

    @Autowired
    private VendorService vendorService;

    @PostMapping("/register")
    public ResponseEntity<ApiResponse> registerVendor(@RequestBody VendorRegistrationRequest request) {
        try {
            Vendor vendor = new Vendor();
            vendor.setEmail(request.getEmail());
            vendor.setBusinessName(request.getBusinessName());
            vendor.setContactPersonName(request.getContactPersonName());
            vendor.setPhoneNumber(request.getPhoneNumber());
            vendor.setBusinessAddress(request.getBusinessAddress());
            vendor.setCity(request.getCity());
            vendor.setState(request.getState());
            vendor.setZipCode(request.getZipCode());
            vendor.setLatitude(request.getLatitude());
            vendor.setLongitude(request.getLongitude());
            vendor.setPricePerPageBWSingleSided(request.getPricePerPageBWSingleSided());
            vendor.setPricePerPageBWDoubleSided(request.getPricePerPageBWDoubleSided());
            vendor.setPricePerPageColorSingleSided(request.getPricePerPageColorSingleSided());
            vendor.setPricePerPageColorDoubleSided(request.getPricePerPageColorDoubleSided()); // Corrected typo
            
            vendor.setEnableDirectOrders(request.getEnableDirectOrders() != null ? request.getEnableDirectOrders() : true);
            vendor.setIsActive(request.getIsActive() != null ? request.getIsActive() : true);
            vendor.setIsStoreOpen(request.getIsStoreOpen() != null ? request.getIsStoreOpen() : false);

            Vendor savedVendor = vendorService.registerVendor(vendor);
            return ResponseEntity.ok(ApiResponse.success("Vendor registered successfully. Please verify your email."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }

    @GetMapping("/verify-email")
    public ResponseEntity<ApiResponse> verifyVendorEmail(@RequestParam String token) {
        try {
            Vendor verifiedVendor = vendorService.verifyVendorEmail(token);
            return ResponseEntity.ok(ApiResponse.success("Email verified successfully. Activation key has been sent."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }

    @PostMapping("/station-login")
    public ResponseEntity<VendorLoginResponse> stationLogin(@RequestBody StationLoginRequest request) {
        try {
            VendorAuthResult authResult = vendorService.authenticateVendorByActivationKey(request.getActivationKey());
            Vendor vendor = authResult.getVendor();
            String token = authResult.getToken();
            
            VendorLoginResponse response = createLoginResponse(vendor);
            response.setToken(token);
            response.setMessage("Login successful! Welcome " + vendor.getBusinessName());
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(new VendorLoginResponse("Error: " + e.getMessage()));
        }
    }

    @PostMapping("/{vendorId}/toggle-store")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<ApiResponse> toggleStoreStatus(@PathVariable Long vendorId, @RequestBody StoreStatusRequest request, HttpServletRequest httpRequest) {
        try {
            // Verify that the authenticated vendor can only access their own resources
            Long authenticatedVendorId = (Long) httpRequest.getAttribute("userId");
            if (!vendorId.equals(authenticatedVendorId)) {
                return ResponseEntity.status(403).body(ApiResponse.error("Access denied: You can only manage your own store"));
            }
            
            Vendor updatedVendor = vendorService.toggleStoreStatus(vendorId, request.getIsOpen());
            return ResponseEntity.ok(ApiResponse.success("Store status updated successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }

    @PostMapping("/{vendorId}/update-capabilities")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<ApiResponse> updatePrinterCapabilities(@PathVariable Long vendorId, @RequestBody PrinterCapabilitiesRequest request, HttpServletRequest httpRequest) {
        try {
            // Verify that the authenticated vendor can only access their own resources
            Long authenticatedVendorId = (Long) httpRequest.getAttribute("userId");
            if (!vendorId.equals(authenticatedVendorId)) {
                return ResponseEntity.status(403).body(ApiResponse.error("Access denied: You can only manage your own printer capabilities"));
            }
            
            Vendor updatedVendor = vendorService.updatePrinterCapabilities(vendorId, request.getCapabilities());
            return ResponseEntity.ok(ApiResponse.success("Printer capabilities updated successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }
    
    // ============ PASSWORD-BASED AUTHENTICATION ENDPOINTS ============
    
    @PostMapping("/first-time-login")
    public ResponseEntity<?> firstTimeLogin(@RequestBody FirstTimeLoginRequest request) {
        try {
            VendorAuthResult authResult = vendorService.firstTimeLoginWithPasswordSetup(request.getActivationKey(), request.getNewPassword());
            Vendor vendor = authResult.getVendor();
            String token = authResult.getToken();
            
            VendorLoginResponse response = createLoginResponse(vendor);
            response.setToken(token);
            response.setMessage("First-time login successful! Password set. Welcome " + vendor.getBusinessName());
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ErrorResponse.of(e.getMessage()));
        }
    }
    
    @PostMapping("/login")
    public ResponseEntity<?> login(@RequestBody VendorLoginRequest request) {
        try {
            VendorAuthResult authResult = vendorService.loginWithStoreCodeAndPassword(request.getStoreCode(), request.getPassword());
            Vendor vendor = authResult.getVendor();
            String token = authResult.getToken();
            
            VendorLoginResponse response = createLoginResponse(vendor);
            response.setToken(token);
            response.setMessage("Login successful! Welcome back " + vendor.getBusinessName());
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ErrorResponse.of(e.getMessage()));
        }
    }
    
    @PostMapping("/change-password")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<ApiResponse> changePassword(@RequestBody ChangePasswordRequest request, HttpServletRequest httpRequest) {
        try {
            // Verify that the authenticated vendor can only change their own password
            Long authenticatedVendorId = (Long) httpRequest.getAttribute("userId");
            if (!request.getVendorId().equals(authenticatedVendorId)) {
                return ResponseEntity.status(403).body(ApiResponse.error("Access denied: You can only change your own password"));
            }
            
            vendorService.changePassword(request.getVendorId(), request.getCurrentPassword(), request.getNewPassword());
            return ResponseEntity.ok(ApiResponse.success("Password changed successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }
    
    @PostMapping("/reset-password")
    public ResponseEntity<ApiResponse> resetPassword(@RequestBody ResetPasswordRequest request) {
        try {
            vendorService.resetPasswordWithActivationKey(request.getActivationKey(), request.getNewPassword());
            return ResponseEntity.ok(ApiResponse.success("Password reset successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }
    
    // Helper method to create login response
    private VendorLoginResponse createLoginResponse(Vendor vendor) {
        VendorLoginResponse response = new VendorLoginResponse();
        response.setVendorId(vendor.getId());
        response.setBusinessName(vendor.getBusinessName());
        response.setEmail(vendor.getEmail());
        response.setContactPersonName(vendor.getContactPersonName());
        response.setPhoneNumber(vendor.getPhoneNumber());
        
        response.setIsStoreOpen(vendor.getIsStoreOpen());
        response.setStationAppConnected(vendor.getStationAppConnected());
        response.setStoreStatusUpdatedAt(vendor.getStoreStatusUpdatedAt());
        
        response.setStoreCode(vendor.getStoreCode());
        response.setQrCodeUrl(vendor.getQrCodeUrl());
        
        response.setPricePerPageBWSingleSided(vendor.getPricePerPageBWSingleSided());
        response.setPricePerPageBWDoubleSided(vendor.getPricePerPageBWDoubleSided());
        response.setPricePerPageColorSingleSided(vendor.getPricePerPageColorSingleSided());
        response.setPricePerPageColorDoubleSided(vendor.getPricePerPageColorDoubleSided()); // Corrected typo
        
        response.setPrinterCapabilities(vendor.getPrinterCapabilities());
        response.setLastLoginAt(vendor.getLastLoginAt());
        response.setPasswordSet(vendor.getPasswordSet());
        
        return response;
    }
}