package com.spoolr.core.repository;

import com.spoolr.core.entity.Vendor;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface VendorRepository extends JpaRepository<Vendor, Long> {

    // Find vendor by email - Used for vendor profile lookup and login validation
    Optional<Vendor> findByEmail(String email);
    
    // Check if email already exists - Used during registration to prevent duplicate emails
    boolean existsByEmail(String email);

    // Find vendor by verification token - Used for email verification process
    // When vendor clicks verification link, we need to find vendor by token
    Optional<Vendor> findByVerificationToken(String verificationToken);

    // Find vendor by activation key - Used for Station app authentication
    // When vendor logs into Station app with activation key
    Optional<Vendor> findByActivationKey(String activationKey);

    // Check if store code exists - Used to generate unique store codes for QR codes
    // Ensures no duplicate store codes when creating new vendors
    boolean existsByStoreCode(String storeCode);

    // Find vendor by store code - Used for QR code scanning workflow
    // When customer scans QR code, we find the vendor by store code
    Optional<Vendor> findByStoreCode(String storeCode);
}

