package com.printwave.core.repository;

import com.printwave.core.entity.User;
import com.printwave.core.enums.UserRole;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface UserRepository extends JpaRepository<User, Long> {
    
    // Find user by email (for login)
    Optional<User> findByEmail(String email);
    
    // Find all users by role
    List<User> findByRole(UserRole role);
    
    // Check if email already exists (for registration)
    boolean existsByEmail(String email);
    
    // Find user by verification token (for email verification)
    Optional<User> findByVerificationToken(String verificationToken);
    
    // Find user by password reset token (for password reset)
    Optional<User> findByPasswordResetToken(String passwordResetToken);
    
    // Find users by name containing text (search functionality)
    List<User> findByNameContainingIgnoreCase(String name);
}
