package com.printwave.core.controller;

import com.printwave.core.entity.PrintJob;
import com.printwave.core.entity.User;
import com.printwave.core.entity.Vendor;
import com.printwave.core.enums.PaperSize;
import com.printwave.core.repository.UserRepository;
import com.printwave.core.repository.VendorRepository;
import com.printwave.core.service.PrintJobService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import jakarta.servlet.http.HttpServletRequest;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Collections;

/**
 * PrintJobController - Complete "Uber for Printing" REST APIs
 * 
 * 🎯 COMPLETE API ENDPOINTS:
 * 
 * 📋 AUTHENTICATED CUSTOMERS (JWT Required):
 * - POST /api/jobs/upload              - Online upload (from anywhere)
 * - GET  /api/jobs/history             - Order history
 * 
 * 🔓 ANONYMOUS CUSTOMERS (QR Code - No Auth):
 * - GET  /store/{storeCode}            - QR landing page
 * - POST /api/jobs/qr-anonymous-upload - Anonymous upload (QR only)
 * - GET  /api/jobs/status/{trackingCode} - Public job tracking
 * 
 * 🏪 VENDOR APIS (Station App):
 * - GET  /api/jobs/queue               - Vendor job queue
 * - POST /api/jobs/{jobId}/accept      - Accept job
 * - POST /api/jobs/{jobId}/print       - Start printing
 * - POST /api/jobs/{jobId}/ready       - Mark ready
 * - POST /api/jobs/{jobId}/complete    - Complete job
 * - GET  /api/jobs/{jobId}/file        - Get streaming URL
 * 
 * 🔄 COMPLETE WORKFLOWS SUPPORTED:
 * 
 * 1️⃣ REGISTERED CUSTOMER (Online Portal):
 *    Customer logs in → uploads from home → pays online → picks up
 * 
 * 2️⃣ ANONYMOUS CUSTOMER (QR Code):
 *    Customer at store → scans QR → uploads → waits → pays at store
 * 
 * 3️⃣ VENDOR (Station App):
 *    Sees jobs → accepts → prints → marks ready → completes
 */
@RestController
@RequestMapping("/api")
public class PrintJobController {

    private final PrintJobService printJobService;
    private final UserRepository userRepository;
    private final VendorRepository vendorRepository;

    @Autowired
    public PrintJobController(PrintJobService printJobService,
                             UserRepository userRepository,
                             VendorRepository vendorRepository) {
        this.printJobService = printJobService;
        this.userRepository = userRepository;
        this.vendorRepository = vendorRepository;
    }

    // ===== AUTHENTICATED CUSTOMER APIs (JWT Required) =====

    /**
     * 🔐 AUTHENTICATED UPLOAD: Registered customers (online portal)
     * 
     * For customers who:
     * - Have created accounts and logged in
     * - Want to upload from home/office
     * - Will pay online before pickup
     * 
     * POST /api/jobs/upload
     * Authorization: Bearer {jwt_token}
     * Content-Type: multipart/form-data
     */
    @PostMapping("/jobs/upload")
    @PreAuthorize("hasRole('CUSTOMER')")
    public ResponseEntity<?> uploadFileAuthenticated(
            @RequestParam("file") MultipartFile file,
            @RequestParam("paperSize") String paperSizeStr,
            @RequestParam("isColor") boolean isColor,
            @RequestParam("isDoubleSided") boolean isDoubleSided,
            @RequestParam("copies") int copies,
            @RequestParam("customerLatitude") double customerLatitude,
            @RequestParam("customerLongitude") double customerLongitude,
            @RequestParam(value = "vendorId", required = false) Long vendorId, // New optional parameter
            HttpServletRequest request) {
        
        try {
            // Get authenticated customer from JWT
            User customer = getAuthenticatedUser(request);
            
            // Create print job (same logic as anonymous, but with customer)
            PrintJob printJob = createPrintJob(
                file, customer, paperSizeStr, isColor, isDoubleSided, copies,
                customerLatitude, customerLongitude, vendorId
            );
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Print job created successfully!",
                "trackingCode", printJob.getTrackingCode(),
                "jobId", printJob.getId(),
                "status", printJob.getStatus().name(),
                "totalPages", printJob.getTotalPages(),
                "totalPrice", printJob.getTotalPrice(),
                "paymentRequired", true, // Online payment required for registered users
                "vendor", printJob.getVendor() != null ? 
                    Map.of(
                        "businessName", printJob.getVendor().getBusinessName(),
                        "address", printJob.getVendor().getBusinessAddress()
                    ) : null,
                "estimatedReady", "5-10 minutes after payment and vendor acceptance"
            ));
            
        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", "Invalid input: " + e.getMessage()
            ));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to create print job: " + e.getMessage()
            ));
        }
    }

    /**
     * 📋 Get customer's order history (authenticated)
     */
    @GetMapping("/jobs/history")
    @PreAuthorize("hasRole('CUSTOMER')")
    public ResponseEntity<?> getJobHistory(HttpServletRequest request) {
        try {
            User customer = getAuthenticatedUser(request);
            List<PrintJob> jobHistory = printJobService.getCustomerJobHistory(customer);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "jobs", jobHistory.stream().map(this::createJobSummary).toList(),
                "totalJobs", jobHistory.size()
            ));
            
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to get job history: " + e.getMessage()
            ));
        }
    }

    // ===== ANONYMOUS QR CODE APIs (No Authentication) =====

    /**
     * 🔓 QR CODE LANDING PAGE
     * 
     * This is where customers land when they scan a QR code at a print shop.
     * Returns store information and allows anonymous upload.
     * 
     * GET /store/{storeCode}
     * No authentication required
     */
    @GetMapping("/store/{storeCode}")
    public ResponseEntity<?> getQrLandingPage(@PathVariable String storeCode) {
        try {
            // Find vendor by store code
            Optional<Vendor> vendorOpt = vendorRepository.findByStoreCode(storeCode);
            
            if (vendorOpt.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            
            Vendor vendor = vendorOpt.get();
            
            // Check if store is open
            boolean isStoreOpen = vendor.getIsStoreOpen() != null && vendor.getIsStoreOpen();
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("storeCode", storeCode);
            response.put("businessName", vendor.getBusinessName());
            response.put("address", vendor.getBusinessAddress());
            response.put("contactPerson", vendor.getContactPersonName());
            response.put("phone", vendor.getPhoneNumber());
            response.put("isOpen", isStoreOpen);
            response.put("pricing", Map.of(
                "bwSingleSided", vendor.getPricePerPageBWSingleSided(),
                "bwDoubleSided", vendor.getPricePerPageBWDoubleSided(),
                "colorSingleSided", vendor.getPricePerPageColorSingleSided(),
                "colorDoubleSided", vendor.getPricePerPageColorDoubleSided()
            ));
            response.put("message", isStoreOpen ? 
                "Welcome! You can upload your documents for printing." :
                "Store is currently closed. Please try again during business hours.");
            response.put("uploadEndpoint", "/api/jobs/qr-anonymous-upload");
            response.put("allowAnonymous", isStoreOpen);
            
            return ResponseEntity.ok(response);
            
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to load store information: " + e.getMessage()
            ));
        }
    }

    /**
     * 🔓 ANONYMOUS UPLOAD: QR code customers (no authentication)
     * 
     * For customers who:
     * - Are physically present at the print shop
     * - Scanned the QR code at the store
     * - Want to upload without creating an account
     * - Will pay at the store after printing
     * 
     * POST /api/jobs/qr-anonymous-upload
     * No authentication required
     * Content-Type: multipart/form-data
     */
    @PostMapping("/jobs/qr-anonymous-upload")
    public ResponseEntity<?> uploadFileAnonymous(
            @RequestParam("file") MultipartFile file,
            @RequestParam("storeCode") String storeCode,
            @RequestParam("paperSize") String paperSizeStr,
            @RequestParam("isColor") boolean isColor,
            @RequestParam("isDoubleSided") boolean isDoubleSided,
            @RequestParam("copies") int copies) {
        
        try {
            // Find and validate vendor by store code
            Optional<Vendor> vendorOpt = vendorRepository.findByStoreCode(storeCode);
            if (vendorOpt.isEmpty()) {
                return ResponseEntity.badRequest().body(Map.of(
                    "success", false,
                    "error", "Invalid store code: " + storeCode
                ));
            }
            
            Vendor vendor = vendorOpt.get();
            
            // Check if store is open (security: only allow uploads when physically present)
            if (vendor.getIsStoreOpen() == null || !vendor.getIsStoreOpen()) {
                return ResponseEntity.badRequest().body(Map.of(
                    "success", false,
                    "error", "Store is currently closed. Anonymous uploads only allowed when store is open."
                ));
            }
            
            // Create anonymous print job (customer = null, use vendor location)
            PrintJob printJob = createPrintJob(
                file, null, paperSizeStr, isColor, isDoubleSided, copies,
                vendor.getLatitude(), vendor.getLongitude(), null // No manual vendor selection for QR
            );
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Print job created successfully! Please wait while we print your document.");
            response.put("jobId", printJob.getId()); // Add jobId for frontend operations
            response.put("trackingCode", printJob.getTrackingCode());
            response.put("status", printJob.getStatus().name());
            response.put("totalPages", printJob.getTotalPages());
            response.put("totalPrice", printJob.getTotalPrice());
            response.put("paymentRequired", false);
            response.put("store", Map.of(
                "businessName", vendor.getBusinessName(),
                "address", vendor.getBusinessAddress(),
                "contactPerson", vendor.getContactPersonName()
            ));
            response.put("instructions", "Your document will be ready in 5-10 minutes. Please show this tracking code to the store staff: " + printJob.getTrackingCode());
            response.put("estimatedReady", "5-10 minutes");
            response.put("isAnonymous", true);
            
            return ResponseEntity.ok(response);
            
        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", "Invalid input: " + e.getMessage()
            ));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to create print job: " + e.getMessage()
            ));
        }
    }

    /**
     * 💰 Get a quote for a print job from multiple vendors.
     * This allows the user to see a list of vendors and prices before committing.
     */
    @PostMapping("/jobs/quote")
    public ResponseEntity<?> getJobQuote(
            @RequestParam("file") MultipartFile file,
            @RequestParam("paperSize") String paperSizeStr,
            @RequestParam("isColor") boolean isColor,
            @RequestParam("isDoubleSided") boolean isDoubleSided,
            @RequestParam("copies") int copies,
            @RequestParam("customerLatitude") double customerLatitude,
            @RequestParam("customerLongitude") double customerLongitude) {
        try {
            List<Map<String, Object>> quotes = printJobService.getQuoteForJob(
                file, paperSizeStr, isColor, isDoubleSided, copies, customerLatitude, customerLongitude
            );

            if (quotes.isEmpty()) {
                return ResponseEntity.ok(Map.of(
                    "success", true,
                    "message", "No vendors are currently available for this job.",
                    "vendors", Collections.emptyList()
                ));
            }

            return ResponseEntity.ok(Map.of(
                "success", true,
                "vendors", quotes
            ));

        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().body(Map.of("success", false, "error", e.getMessage()));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(Map.of("success", false, "error", "An unexpected error occurred while generating quotes."));
        }
    }

    /**
     * 📍 PUBLIC JOB TRACKING (works for both registered and anonymous)
     * 
     * GET /api/jobs/status/{trackingCode}
     * No authentication required
     */
    @GetMapping("/jobs/status/{trackingCode}")
    public ResponseEntity<?> trackJobStatus(@PathVariable String trackingCode) {
        try {
            Optional<PrintJob> jobOpt = printJobService.getJobByTrackingCode(trackingCode);
            
            if (jobOpt.isEmpty()) {
                return ResponseEntity.badRequest().body(Map.of(
                    "success", false,
                    "error", "Tracking code not found: " + trackingCode
                ));
            }
            
            PrintJob job = jobOpt.get();
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("trackingCode", trackingCode);
            response.put("status", job.getStatus().name());
            response.put("statusDescription", job.getStatus().getDescription());
            response.put("fileName", job.getOriginalFileName());
            response.put("totalPages", job.getTotalPages());
            response.put("copies", job.getCopies());
            response.put("totalPrice", job.getTotalPrice());
            response.put("printSpecs", job.getPrintSpecsSummary());
            response.put("vendor", job.getVendor() != null ? 
                Map.of(
                    "businessName", job.getVendor().getBusinessName(),
                    "address", job.getVendor().getBusinessAddress()
                ) : null);
            response.put("createdAt", job.getCreatedAt());
            response.put("estimatedCompletion", getEstimatedCompletion(job));
            response.put("isAnonymous", job.isAnonymous());
            response.put("paymentInfo", job.isAnonymous() ? 
                "Pay at store when you collect your documents" :
                "Payment processed online");
            
            return ResponseEntity.ok(response);
            
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to track job: " + e.getMessage()
            ));
        }
    }

    // ===== VENDOR APIs (Station App - JWT Required) =====

    /**
     * 📱 Get vendor's job queue (Station App)
     */
    @GetMapping("/jobs/queue")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> getVendorJobQueue(HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            List<PrintJob> jobQueue = printJobService.getVendorJobQueue(vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "jobs", jobQueue.stream().map(this::createVendorJobDetails).toList(),
                "queueSize", jobQueue.size(),
                "storeStatus", vendor.getIsStoreOpen() ? "OPEN" : "CLOSED",
                "storeCode", vendor.getStoreCode()
            ));
            
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(Map.of(
                "success", false,
                "error", "Failed to get job queue: " + e.getMessage()
            ));
        }
    }

    /**
     * ✅ Accept a print job (Station App)
     */
    @PostMapping("/jobs/{jobId}/accept")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> acceptJob(@PathVariable Long jobId, 
                                     HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            PrintJob updatedJob = printJobService.acceptJob(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Job accepted successfully!",
                "jobId", updatedJob.getId(),
                "status", updatedJob.getStatus().name(),
                "trackingCode", updatedJob.getTrackingCode()
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    /**
     * ❌ Reject a print job (Station App)
     */
    @PostMapping("/jobs/{jobId}/reject")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> rejectJob(@PathVariable Long jobId, 
                                     HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            PrintJob updatedJob = printJobService.rejectJob(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Job rejected. We will offer it to another vendor.",
                "jobId", updatedJob.getId(),
                "status", updatedJob.getStatus().name()
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    /**
     * 🖨️ Mark job as printing (Station App)
     */
    @PostMapping("/jobs/{jobId}/print")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> startPrinting(@PathVariable Long jobId,
                                         HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            PrintJob updatedJob = printJobService.startPrinting(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Job printing started!",
                "jobId", updatedJob.getId(),
                "status", updatedJob.getStatus().name(),
                "printingStarted", updatedJob.getPrintingAt()
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    /**
     * 📄 Mark job as ready for pickup (Station App)
     */
    @PostMapping("/jobs/{jobId}/ready")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> markJobReady(@PathVariable Long jobId,
                                        HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            PrintJob updatedJob = printJobService.markJobReady(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Job ready for pickup!",
                "jobId", updatedJob.getId(),
                "status", updatedJob.getStatus().name(),
                "readyAt", updatedJob.getReadyAt(),
                "customerMessage", "Print job " + updatedJob.getTrackingCode() + " is ready for pickup!"
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    /**
     * ✅ Complete job (Customer picked up)
     */
    @PostMapping("/jobs/{jobId}/complete")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> completeJob(@PathVariable Long jobId,
                                       HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            PrintJob updatedJob = printJobService.completeJob(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "message", "Job completed successfully!",
                "jobId", updatedJob.getId(),
                "status", updatedJob.getStatus().name(),
                "completedAt", updatedJob.getCompletedAt(),
                "earnings", updatedJob.getTotalPrice()
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    /**
     * 🌐 Get file streaming URL for printing (Station App)
     */
    @GetMapping("/jobs/{jobId}/file")
    @PreAuthorize("hasRole('VENDOR')")
    public ResponseEntity<?> getJobFileUrl(@PathVariable Long jobId,
                                         HttpServletRequest request) {
        try {
            Vendor vendor = getAuthenticatedVendor(request);
            String streamingUrl = printJobService.getJobFileStreamingUrl(jobId, vendor);
            
            return ResponseEntity.ok(Map.of(
                "success", true,
                "streamingUrl", streamingUrl,
                "expiryMinutes", 30,
                "instructions", "Use this URL to stream the file directly to your printer. URL expires in 30 minutes.",
                "jobId", jobId
            ));
            
        } catch (RuntimeException e) {
            return ResponseEntity.badRequest().body(Map.of(
                "success", false,
                "error", e.getMessage()
            ));
        }
    }

    // ===== HELPER METHODS =====

    /**
     * Create print job (shared logic for authenticated and anonymous)
     */
    private PrintJob createPrintJob(MultipartFile file, User customer, String paperSizeStr,
                                   boolean isColor, boolean isDoubleSided, int copies,
                                   double latitude, double longitude, Long vendorId) {
        
        // Parse paper size
        PaperSize paperSize = PaperSize.fromString(paperSizeStr);
        
        // Create print job using the service
        return printJobService.createPrintJob(
            file, customer, paperSize, isColor, isDoubleSided, copies,
            latitude, longitude, vendorId
        );
    }

    /**
     * Get authenticated customer from JWT token
     */
    private User getAuthenticatedUser(HttpServletRequest request) {
        Long userId = (Long) request.getAttribute("userId");
        return userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("User not found"));
    }

    /**
     * Get authenticated vendor from JWT token
     */
    private Vendor getAuthenticatedVendor(HttpServletRequest request) {
        Long vendorId = (Long) request.getAttribute("userId"); // JWT filter sets userId for both customers and vendors
        return vendorRepository.findById(vendorId)
                .orElseThrow(() -> new RuntimeException("Vendor not found"));
    }

    /**
     * Create job summary for customer history
     */
    private Map<String, Object> createJobSummary(PrintJob job) {
        Map<String, Object> summary = new HashMap<>();
        summary.put("jobId", job.getId());
        summary.put("trackingCode", job.getTrackingCode());
        summary.put("fileName", job.getOriginalFileName());
        summary.put("status", job.getStatus().name());
        summary.put("statusDescription", job.getStatus().getDescription());
        summary.put("totalPrice", job.getTotalPrice());
        summary.put("totalPages", job.getTotalPages());
        summary.put("copies", job.getCopies());
        summary.put("printSpecs", job.getPrintSpecsSummary());
        summary.put("vendor", job.getVendor() != null ? job.getVendor().getBusinessName() : "Not assigned");
        summary.put("createdAt", job.getCreatedAt());
        summary.put("canCancel", job.canBeCancelled());
        return summary;
    }

    /**
     * Create detailed job info for vendor queue
     */
    private Map<String, Object> createVendorJobDetails(PrintJob job) {
        Map<String, Object> details = new HashMap<>();
        details.put("jobId", job.getId());
        details.put("trackingCode", job.getTrackingCode());
        details.put("fileName", job.getOriginalFileName());
        details.put("status", job.getStatus().name());
        details.put("customer", job.getCustomerDisplayName());
        details.put("fileInfo", job.getFileDisplayInfo());
        details.put("printSpecs", job.getPrintSpecsSummary());
        details.put("totalPrice", job.getTotalPrice());
        details.put("earnings", job.getTotalPrice()); // TODO: Subtract platform fee
        details.put("createdAt", job.getCreatedAt());
        details.put("isAnonymous", job.isAnonymous());
        details.put("requiresAction", job.getStatus().name().equals("AWAITING_ACCEPTANCE"));
        details.put("paymentType", job.isAnonymous() ? "Pay at store" : "Paid online");
        return details;
    }

    /**
     * Calculate estimated completion time based on job status
     */
    private String getEstimatedCompletion(PrintJob job) {
        return switch (job.getStatus()) {
            case UPLOADED, PROCESSING -> "Finding nearby vendor...";
            case AWAITING_ACCEPTANCE -> "Waiting for vendor to accept (under 90 seconds)";
            case ACCEPTED -> "Vendor preparing to print (2-5 minutes)";
            case PRINTING -> "Currently printing (2-5 minutes)";
            case READY -> "Ready for pickup at store!";
            case COMPLETED -> "Completed";
            case CANCELLED -> "Cancelled by user";
            case VENDOR_REJECTED -> "Vendor rejected the offer, searching for a new one...";
            case VENDOR_TIMEOUT -> "Vendor did not respond, searching for a new one...";
            case NO_VENDORS_AVAILABLE -> "Could not find any available vendors for your job.";
        };
    }
}
