package com.printwave.core.enums;

/**
 * JobStatus Enum - Tracks the lifecycle of a print job
 * 
 * Think of this like package tracking:
 * - UPLOADED: Customer just uploaded the document
 * - PROCESSING: System is analyzing the document  
 * - MATCHED: System found a suitable vendor
 * - ACCEPTED: Vendor accepted the job in Station App
 * - PRINTING: Vendor is currently printing
 * - READY: Document is printed and ready for pickup
 * - COMPLETED: Customer picked up the document
 * - CANCELLED: Customer cancelled the job
 * - REJECTED: Vendor rejected the job
 */
public enum JobStatus {
    UPLOADED("Document uploaded, waiting for processing"),
    PROCESSING("Analyzing document and finding vendors"),
    MATCHED("Assigned to vendor, waiting for acceptance"),
    ACCEPTED("Vendor accepted, preparing to print"),
    PRINTING("Document is being printed"),
    READY("Document ready for customer pickup"),
    COMPLETED("Job completed successfully"),
    CANCELLED("Job cancelled by customer"),
    REJECTED("Job rejected by vendor");
    
    private final String description;
    
    // Constructor to store description
    JobStatus(String description) {
        this.description = description;
    }
    
    // Getter method to retrieve description
    public String getDescription() {
        return description;
    }
    
    // Helper methods for business logic
    public boolean isActive() {
        return this != COMPLETED && this != CANCELLED && this != REJECTED;
    }
    
    public boolean canBeCancelled() {
        return this == UPLOADED || this == PROCESSING || this == MATCHED;
    }
    
    public boolean requiresCustomerAction() {
        return this == READY;
    }
}
