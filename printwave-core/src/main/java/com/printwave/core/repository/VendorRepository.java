package com.printwave.core.repository;

import com.printwave.core.entity.Vendor;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface VendorRepository extends JpaRepository<Vendor, Long> {

    // Find vendor by email
    Optional<Vendor> findByEmail(String email);

    // Find vendor by activation key
    Optional<Vendor> findByActivationKey(String activationKey);

    // Check if vendor exists by store code
    boolean existsByStoreCode(String storeCode);

    // Find vendor by store code
    Optional<Vendor> findByStoreCode(String storeCode);
}

