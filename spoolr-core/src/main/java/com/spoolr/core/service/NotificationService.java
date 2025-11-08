package com.spoolr.core.service;

import com.spoolr.core.entity.PrintJob;
import com.spoolr.core.service.EmailService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.Map;

/**
 * NotificationService - Handles sending real-time WebSocket messages to clients.
 *
 * This service acts as a centralized hub for all WebSocket-based notifications,
 * ensuring a clean separation of concerns. It is used by other services (like PrintJobService)
 * to push updates to the correct users or vendors.
 */
@Service
public class NotificationService {

    // SimpMessagingTemplate is the core Spring class for sending WebSocket messages.
    // It's like the postman that knows how to deliver messages to the right addresses.
    private final SimpMessagingTemplate messagingTemplate;
    
    // EmailService for sending email notifications alongside WebSocket notifications
    private final EmailService emailService;

    @Autowired
    public NotificationService(SimpMessagingTemplate messagingTemplate, EmailService emailService) {
        this.messagingTemplate = messagingTemplate;
        this.emailService = emailService;
    }

    /**
     * Sends a new job offer to a specific vendor.
     *
     * This method sends a message to a private, user-specific queue.
     * The destination is "/queue/job-offers/{vendorId}". The Station App for that vendor
     * will need to be subscribed to this specific destination to receive the offer.
     *
     * @param vendorId The ID of the vendor to send the offer to.
     * @param job      The PrintJob being offered.
     */
    public void notifyVendorOfNewJob(Long vendorId, PrintJob job) {
        // The destination for the message. Using "/queue" ensures it's a private message
        // sent only to the user session associated with the vendor.
        String destination = "/queue/job-offers-" + vendorId;

        // The message payload. We send a simple map of key-value pairs.
        // The Station App will parse this JSON to display the job details.
        Map<String, Object> payload = new HashMap<>();
        payload.put("type", "NEW_JOB_OFFER");
        payload.put("jobId", job.getId());
        payload.put("trackingCode", job.getTrackingCode());
        payload.put("fileName", job.getOriginalFileName());
        payload.put("customer", job.getCustomerDisplayName());
        payload.put("printSpecs", job.getPrintSpecsSummary());
        payload.put("totalPrice", job.getTotalPrice());
        payload.put("earnings", job.getTotalPrice()); // TODO: Subtract platform fee later
        payload.put("createdAt", job.getCreatedAt());
        payload.put("isAnonymous", job.isAnonymous());
        payload.put("offerExpiresInSeconds", 90); // Let the frontend know about the timeout

        // Send the message.
        messagingTemplate.convertAndSend(destination, payload);
        System.out.println("Sent job offer for job " + job.getId() + " to vendor " + vendorId);
    }

    /**
     * Notifies a vendor that a job offer has been rescinded (e.g., because another vendor accepted it).
     *
     * @param vendorId The ID of the vendor to notify.
     * @param jobId    The ID of the job that is no longer available.
     */
    public void notifyVendorOfOfferCancellation(Long vendorId, Long jobId) {
        String destination = "/queue/job-offers-" + vendorId;
        Map<String, Object> payload = Map.of(
                "type", "OFFER_CANCELLED",
                "jobId", jobId,
                "message", "This job offer has been accepted by another vendor or cancelled."
        );
        messagingTemplate.convertAndSend(destination, payload);
    }

    /**
     * Sends a general status update to the customer for a specific job.
     *
     * @param trackingCode The job's tracking code.
     * @param status       The new status of the job.
     * @param message      A descriptive message for the customer.
     */
    public void notifyCustomerOfStatusUpdate(String trackingCode, String status, String message) {
        // The destination for this message is a public topic.
        // Anyone (including the customer) who is subscribed to this tracking code's topic will get the update.
        String destination = "/topic/job-status/" + trackingCode;
        Map<String, Object> payload = Map.of(
                "type", "STATUS_UPDATE",
                "trackingCode", trackingCode,
                "status", status,
                "message", message
        );
        messagingTemplate.convertAndSend(destination, payload);
    }
    
    // ===== ENHANCED NOTIFICATION METHODS (WebSocket + Email) =====
    
    /**
     * 🎉 Notify customer when job is accepted - both WebSocket and Email
     */
    public void notifyCustomerJobAccepted(PrintJob job) {
        String message = "A vendor has accepted your job and will begin printing shortly.";
        
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), "ACCEPTED", message);
        
        // Send Email notification (persistent, for registered customers only)
        emailService.sendJobAcceptedEmail(job);
    }
    
    /**
     * 🖨️ Notify customer when job starts printing - both WebSocket and Email
     */
    public void notifyCustomerJobPrinting(PrintJob job, int estimatedMinutes) {
        String message = "Your document is now being printed. Estimated completion: " + estimatedMinutes + " minutes.";
        
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), "PRINTING", message);
        
        // 🔧 FIXED: Force loading of customer data before async call
        // This prevents Hibernate LazyInitializationException in async context
        if (job.getCustomer() != null) {
            // Force loading of customer data by accessing properties
            String customerEmail = job.getCustomer().getEmail();
            String customerName = job.getCustomer().getName();
            // Data is now loaded and will be available in async context
        }
        
        // Send Email notification (for registered customers only)
        emailService.sendJobPrintingEmail(job, estimatedMinutes);
    }
    
    /**
     * ✅ Notify customer when job is ready for pickup - both WebSocket and Email
     * THIS IS THE MOST IMPORTANT NOTIFICATION!
     */
    public void notifyCustomerJobReadyForPickup(PrintJob job) {
        String message = "Great news! Your print job is ready for pickup at " + job.getVendor().getBusinessName() + "!";
        
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), "READY", message);
        
        // 📧 Send Email notification - THE KEY ENHANCEMENT!
        // This ensures customers get notified even if they're not actively using the app
        emailService.sendJobReadyForPickupEmail(job);
    }
    
    /**
     * ✅ Notify customer when job is completed - both WebSocket and Email
     */
    public void notifyCustomerJobCompleted(PrintJob job) {
        String message = "Your print job has been completed successfully!";
        
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), "COMPLETED", message);
        
        // Send Email notification (for registered customers only)
        emailService.sendJobCompletedEmail(job);
    }
    
    /**
     * ⚠️ Notify customer of job issues - both WebSocket and Email
     */
    public void notifyCustomerJobIssue(PrintJob job, String status, String message) {
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), status, message);
        
        // Send Email notification (for registered customers only)
        emailService.sendJobIssueEmail(job, status, message);
    }
    
    /**
     * ❌ Notify customer when vendor rejects the job - both WebSocket and Email
     */
    public void notifyCustomerJobRejected(PrintJob job) {
        String message = "The vendor declined your job. We're searching for another vendor nearby...";
        
        // Send WebSocket notification (real-time)
        notifyCustomerOfStatusUpdate(job.getTrackingCode(), "VENDOR_REJECTED", message);
        
        // Send Email notification (for registered customers only)
        emailService.sendJobRejectedEmail(job);
    }
}
