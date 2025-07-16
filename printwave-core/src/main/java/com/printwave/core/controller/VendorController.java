package com.printwave.core.controller;

import com.printwave.core.dto.*;
import com.printwave.core.entity.Vendor;
import com.printwave.core.service.VendorService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/vendors")
public class VendorController {

    @Autowired
    private VendorService vendorService;

    @PostMapping("/register")
    public ResponseEntity<ApiResponse> registerVendor(@RequestBody VendorRegistrationRequest request) {
        try {
            // Create vendor entity from request
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
            vendor.setPricePerPageColorDoubleSided(request.getPricePerPageColorDoubleSided());
            
            // Set optional fields with defaults
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
            Vendor vendor = vendorService.authenticateVendorByActivationKey(request.getActivationKey());
            
            // Create comprehensive login response
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
            response.setPricePerPageColorDoubleSided(vendor.getPricePerPageColorDoubleSided());
            
            response.setPrinterCapabilities(vendor.getPrinterCapabilities());
            response.setLastLoginAt(vendor.getLastLoginAt());
            response.setMessage("Login successful! Welcome " + vendor.getBusinessName());
            
            return ResponseEntity.ok(response);
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(new VendorLoginResponse("Error: " + e.getMessage()));
        }
    }

    @PostMapping("/{vendorId}/toggle-store")
    public ResponseEntity<ApiResponse> toggleStoreStatus(@PathVariable Long vendorId, @RequestBody StoreStatusRequest request) {
        try {
            Vendor updatedVendor = vendorService.toggleStoreStatus(vendorId, request.getIsOpen());
            return ResponseEntity.ok(ApiResponse.success("Store status updated successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }

    @PostMapping("/{vendorId}/update-capabilities")
    public ResponseEntity<ApiResponse> updatePrinterCapabilities(@PathVariable Long vendorId, @RequestBody PrinterCapabilitiesRequest request) {
        try {
            Vendor updatedVendor = vendorService.updatePrinterCapabilities(vendorId, request.getCapabilities());
            return ResponseEntity.ok(ApiResponse.success("Printer capabilities updated successfully."));
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(ApiResponse.error(e.getMessage()));
        }
    }
}
