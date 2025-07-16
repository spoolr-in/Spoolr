package com.printwave.core.service;

import com.printwave.core.entity.User;
import com.printwave.core.entity.Vendor;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

@Service
public class EmailService {
    
    @Autowired
    private JavaMailSender mailSender;
    
    @Async
    public void sendVerificationEmail(User user) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(user.getEmail());
        message.setSubject("PrintWave - Email Verification");
        message.setText("Hello " + user.getName() + ",\n\n" +
                       "Welcome to PrintWave! Please click the link below to verify your email:\n\n" +
                       "http://localhost:8080/api/users/verify?token=" + user.getVerificationToken() + "\n\n" +
                       "If you didn't create an account, please ignore this email.\n\n" +
                       "Thank you,\nPrintWave Team");
        
        mailSender.send(message);
    }
    
    @Async
    public void sendPasswordResetEmail(User user) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(user.getEmail());
        message.setSubject("PrintWave - Password Reset Request");
        message.setText("Hello " + user.getName() + ",\n\n" +
                       "You requested a password reset. Please click the link below to reset your password:\n\n" +
                       "http://localhost:8080/api/users/reset-password?token=" + user.getPasswordResetToken() + "\n\n" +
                       "This link will expire in 15 minutes.\n" +
                       "If you didn't request this, please ignore this email.\n\n" +
                       "Thank you,\nPrintWave Team");
        
        mailSender.send(message);
    }
    
    // Vendor Email Methods
    
    @Async
    public void sendVendorVerificationEmail(Vendor vendor) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(vendor.getEmail());
        message.setSubject("PrintWave - Vendor Email Verification");
        message.setText("Hello " + vendor.getContactPersonName() + ",\n\n" +
                       "Welcome to PrintWave! Thank you for registering " + vendor.getBusinessName() + ".\n\n" +
                       "Please click the link below to verify your email address:\n\n" +
                       "http://localhost:8080/api/vendors/verify-email?token=" + vendor.getVerificationToken() + "\n\n" +
                       "After verification, you'll receive your Station App activation key.\n\n" +
                       "If you didn't register this business, please ignore this email.\n\n" +
                       "Thank you,\nPrintWave Team");
        
        mailSender.send(message);
    }
    
    @Async
    public void sendVendorActivationEmail(Vendor vendor) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(vendor.getEmail());
        message.setSubject("PrintWave - Station App Activation Key");
        message.setText("Hello " + vendor.getContactPersonName() + ",\n\n" +
                       "Congratulations! Your email has been verified for " + vendor.getBusinessName() + ".\n\n" +
                       "Here are your Station App details:\n\n" +
                       "🔑 Activation Key: " + vendor.getActivationKey() + "\n" +
                       "🏪 Store Code: " + vendor.getStoreCode() + "\n" +
                       "🌐 QR Code URL: " + vendor.getQrCodeUrl() + "\n\n" +
                       "Next Steps:\n" +
                       "1. Download the PrintWave Station App\n" +
                       "2. Log in using your activation key\n" +
                       "3. Connect your printers for auto-discovery\n" +
                       "4. Open your store to start receiving orders\n\n" +
                       "Your customers can now scan the QR code at your location to place orders!\n\n" +
                       "Thank you for joining PrintWave!\n\n" +
                       "PrintWave Team");
        
        mailSender.send(message);
    }
}
