package com.spoolr.core.controller;

import com.spoolr.core.entity.Vendor;
import com.spoolr.core.repository.VendorRepository;
import com.spoolr.core.service.PrintJobService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.messaging.simp.SimpMessageHeaderAccessor;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Controller;

import java.util.Map;
import java.util.Optional;

/**
 * WebSocketController - Handles STOMP message mappings from WebSocket clients.
 * 
 * This controller processes messages sent by vendors (Station App) via WebSocket
 * to destinations starting with "/app" prefix (configured in WebSocketConfig).
 */
@Controller
public class WebSocketController {

    @Autowired
    private SimpMessagingTemplate messagingTemplate;
    
    @Autowired
    private VendorRepository vendorRepository;
    
    @Autowired
    private PrintJobService printJobService;

    /**
     * Handle vendor status updates from Station App
     * 
     * Station App sends: SEND destination:/app/vendor-status
     * This method receives the message and updates vendor availability
     */
    @MessageMapping("/vendor-status")
    public void handleVendorStatus(@Payload Map<String, Object> statusMessage, 
                                  SimpMessageHeaderAccessor headerAccessor) {
        try {
            // Extract vendor information from message
            Long vendorId = Long.valueOf(statusMessage.get("vendorId").toString());
            Boolean isAvailable = (Boolean) statusMessage.get("isAvailable");
            String businessName = (String) statusMessage.get("businessName");
            
            System.out.println("Received vendor status update: VendorId=" + vendorId + 
                             ", Available=" + isAvailable + ", Business=" + businessName);
            
            // Update vendor availability in database (if needed)
            Optional<Vendor> vendorOpt = vendorRepository.findById(vendorId);
            if (vendorOpt.isPresent()) {
                Vendor vendor = vendorOpt.get();
                // Update station app connection status
                vendor.setStationAppConnected(true);
                // Update store open/closed status based on vendor availability toggle
                vendor.updateStoreStatus(isAvailable);
                vendorRepository.save(vendor);
                
                System.out.println("Updated vendor " + vendorId + " status: connected=true, isStoreOpen=" + isAvailable);
            }
            
            // Store session information for this vendor
            headerAccessor.getSessionAttributes().put("vendorId", vendorId);
            headerAccessor.getSessionAttributes().put("businessName", businessName);
            
        } catch (Exception e) {
            System.err.println("Error processing vendor status: " + e.getMessage());
            e.printStackTrace();
        }
    }

    /**
     * Handle job offer responses from Station App
     * 
     * Station App sends: SEND destination:/app/job-response
     * This method processes accept/decline responses for job offers
     */
    @MessageMapping("/job-response")
    public void handleJobResponse(@Payload Map<String, Object> responseMessage, 
                                 SimpMessageHeaderAccessor headerAccessor) {
        try {
            // Extract response information
            Long jobId = Long.valueOf(responseMessage.get("jobId").toString());
            String response = (String) responseMessage.get("response"); // "accept" or "decline"
            Long vendorId = Long.valueOf(responseMessage.get("vendorId").toString());
            
            System.out.println("Received job response: JobId=" + jobId + 
                             ", Response=" + response + ", VendorId=" + vendorId);
            
            // Process the job response through PrintJobService
            if ("accept".equalsIgnoreCase(response)) {
                // Accept the job offer
                try {
                    // Get the vendor from repository to pass to acceptJob method
                    Optional<Vendor> vendorOpt = vendorRepository.findById(vendorId);
                    if (vendorOpt.isPresent()) {
                        printJobService.acceptJob(jobId, vendorOpt.get());
                        System.out.println("Job " + jobId + " accepted by vendor " + vendorId);
                        
                        // Send success confirmation back to vendor
                        messagingTemplate.convertAndSend("/queue/job-offers-" + vendorId, Map.of(
                            "type", "JOB_ACCEPTED",
                            "jobId", jobId,
                            "message", "Job accepted successfully!"
                        ));
                    } else {
                        throw new Exception("Vendor not found: " + vendorId);
                    }
                } catch (Exception e) {
                    System.err.println("Failed to accept job " + jobId + ": " + e.getMessage());
                    
                    // Send error response back to vendor
                    messagingTemplate.convertAndSend("/queue/job-offers-" + vendorId, Map.of(
                        "type", "JOB_RESPONSE_ERROR",
                        "jobId", jobId,
                        "error", "Failed to accept job: " + e.getMessage()
                    ));
                }
            } else if ("decline".equalsIgnoreCase(response)) {
                // Decline the job offer
                try {
                    // Get the vendor from repository to pass to rejectJob method
                    Optional<Vendor> vendorOpt = vendorRepository.findById(vendorId);
                    if (vendorOpt.isPresent()) {
                        printJobService.rejectJob(jobId, vendorOpt.get());
                        System.out.println("Job " + jobId + " declined by vendor " + vendorId);
                        
                        // Send confirmation back to vendor
                        messagingTemplate.convertAndSend("/queue/job-offers-" + vendorId, Map.of(
                            "type", "JOB_DECLINED",
                            "jobId", jobId,
                            "message", "Job declined. Offering to next vendor."
                        ));
                    } else {
                        throw new Exception("Vendor not found: " + vendorId);
                    }
                } catch (Exception e) {
                    System.err.println("Failed to decline job " + jobId + ": " + e.getMessage());
                }
            }
            
        } catch (Exception e) {
            System.err.println("Error processing job response: " + e.getMessage());
            e.printStackTrace();
        }
    }

    /**
     * Handle ping messages for connection health checks
     */
    @MessageMapping("/ping")
    public void handlePing(@Payload Map<String, Object> pingMessage, 
                          SimpMessageHeaderAccessor headerAccessor) {
        try {
            Long vendorId = (Long) headerAccessor.getSessionAttributes().get("vendorId");
            if (vendorId != null) {
                // Send pong response
                messagingTemplate.convertAndSend("/queue/job-offers-" + vendorId, Map.of(
                    "type", "PONG",
                    "timestamp", System.currentTimeMillis()
                ));
            }
        } catch (Exception e) {
            System.err.println("Error processing ping: " + e.getMessage());
        }
    }
}