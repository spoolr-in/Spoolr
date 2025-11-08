package com.spoolr.core.service;

import com.spoolr.core.entity.PrintJob;
import com.spoolr.core.entity.User;
import com.spoolr.core.entity.Vendor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

@Service
public class EmailService {

    @Autowired
    private JavaMailSender mailSender;

    @Value("${frontend.base.url}")
    private String frontendBaseUrl;

    @Async
    public void sendVerificationEmail(User user) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(user.getEmail());
        message.setSubject("Spoolr - Email Verification");
        message.setText("Hello " + user.getName() + ",\n\n" +
                "Welcome to Spoolr! Please click the link below to verify your email:\n\n" +
                frontendBaseUrl + "/verify-email?token=" + user.getVerificationToken() + "\n\n" +
                "If you didn't create an account, please ignore this email.\n\n" +
                "Thank you,\nSpoolr Team");

        mailSender.send(message);
    }

    @Async
    public void sendPasswordResetEmail(User user) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(user.getEmail());
        message.setSubject("Spoolr - Password Reset Request");
        message.setText("Hello " + user.getName() + ",\n\n" +
                "You requested a password reset. Please click the link below to reset your password:\n\n" +
                frontendBaseUrl + "/reset-password?token=" + user.getPasswordResetToken() + "\n\n" +
                "This link will expire in 15 minutes.\n" +
                "If you didn't request this, please ignore this email.\n\n" +
                "Thank you,\nSpoolr Team");

        mailSender.send(message);
    }

    // Vendor Email Methods

    @Async
    public void sendVendorVerificationEmail(Vendor vendor) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(vendor.getEmail());
        message.setSubject("Spoolr - Vendor Email Verification");
        message.setText("Hello " + vendor.getContactPersonName() + ",\n\n" +
                "Welcome to Spoolr! Thank you for registering " + vendor.getBusinessName() + ".\n\n" +
                "Please click the link below to verify your email address:\n\n" +
                frontendBaseUrl + "/vendor/verify-email?token=" + vendor.getVerificationToken() + "\n\n" +
                "After verification, you'll receive your Station App activation key.\n\n" +
                "If you didn't register this business, please ignore this email.\n\n" +
                "Thank you,\nSpoolr Team");

        mailSender.send(message);
    }

    @Async
    public void sendVendorActivationEmail(Vendor vendor) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(vendor.getEmail());
        message.setSubject("Spoolr - Station App Activation Key");
        message.setText("Hello " + vendor.getContactPersonName() + ",\n\n" +
                       "Congratulations! Your email has been verified for " + vendor.getBusinessName() + ".\n\n" +
                       "Here are your Station App details:\n\n" +
                       "🔑 Activation Key: " + vendor.getActivationKey() + "\n" +
                       "🏪 Store Code: " + vendor.getStoreCode() + "\n" +
                       "🌐 QR Code URL: " + vendor.getQrCodeUrl() + "\n\n" +
                       "Next Steps:\n" +
                       "1. Download the Spoolr Station App\n" +
                       "2. Log in using your activation key\n" +
                       "3. Connect your printers for auto-discovery\n" +
                       "4. Open your store to start receiving orders\n\n" +
                       "Your customers can now scan the QR code at your location to place orders!\n\n" +
                       "Thank you for joining Spoolr!\n\n" +
                       "Spoolr Team");
        
        mailSender.send(message);
    }

    // ===== JOB STATUS EMAIL NOTIFICATIONS =====

    /**
     * Send email notification when job is accepted by vendor
     */
    @Async
    public void sendJobAcceptedEmail(PrintJob job) {
        // Only send email to registered customers (not anonymous)
        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            SimpleMailMessage message = new SimpleMailMessage();
            message.setTo(job.getCustomer().getEmail());
            message.setSubject("Spoolr - Your Print Job Has Been Accepted! 🎉");
            message.setText("Hello " + job.getCustomer().getName() + ",\n\n" +
                    "Great news! Your print job has been accepted by a vendor.\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Print Specs: " + job.getPrintSpecsSummary() + "\n" +
                    "• Total Price: ₹" + job.getTotalPrice() + "\n\n" +
                    "🏪 Vendor Details:\n" +
                    "• Business: " + job.getVendor().getBusinessName() + "\n" +
                    "• Address: " + job.getVendor().getBusinessAddress() + "\n" +
                    "• Contact: " + job.getVendor().getPhoneNumber() + "\n\n" +
                    "Your document will begin printing shortly. You'll receive another email when it's ready for pickup.\n\n" +
                    "Track your job: " + frontendBaseUrl + "/track?code=" + job.getTrackingCode() + "\n\n" +
                    "Thank you for using Spoolr!\n\n" +
                    "Spoolr Team");

            mailSender.send(message);
        }
    }

    /**
     * Send email notification when job starts printing
     */
    @Async
    public void sendJobPrintingEmail(PrintJob job, int estimatedMinutes) {
        // Only send email to registered customers (not anonymous)
        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            SimpleMailMessage message = new SimpleMailMessage();
            message.setTo(job.getCustomer().getEmail());
            message.setSubject("Spoolr - Your Document Is Being Printed! 🖨️");
            message.setText("Hello " + job.getCustomer().getName() + ",\n\n" +
                    "Your document is now being printed!\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Print Specs: " + job.getPrintSpecsSummary() + "\n\n" +
                    "⏰ Estimated Completion: " + estimatedMinutes + " minutes\n" +
                    "🏪 Pickup Location: " + job.getVendor().getBusinessName() + "\n" +
                    "📍 Address: " + job.getVendor().getBusinessAddress() + "\n\n" +
                    "You'll receive an email notification as soon as your document is ready for pickup.\n\n" +
                    "Track your job: " + frontendBaseUrl + "/track?code=" + job.getTrackingCode() + "\n\n" +
                    "Thank you for using Spoolr!\n\n" +
                    "Spoolr Team");

            mailSender.send(message);
        }
    }

    /**
     * Send email notification when job is ready for pickup - THE MOST IMPORTANT ONE!
     */
    @Async
    public void sendJobReadyForPickupEmail(PrintJob job) {
        // Send to both registered customers AND anonymous customers if we have their email
        String customerEmail = null;
        String customerName = "Customer";

        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            // Registered customer
            customerEmail = job.getCustomer().getEmail();
            customerName = job.getCustomer().getName();
        }
        // Note: For anonymous customers, we don't have email, so only WebSocket notification

        if (customerEmail != null) {
            SimpleMailMessage message = new SimpleMailMessage();
            message.setTo(customerEmail);
            message.setSubject("Spoolr - Your Print Job Is Ready for Pickup! ✅");
            message.setText("Hello " + customerName + ",\n\n" +
                    "🎉 GREAT NEWS! Your print job is ready for pickup!\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Print Specs: " + job.getPrintSpecsSummary() + "\n" +
                    "• Total Price: ₹" + job.getTotalPrice() + "\n\n" +
                    "📍 PICKUP LOCATION:\n" +
                    "• Business: " + job.getVendor().getBusinessName() + "\n" +
                    "• Address: " + job.getVendor().getBusinessAddress() + "\n" +
                    "• Contact: " + job.getVendor().getPhoneNumber() + "\n\n" +
                    "⏰ Please pickup during business hours.\n" +
                    "💳 Payment: " + (job.isAnonymous() ? "Pay at the store" : "Already processed online") + "\n\n" +
                    "Show this tracking code when you arrive: " + job.getTrackingCode() + "\n\n" +
                    "Track your job: " + frontendBaseUrl + "/track?code=" + job.getTrackingCode() + "\n\n" +
                    "Thank you for using Spoolr!\n\n" +
                    "Spoolr Team");

            mailSender.send(message);
        }
    }

    /**
     * Send email notification when job is completed
     */
    @Async
    public void sendJobCompletedEmail(PrintJob job) {
        // Only send email to registered customers (not anonymous)
        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            SimpleMailMessage message = new SimpleMailMessage();
            message.setTo(job.getCustomer().getEmail());
            message.setSubject("Spoolr - Print Job Completed! Thank You! 🙏");
            message.setText("Hello " + job.getCustomer().getName() + ",\n\n" +
                    "Your print job has been completed successfully!\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Print Specs: " + job.getPrintSpecsSummary() + "\n" +
                    "• Total Price: ₹" + job.getTotalPrice() + "\n\n" +
                    "Thank you for choosing Spoolr! We hope you're satisfied with our service.\n\n" +
                    "📝 We'd love your feedback! Please rate your experience:\n" +
                    frontendBaseUrl + "/feedback?job=" + job.getTrackingCode() + "\n\n" +
                    "Need another print job? Visit our website or find the nearest Spoolr partner.\n\n" +
                    "Best regards,\n" +
                    "Spoolr Team");

            mailSender.send(message);
        }
    }

    /**
     * Send email notification if job is rejected or cancelled
     */
    @Async
    public void sendJobIssueEmail(PrintJob job, String issue, String message) {
        // Only send email to registered customers (not anonymous)
        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            SimpleMailMessage emailMessage = new SimpleMailMessage();
            emailMessage.setTo(job.getCustomer().getEmail());
            emailMessage.setSubject("Spoolr - Update on Your Print Job 📋");
            emailMessage.setText("Hello " + job.getCustomer().getName() + ",\n\n" +
                    "We have an update regarding your print job:\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Status: " + issue + "\n\n" +
                    "ℹ️ Details: " + message + "\n\n" +
                    "We're working to resolve this and will update you shortly.\n" +
                    "If you have any questions, please contact us.\n\n" +
                    "Track your job: " + frontendBaseUrl + "/track?code=" + job.getTrackingCode() + "\n\n" +
                    "Thank you for your patience.\n\n" +
                    "Spoolr Team");

            mailSender.send(emailMessage);
        }
    }
    
    /**
     * Send email notification when vendor rejects the job
     */
    @Async
    public void sendJobRejectedEmail(PrintJob job) {
        // Only send email to registered customers (not anonymous)
        if (job.getCustomer() != null && job.getCustomer().getEmail() != null) {
            String vendorName = (job.getVendor() != null) ? job.getVendor().getBusinessName() : "the print shop";
            
            SimpleMailMessage message = new SimpleMailMessage();
            message.setTo(job.getCustomer().getEmail());
            message.setSubject("Spoolr - Searching for Another Vendor 🔍");
            message.setText("Hello " + job.getCustomer().getName() + ",\n\n" +
                    vendorName + " was unable to accept your job at this time.\n\n" +
                    "📄 Job Details:\n" +
                    "• File: " + job.getOriginalFileName() + "\n" +
                    "• Tracking Code: " + job.getTrackingCode() + "\n" +
                    "• Print Specs: " + job.getPrintSpecsSummary() + "\n" +
                    "• Vendor: " + vendorName + "\n\n" +
                    "🔍 What's happening now:\n" +
                    "We're automatically searching for another nearby print shop that can fulfill your order.\n" +
                    "You'll receive a notification once we find a new vendor.\n\n" +
                    "ℹ️ Your job is still active and we're working on it!\n" +
                    "No action is needed from your side.\n\n" +
                    "Track your job: " + frontendBaseUrl + "/track?code=" + job.getTrackingCode() + "\n\n" +
                    "Thank you for your patience.\n\n" +
                    "Spoolr Team");

            mailSender.send(message);
        }
    }

    /**
     * 🔧 FIXED: Send job ready notification with explicit parameters to avoid Hibernate issues
     * This method is designed to work with explicit data instead of Hibernate entities in scheduled tasks
     */
    @Async
    public void sendDirectJobReadyEmail(String customerEmail, String customerName,
                                        String trackingCode, String fileName,
                                        String printSpecs, String totalPrice,
                                        String vendorName, String vendorAddress) {
        if (customerEmail != null && !customerEmail.trim().isEmpty()) {
            try {
                SimpleMailMessage message = new SimpleMailMessage();
                message.setTo(customerEmail);
                message.setSubject("Spoolr- Your Print Job Is Ready for Pickup! ✅");
                message.setText("Hello " + (customerName != null ? customerName : "Customer") + ",\n\n" +
                        "🎉 GREAT NEWS! Your print job is ready for pickup!\n\n" +
                        "📄 Job Details:\n" +
                        "• File: " + fileName + "\n" +
                        "• Tracking Code: " + trackingCode + "\n" +
                        "• Print Specs: " + printSpecs + "\n" +
                        "• Total Price: ₹" + totalPrice + "\n\n" +
                        "📍 PICKUP LOCATION:\n" +
                        "• Business: " + vendorName + "\n" +
                        "• Address: " + vendorAddress + "\n\n" +
                        "⏰ Please pickup during business hours.\n" +
                        "💳 Payment: Already processed online\n\n" +
                        "Show this tracking code when you arrive: " + trackingCode + "\n\n" +
                        "Track your job: " + frontendBaseUrl + "/track?code=" + trackingCode + "\n\n" +
                        "Thank you for using Spoolr!\n\n" +
                        "Spoolr Team");

                mailSender.send(message);
                System.out.println("✅ Ready email sent successfully to: " + customerEmail);
            } catch (Exception e) {
                System.err.println("❌ Failed to send ready email to " + customerEmail + ": " + e.getMessage());
                e.printStackTrace();
            }
        }
    }
}
