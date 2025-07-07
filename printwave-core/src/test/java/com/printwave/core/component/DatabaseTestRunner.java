package com.printwave.core.component;

import com.printwave.core.entity.User;
import com.printwave.core.enums.UserRole;
import com.printwave.core.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Optional;

@Component
@RequiredArgsConstructor
public class DatabaseTestRunner implements CommandLineRunner {
    
    private final UserRepository userRepository;
    
    @Override
    public void run(String... args) throws Exception {
        System.out.println("\n🧪 Testing UserRepository...\n");
        
        // Test 1: Save a new user (or find existing)
        System.out.println("📝 Test 1: Creating and saving a user...");
        String testEmail = "john.customer@printwave.com";
        
        User savedUser;
        if (userRepository.existsByEmail(testEmail)) {
            System.out.println("ℹ️  User already exists, fetching existing user...");
            savedUser = userRepository.findByEmail(testEmail).orElse(null);
        } else {
            User newUser = new User();
            newUser.setEmail(testEmail);
            newUser.setName("John Customer");
            newUser.setRole(UserRole.CUSTOMER);
            savedUser = userRepository.save(newUser);
            System.out.println("✅ New user created!");
        }
        
        System.out.println("✅ User ID: " + savedUser.getId());
        System.out.println("   Created at: " + savedUser.getCreatedAt());
        
        // Test 2: Find user by email
        System.out.println("\n🔍 Test 2: Finding user by email...");
        Optional<User> foundUser = userRepository.findByEmail("john.customer@printwave.com");
        if (foundUser.isPresent()) {
            System.out.println("✅ User found: " + foundUser.get().getName());
        } else {
            System.out.println("❌ User not found");
        }
        
        // Test 3: Check if email exists
        System.out.println("\n📧 Test 3: Checking if email exists...");
        boolean emailExists = userRepository.existsByEmail("john.customer@printwave.com");
        System.out.println("✅ Email exists: " + emailExists);
        
        // Test 4: Create a vendor user (or find existing)
        System.out.println("\n🏦 Test 4: Creating a vendor user...");
        String vendorEmail = "print.shop@printwave.com";
        
        if (!userRepository.existsByEmail(vendorEmail)) {
            User vendor = new User();
            vendor.setEmail(vendorEmail);
            vendor.setName("PrintShop Owner");
            vendor.setRole(UserRole.VENDOR);
            userRepository.save(vendor);
            System.out.println("✅ New vendor user created");
        } else {
            System.out.println("ℹ️  Vendor user already exists");
        }
        
        // Test 5: Find all customers
        System.out.println("\n👥 Test 5: Finding all customers...");
        List<User> customers = userRepository.findByRole(UserRole.CUSTOMER);
        System.out.println("✅ Found " + customers.size() + " customer(s)");
        
        // Test 6: Count total users
        System.out.println("\n📊 Test 6: Counting total users...");
        long totalUsers = userRepository.count();
        System.out.println("✅ Total users in database: " + totalUsers);
        
        System.out.println("\n🎉 All repository tests completed successfully!\n");
    }
}
