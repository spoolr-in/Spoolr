package com.spoolr.core.enums;

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
    AWAITING_ACCEPTANCE("Offered to a vendor, awaiting their response"),
    ACCEPTED("Vendor has accepted the job and is preparing to print"),
    PRINTING("The document is currently being printed"),
    READY("Your document is printed and ready for pickup"),
    COMPLETED("The job has been successfully completed"),
    CANCELLED("The job was cancelled by the customer"),
    VENDOR_REJECTED("The vendor actively rejected the job offer"),
    VENDOR_TIMEOUT("The vendor did not respond to the job offer in time"),
    NO_VENDORS_AVAILABLE("No suitable vendors were found for this job");
    
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
        return this != COMPLETED && this != CANCELLED && this != VENDOR_REJECTED && this != NO_VENDORS_AVAILABLE;
    }
    
    public boolean canBeCancelled() {
        return this == UPLOADED || this == PROCESSING || this == AWAITING_ACCEPTANCE;
    }
    
    public boolean requiresCustomerAction() {
        return this == READY;
    }
}
