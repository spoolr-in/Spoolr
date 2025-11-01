package com.spoolr.core.service;

import com.spoolr.core.entity.Vendor;
import com.spoolr.core.repository.VendorRepository;
import com.spoolr.core.util.JwtUtil;
import com.spoolr.core.dto.VendorAuthResult; // Import the new DTO
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;
import java.util.Random;

@Service
public class VendorService {
    
    @Autowired
    private VendorRepository vendorRepository;
    
    @Autowired
    private EmailService emailService;

    @Autowired
    private JwtUtil jwtUtil;
    
    /**
     * Register a new vendor with business details
     * Step 1 of two-step registration process
     * Only sets system-generated values, not user-provided ones
     */
    @Transactional
    public Vendor registerVendor(Vendor vendor) {
        if (vendorRepository.existsByEmail(vendor.getEmail())) {
            throw new RuntimeException("Vendor with email " + vendor.getEmail() + " already exists");
        }
        
        vendor.setEmailVerified(false);
        vendor.setVerificationToken(UUID.randomUUID().toString());
        vendor.setActivationKeySent(false);
        
        vendor.setStoreCode(generateUniqueStoreCode());
        vendor.generateQRCodeUrl();
        
        Vendor savedVendor = vendorRepository.save(vendor);
        
        emailService.sendVendorVerificationEmail(savedVendor);
        
        return savedVendor;
    }
    
    /**
     * Verify vendor email address
     * Step 2 of two-step registration process
     */
    @Transactional
    public Vendor verifyVendorEmail(String token) {
        Vendor vendor = vendorRepository.findByVerificationToken(token)
            .orElseThrow(() -> new RuntimeException("Invalid verification token"));

        // If the email is already verified, do nothing further.
        // This makes the endpoint idempotent and prevents re-sending activation keys.
        if (vendor.getEmailVerified() != null && vendor.getEmailVerified()) {
            return vendor;
        }

        vendor.setEmailVerified(true);
        vendor.setVerificationToken(null); // This is now safe to do.

        vendor.setActivationKey(generateActivationKey());
        vendor.setActivationKeySent(true);

        Vendor savedVendor = vendorRepository.save(vendor);

        emailService.sendVendorActivationEmail(savedVendor);

        return savedVendor;
    }
    
    /**
     * Authenticate vendor using activation key (for Station app)
     */
    @Transactional
    public VendorAuthResult authenticateVendorByActivationKey(String activationKey) {
        Vendor vendor = vendorRepository.findByActivationKey(activationKey)
            .orElseThrow(() -> new RuntimeException("Invalid activation key"));
        
        if (!vendor.getIsActive()) {
            throw new RuntimeException("Vendor account is deactivated");
        }
        
        if (!vendor.getEmailVerified()) {
            throw new RuntimeException("Email not verified. Please verify your email first.");
        }
        
        vendor.setLastLoginAt(LocalDateTime.now());
        vendor.setStationAppConnected(true);
        
        vendorRepository.save(vendor);

        String token = jwtUtil.generateToken(vendor.getEmail(), "VENDOR", vendor.getId());
        return new VendorAuthResult(vendor, token);
    }
    
    /**
     * Toggle store open/closed status
     */
    @Transactional
    public Vendor toggleStoreStatus(Long vendorId, boolean isOpen) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        vendor.updateStoreStatus(isOpen);
        return vendorRepository.save(vendor);
    }
    
    /**
     * Update vendor's printer capabilities (from Station app)
     */
    @Transactional
    public Vendor updatePrinterCapabilities(Long vendorId, String capabilitiesJson) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        vendor.setPrinterCapabilities(capabilitiesJson);
        return vendorRepository.save(vendor);
    }
    
    /**
     * Update Station app connection status
     */
    @Transactional
    public Vendor updateStationAppConnection(Long vendorId, boolean isConnected) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        vendor.setStationAppConnected(isConnected);
        if (!isConnected) {
            vendor.setLastLoginAt(LocalDateTime.now());
        }
        
        return vendorRepository.save(vendor);
    }
    
    /**
     * Find vendor by store code (for QR code workflow)
     */
    public Vendor getVendorByStoreCode(String storeCode) {
        return vendorRepository.findByStoreCode(storeCode)
            .orElseThrow(() -> new RuntimeException("Store not found with code: " + storeCode));
    }
    
    /**
     * Get vendor by email
     */
    public Vendor getVendorByEmail(String email) {
        return vendorRepository.findByEmail(email)
            .orElseThrow(() -> new RuntimeException("Vendor not found with email: " + email));
    }
    
    /**
     * Get vendor by ID
     */
    public Vendor getVendorById(Long id) {
        return vendorRepository.findById(id)
            .orElseThrow(() -> new RuntimeException("Vendor not found with ID: " + id));
    }
    
    /**
     * Get all vendors ready for orders
     * More flexible - can accept orders even without printer capabilities initially
     */
    public List<Vendor> getVendorsReadyForOrders() {
        return vendorRepository.findAll().stream()
            .filter(this::isVendorReadyForOrders)
            .toList();
    }
    
    /**
     * Check if vendor is ready for orders - more flexible approach
     * Basic requirements: active, email verified, store open
     * Station app connection is checked but not required (for testing)
     */
    public boolean isVendorReadyForOrders(Vendor vendor) {
        return vendor.getIsActive() && 
               vendor.getEmailVerified() && 
               vendor.getIsStoreOpen();
    }
    
    /**
     * Strict vendor readiness check - all requirements
     * Use this for production when Station app is fully implemented
     */
    public boolean isVendorFullyReady(Vendor vendor) {
        return vendor.getIsActive() && 
               vendor.getEmailVerified() && 
               vendor.getIsStoreOpen() &&
               vendor.getStationAppConnected() &&
               vendor.getPrinterCapabilities() != null;
    }
    
    /**
     * Get vendors within distance of coordinates (for location-based matching)
     */
    public List<Vendor> getVendorsNearLocation(double latitude, double longitude, double radiusKm) {
        return vendorRepository.findAll().stream()
            .filter(this::isVendorReadyForOrders)
            .filter(vendor -> {
                double distance = calculateDistance(latitude, longitude, 
                    vendor.getLatitude(), vendor.getLongitude());
                return distance <= radiusKm;
            })
            .toList();
    }
    
    /**
     * Calculate distance between two points using Haversine formula
     * Returns distance in kilometers
     */
    public double calculateDistance(double lat1, double lon1, double lat2, double lon2) {
        final int R = 6371; // Radius of the earth in km
        
        double latDistance = Math.toRadians(lat2 - lat1);
        double lonDistance = Math.toRadians(lon2 - lon1);
        double a = Math.sin(latDistance / 2) * Math.sin(latDistance / 2)
                + Math.cos(Math.toRadians(lat1)) * Math.cos(Math.toRadians(lat2))
                * Math.sin(lonDistance / 2) * Math.sin(lonDistance / 2);
        double c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        double distance = R * c;
        
        return distance;
    }
    
    /**
     * Generate unique store code in format "PW0001"
     */
    private String generateUniqueStoreCode() {
        String storeCode;
        do {
            long count = vendorRepository.count();
            storeCode = String.format("PW%04d", count + 1);
        } while (vendorRepository.existsByStoreCode(storeCode));
        
        return storeCode;
    }
    
    /**
     * Generate activation key in format "PW-ABC123-XYZ789"
     */
    private String generateActivationKey() {
        String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random random = new Random();
        
        StringBuilder part1 = new StringBuilder();
        StringBuilder part2 = new StringBuilder();
        
        for (int i = 0; i < 6; i++) {
            part1.append(chars.charAt(random.nextInt(chars.length())));
            part2.append(chars.charAt(random.nextInt(chars.length())));
        }
        
        return "PW-" + part1.toString() + "-" + part2.toString();
    }
    
    /**
     * Get all vendors (for admin/testing)
     */
    public List<Vendor> getAllVendors() {
        return vendorRepository.findAll();
    }
    
    /**
     * Deactivate vendor account
     */
    @Transactional
    public Vendor deactivateVendor(Long vendorId) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        vendor.setIsActive(false);
        vendor.setIsStoreOpen(false);
        vendor.setStationAppConnected(false);
        
        return vendorRepository.save(vendor);
    }
    
    /**
     * Reactivate vendor account
     */
    @Transactional
    public Vendor reactivateVendor(Long vendorId) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        vendor.setIsActive(true);
        
        return vendorRepository.save(vendor);
    }
    
    // ============ PASSWORD-BASED AUTHENTICATION ============
    
    /**
     * First-time login with activation key + password setup
     * This replaces the old station-login for first-time users
     */
    @Transactional
    public VendorAuthResult firstTimeLoginWithPasswordSetup(String activationKey, String newPassword) {
        Vendor vendor = vendorRepository.findByActivationKey(activationKey)
            .orElseThrow(() -> new RuntimeException("Invalid activation key"));
        
        if (!vendor.getIsActive()) {
            throw new RuntimeException("Vendor account is deactivated");
        }
        
        if (!vendor.getEmailVerified()) {
            throw new RuntimeException("Email not verified. Please verify your email first.");
        }
        
        if (vendor.getPasswordSet() != null && vendor.getPasswordSet()) {
            throw new RuntimeException("Password already set. Please use regular login.");
        }
        
        if (newPassword == null || newPassword.trim().length() < 6) {
            throw new RuntimeException("Password must be at least 6 characters long");
        }
        
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        vendor.setPasswordHash(encoder.encode(newPassword));
        vendor.setPasswordSet(true);
        
        vendor.setLastLoginAt(LocalDateTime.now());
        vendor.setStationAppConnected(true);
        
        vendorRepository.save(vendor);

        String token = jwtUtil.generateToken(vendor.getEmail(), "VENDOR", vendor.getId());
        return new VendorAuthResult(vendor, token);
    }
    
    /**
     * Regular login with store code + password
     * For vendors who have already set up their password
     */
    @Transactional
    public VendorAuthResult loginWithStoreCodeAndPassword(String storeCode, String password) {
        Vendor vendor = vendorRepository.findByStoreCode(storeCode)
            .orElseThrow(() -> new RuntimeException("Invalid store code"));
        
        if (!vendor.getIsActive()) {
            throw new RuntimeException("Vendor account is deactivated");
        }
        
        if (!vendor.getEmailVerified()) {
            throw new RuntimeException("Email not verified. Please complete email verification first.");
        }
        
        if (vendor.getPasswordSet() == null || !vendor.getPasswordSet()) {
            throw new RuntimeException("Password not set. Please use activation key for first-time login.");
        }
        
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        if (!encoder.matches(password, vendor.getPasswordHash())) {
            throw new RuntimeException("Invalid password");
        }
        
        vendor.setLastLoginAt(LocalDateTime.now());
        vendor.setStationAppConnected(true);
        
        vendorRepository.save(vendor);

        String token = jwtUtil.generateToken(vendor.getEmail(), "VENDOR", vendor.getId());
        return new VendorAuthResult(vendor, token);
    }
    
    /**
     * Change password for existing vendor
     */
    @Transactional
    public Vendor changePassword(Long vendorId, String currentPassword, String newPassword) {
        Vendor vendor = vendorRepository.findById(vendorId)
            .orElseThrow(() -> new RuntimeException("Vendor not found"));
        
        if (vendor.getPasswordSet() == null || !vendor.getPasswordSet()) {
            throw new RuntimeException("No password set. Please use first-time login.");
        }
        
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        if (!encoder.matches(currentPassword, vendor.getPasswordHash())) {
            throw new RuntimeException("Current password is incorrect");
        }
        
        if (newPassword == null || newPassword.trim().length() < 6) {
            throw new RuntimeException("New password must be at least 6 characters long");
        }
        
        vendor.setPasswordHash(encoder.encode(newPassword));
        
        return vendorRepository.save(vendor);
    }
    
    /**
     * Reset password using activation key (forgot password)
     */
    @Transactional
    public Vendor resetPasswordWithActivationKey(String activationKey, String newPassword) {
        Vendor vendor = vendorRepository.findByActivationKey(activationKey)
            .orElseThrow(() -> new RuntimeException("Invalid activation key"));
        
        if (!vendor.getIsActive()) {
            throw new RuntimeException("Vendor account is deactivated");
        }
        
        if (!vendor.getEmailVerified()) {
            throw new RuntimeException("Email not verified");
        }
        
        if (newPassword == null || newPassword.trim().length() < 6) {
            throw new RuntimeException("Password must be at least 6 characters long");
        }
        
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        vendor.setPasswordHash(encoder.encode(newPassword));
        vendor.setPasswordSet(true);
        
        return vendorRepository.save(vendor);
    }
}
