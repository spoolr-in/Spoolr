package com.printwave.core.service;

import com.printwave.core.entity.User;
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
}
