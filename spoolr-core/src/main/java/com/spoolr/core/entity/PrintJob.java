package com.spoolr.core.entity;

import com.spoolr.core.enums.FileType;
import com.spoolr.core.enums.JobStatus;
import com.spoolr.core.enums.PaperSize;
import jakarta.persistence.*;
import lombok.Data;
import org.hibernate.annotations.CreationTimestamp;

import java.math.BigDecimal;
import java.time.LocalDateTime;

/**
 * PrintJob Entity - Represents a customer's print order
 * 
 * Think of this as a digital order ticket that contains:
 * - WHO ordered it (customer)
 * - WHERE it will be printed (vendor)  
 * - WHAT document to print (file information)
 * - HOW to print it (paper size, color, copies)
 * - HOW MUCH it costs (pricing)
 * - WHAT'S the status (tracking)
 */
@Entity
@Table(name = "print_jobs")
@Data  // Lombok: Automatically creates getters, setters, toString, etc.
public class PrintJob {
    
    // ===== CORE IDENTITY =====
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(unique = true, nullable = false)
    private String trackingCode;  // Like "PJ123456" - for customer tracking
    
    // ===== RELATIONSHIPS =====
    /**
     * Customer who placed the order
     * Can be null for anonymous QR code orders
     */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "customer_id")
    private User customer;  
    
    /**
     * Vendor (print shop) assigned to print this job
     */
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "vendor_id")
    private Vendor vendor;
    
    // ===== FILE INFORMATION (MinIO Cloud Storage) =====
    @Column(nullable = false)
    private String originalFileName;     // "resume.pdf" - what customer named it
    
    @Column(nullable = false)
    private String storedFileName;       // "job_123_resume.pdf" - unique storage name
    
    @Column(nullable = false)
    private String s3BucketName;         // "printwave-documents" - storage bucket
    
    @Column(nullable = false)
    private String s3ObjectKey;          // "2024/01/15/job_123_resume.pdf" - full path
    
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private FileType fileType;           // PDF, DOCX, JPG, PNG
    
    @Column(nullable = false)
    private Long fileSizeBytes;          // File size for storage tracking
    
    // ===== PRINT SPECIFICATIONS =====
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private PaperSize paperSize;         // A4, A3, LETTER, LEGAL
    
    @Column(nullable = false)
    private Boolean isColor;             // true = color, false = black & white
    
    @Column(nullable = false)
    private Boolean isDoubleSided;       // true = print both sides, false = single side
    
    @Column(nullable = false)
    private Integer copies;              // Number of copies to print
    
    // ===== PRICING (BigDecimal for precise money calculations) =====
    /**
     * Price per page based on vendor's pricing structure
     * Why BigDecimal? Because 0.1 + 0.2 = 0.30000000000000004 with double
     * But BigDecimal gives exact: 0.30
     */
    @Column(precision = 10, scale = 2, nullable = false)
    private BigDecimal pricePerPage;     // Like $0.25 per page
    
    @Column(precision = 10, scale = 2, nullable = false)
    private BigDecimal totalPrice;       // Final price customer pays
    
    @Column(nullable = false)
    private Integer totalPages;          // Number of pages in document
    
    // ===== STATUS TRACKING & TIMESTAMPS =====
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private JobStatus status;            // Current job status
    
    @CreationTimestamp
    @Column(nullable = false)
    private LocalDateTime createdAt;     // When job was uploaded
    
    private LocalDateTime matchedAt;     // When assigned to vendor
    
    private LocalDateTime acceptedAt;    // When vendor accepted
    
    private LocalDateTime printingAt;    // When printing started
    
    private LocalDateTime readyAt;       // When ready for pickup
    
    private LocalDateTime completedAt;   // When customer picked up
    
    // ===== HELPER METHODS FOR BUSINESS LOGIC =====
    
    /**
     * Check if this is an anonymous order (QR code)
     */
    public boolean isAnonymous() {
        return customer == null;
    }
    
    /**
     * Check if job is ready for customer pickup
     */
    public boolean isReadyForPickup() {
        return status == JobStatus.READY;
    }
    
    /**
     * Check if job is still active (not finished)
     */
    public boolean isActive() {
        return status.isActive();
    }
    
    /**
     * Check if customer can still cancel this job
     */
    public boolean canBeCancelled() {
        return status.canBeCancelled();
    }
    
    /**
     * Calculate total cost based on specifications
     */
    public BigDecimal calculateTotalCost() {
        if (pricePerPage == null || totalPages == null || copies == null) {
            return BigDecimal.ZERO;
        }
        
        return pricePerPage
                .multiply(new BigDecimal(totalPages))
                .multiply(new BigDecimal(copies));
    }
    
    /**
     * Get customer display name (for Station app)
     */
    public String getCustomerDisplayName() {
        if (isAnonymous()) {
            return "Anonymous Customer";
        }
        return customer.getName() != null ? customer.getName() : "Registered Customer";
    }
    
    /**
     * Get file display information
     */
    public String getFileDisplayInfo() {
        return String.format("%s (%d pages, %s)", 
                originalFileName, 
                totalPages, 
                fileType.getDisplayName());
    }
    
    /**
     * Get print specifications summary
     */
    public String getPrintSpecsSummary() {
        return String.format("%s, %s, %s, %d %s", 
                paperSize.getDisplayName(),
                isColor ? "Color" : "B&W",
                isDoubleSided ? "Double-sided" : "Single-sided",
                copies,
                copies == 1 ? "copy" : "copies");
    }
}
