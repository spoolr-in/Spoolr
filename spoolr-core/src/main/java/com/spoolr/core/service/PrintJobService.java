package com.spoolr.core.service;

import com.spoolr.core.entity.PrintJob;
import com.spoolr.core.entity.User;
import com.spoolr.core.entity.Vendor;
import com.spoolr.core.enums.FileType;
import com.spoolr.core.enums.JobStatus;
import com.spoolr.core.enums.PaperSize;
import com.spoolr.core.repository.PrintJobRepository;
import com.spoolr.core.repository.UserRepository;
import com.spoolr.core.repository.VendorRepository;

import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.poi.xwpf.usermodel.XWPFDocument;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.scheduling.TaskScheduler;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Instant;
import java.time.LocalDateTime;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ScheduledFuture;
import java.util.stream.Collectors;

import org.springframework.scheduling.annotation.Async;
import org.springframework.scheduling.annotation.Scheduled;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@Service
@Transactional
public class PrintJobService {

    private final PrintJobRepository printJobRepository;
    private final UserRepository userRepository;
    private final VendorRepository vendorRepository;
    private final FileStorageService fileStorageService;
    private final NotificationService notificationService;
    private final EmailService emailService;
    private final TaskScheduler taskScheduler;
    private final Random random = new Random();
    private static final Logger log = LoggerFactory.getLogger(PrintJobService.class);

    // A thread-safe map to keep track of the scheduled timeout tasks for each job.
    // This allows us to cancel the timeout task if a vendor accepts or rejects the job.
    private final Map<Long, ScheduledFuture<?>> scheduledTasks = new ConcurrentHashMap<>();

    @Autowired
    public PrintJobService(PrintJobRepository printJobRepository,
                          UserRepository userRepository,
                          VendorRepository vendorRepository,
                          FileStorageService fileStorageService,
                          NotificationService notificationService,
                          EmailService emailService,
                          TaskScheduler taskScheduler) {
        this.printJobRepository = printJobRepository;
        this.userRepository = userRepository;
        this.vendorRepository = vendorRepository;
        this.fileStorageService = fileStorageService;
        this.notificationService = notificationService;
        this.emailService = emailService;
        this.taskScheduler = taskScheduler;
    }

    public PrintJob createPrintJob(MultipartFile file,
                                  User customer,
                                  PaperSize paperSize,
                                  boolean isColor,
                                  boolean isDoubleSided,
                                  int copies,
                                  Double customerLatitude,
                                  Double customerLongitude,
                                  Long manualVendorId) {

        validateJobRequest(file, paperSize, copies, customerLatitude, customerLongitude);

        // Generate a unique identifier for the file before saving the PrintJob
        String fileIdentifier = UUID.randomUUID().toString();

        // Declare printJob outside try-catch so it's accessible in catch block
        PrintJob printJob = new PrintJob();

        try {
            // Upload file to MinIO cloud storage using the generated identifier
            FileStorageService.FileUploadResult uploadResult = fileStorageService.uploadFile(file, fileIdentifier);
            int totalPages = countDocumentPages(file, uploadResult.getFileType());

            // Calculate pricing before creating the PrintJob object
            // This is a temporary calculation for the initial save, actual pricing will be set during vendor matching
            // We need to ensure these are non-null for the first save.
            BigDecimal tempPricePerPage = BigDecimal.ZERO; // Placeholder
            BigDecimal tempTotalPrice = BigDecimal.ZERO; // Placeholder

            // Populate ALL non-nullable fields before the first save
            printJob.setStatus(JobStatus.UPLOADED);
            printJob.setCustomer(customer);
            printJob.setPaperSize(paperSize);
            printJob.setIsColor(isColor);
            printJob.setIsDoubleSided(isDoubleSided);
            printJob.setCopies(copies);
            printJob.setOriginalFileName(uploadResult.getOriginalFileName());
            printJob.setStoredFileName(uploadResult.getStoredFileName());
            printJob.setS3BucketName(uploadResult.getBucketName());
            printJob.setS3ObjectKey(uploadResult.getObjectKey());
            printJob.setFileType(uploadResult.getFileType());
            printJob.setFileSizeBytes(uploadResult.getFileSizeBytes());
            printJob.setTotalPages(totalPages);
            printJob.setTrackingCode(generateUniqueTrackingCode());
            printJob.setPricePerPage(tempPricePerPage); // Set temporary price
            printJob.setTotalPrice(tempTotalPrice);     // Set temporary total price

            // Save the PrintJob for the first time (all non-nullable fields are now set)
            printJob = printJobRepository.save(printJob);

            // Now proceed with vendor matching and update the job with actual prices
            if (manualVendorId != null) {
                startVendorOfferProcess(printJob, customerLatitude, customerLongitude, new ArrayList<>(), manualVendorId);
            } else {
                startVendorOfferProcess(printJob, customerLatitude, customerLongitude, new ArrayList<>(), null);
            }

            return printJob;

        } catch (Exception e) {
            // If anything goes wrong, delete the partially created job and re-throw
            // Note: File in MinIO might remain, consider adding cleanup for failed uploads
            if (printJob != null && printJob.getId() != null && printJobRepository.existsById(printJob.getId())) {
                printJobRepository.delete(printJob);
            }
            throw new RuntimeException("Failed to create print job: " + e.getMessage(), e);
        }
    }

    public void startVendorOfferProcess(PrintJob job, Double customerLat, Double customerLon, List<Long> excludedVendorIds, Long manualVendorId) {
        List<VendorMatch> bestMatches;

        if (manualVendorId != null) {
            Vendor vendor = vendorRepository.findById(manualVendorId)
                .orElseThrow(() -> new IllegalArgumentException("Invalid vendor ID selected."));
            VendorMatch match = calculateVendorMatch(vendor, customerLat, customerLon, job.getIsColor(), job.getIsDoubleSided(), job.getTotalPages(), job.getCopies());
            bestMatches = (match != null) ? List.of(match) : new ArrayList<>();
        } else {
            bestMatches = findBestVendorMatches(job, customerLat, customerLon, excludedVendorIds);
        }

        if (bestMatches.isEmpty()) {
            job.setStatus(JobStatus.NO_VENDORS_AVAILABLE);
            printJobRepository.save(job);
            notificationService.notifyCustomerOfStatusUpdate(job.getTrackingCode(), job.getStatus().name(), "We couldn't find any available vendors for your print job.");
            return;
        }

        VendorMatch bestMatch = bestMatches.get(0);
        Vendor vendor = bestMatch.getVendor();

        job.setVendor(vendor);
        job.setPricePerPage(bestMatch.getPricePerPage());
        job.setTotalPrice(bestMatch.getTotalPrice());
        job.setStatus(JobStatus.AWAITING_ACCEPTANCE);
        job.setMatchedAt(LocalDateTime.now());
        printJobRepository.save(job);

        notificationService.notifyVendorOfNewJob(vendor.getId(), job);

        ScheduledFuture<?> timeoutTask = taskScheduler.schedule(
                () -> handleVendorTimeout(job.getId(), vendor.getId(), customerLat, customerLon, excludedVendorIds, manualVendorId),
                Instant.now().plusSeconds(90)
        );
        scheduledTasks.put(job.getId(), timeoutTask);
    }

    public void handleVendorTimeout(Long jobId, Long vendorId, Double customerLat, Double customerLon, List<Long> excludedVendorIds, Long manualVendorId) {
        Optional<PrintJob> jobOpt = printJobRepository.findById(jobId);
        if (jobOpt.isPresent()) {
            PrintJob job = jobOpt.get();
            if (job.getStatus() == JobStatus.AWAITING_ACCEPTANCE && job.getVendor().getId().equals(vendorId)) {
                job.setStatus(JobStatus.VENDOR_TIMEOUT);
                printJobRepository.save(job);

                if (manualVendorId == null) {
                    List<Long> newExcludedIds = new ArrayList<>(excludedVendorIds);
                    newExcludedIds.add(vendorId);
                    startVendorOfferProcess(job, customerLat, customerLon, newExcludedIds, null);
                } else {
                    notificationService.notifyCustomerOfStatusUpdate(job.getTrackingCode(), job.getStatus().name(), "The selected vendor did not respond in time.");
                }
            }
        }
        scheduledTasks.remove(jobId);
    }

    public PrintJob acceptJob(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);

        if (job.getStatus() != JobStatus.AWAITING_ACCEPTANCE) {
            throw new RuntimeException("Job cannot be accepted in its current state: " + job.getStatus());
        }

        // Cancel the scheduled timeout task since the vendor has responded.
        cancelTimeoutTask(jobId);

        job.setStatus(JobStatus.ACCEPTED);
        job.setAcceptedAt(LocalDateTime.now());
        printJobRepository.save(job);

        // 🎉 Enhanced notification with both WebSocket and Email!
        notificationService.notifyCustomerJobAccepted(job);

        return job;
    }

    public PrintJob rejectJob(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);

        if (job.getStatus() != JobStatus.AWAITING_ACCEPTANCE) {
            throw new RuntimeException("Job cannot be rejected in its current state: " + job.getStatus());
        }

        cancelTimeoutTask(jobId);

        job.setStatus(JobStatus.VENDOR_REJECTED);
        printJobRepository.save(job);

        startVendorOfferProcess(job, vendor.getLatitude(), vendor.getLongitude(), List.of(vendor.getId()), null);

        return job;
    }

    private void cancelTimeoutTask(Long jobId) {
        ScheduledFuture<?> timeoutTask = scheduledTasks.get(jobId);
        if (timeoutTask != null) {
            timeoutTask.cancel(false);
            scheduledTasks.remove(jobId);
        }
    }

    private List<VendorMatch> findBestVendorMatches(PrintJob job, Double customerLat, Double customerLon, List<Long> excludedVendorIds) {
        List<Vendor> eligibleVendors = getEligibleVendors(job.getPaperSize(), job.getIsColor()).stream()
                .filter(v -> !excludedVendorIds.contains(v.getId()))
                .collect(Collectors.toList());

        if (eligibleVendors.isEmpty()) {
            return new ArrayList<>();
        }

        List<VendorMatch> vendorMatches = eligibleVendors.stream()
                .map(vendor -> calculateVendorMatch(vendor, customerLat, customerLon, job.getIsColor(), job.getIsDoubleSided(), job.getTotalPages(), job.getCopies()))
                .filter(Objects::nonNull)
                .collect(Collectors.toList());

        vendorMatches.sort(Comparator.comparingDouble(VendorMatch::getMatchScore));

        return vendorMatches;
    }

    public int countDocumentPages(MultipartFile file, FileType fileType) {
        try {
            switch (fileType) {
                case PDF:
                    try (PDDocument document = PDDocument.load(file.getInputStream())) {
                        return document.getNumberOfPages();
                    }
                case DOCX:
                    try (XWPFDocument document = new XWPFDocument(file.getInputStream())) {
                        return Math.max(1, (int) Math.ceil(document.getParagraphs().stream().mapToInt(p -> p.getText().length()).sum() / 2000.0));
                    }
                default:
                    return 1;
            }
        } catch (Exception e) {
            log.warn("Page counting failed for file: {} - {}", file.getOriginalFilename(), e.getMessage());
            return 1;
        }
    }

    private List<Vendor> getEligibleVendors(PaperSize paperSize, boolean isColor) {
        log.debug("Finding eligible vendors for job - paperSize: {}, isColor: {}", paperSize, isColor);
        List<Vendor> allVendors = vendorRepository.findAll();
        log.debug("Found {} total vendors in database", allVendors.size());

        List<Vendor> activeVendors = allVendors.stream()
                .filter(v -> {
                    boolean isEligible = Boolean.TRUE.equals(v.getEmailVerified()) && Boolean.TRUE.equals(v.getIsStoreOpen()) && Boolean.TRUE.equals(v.getIsActive());
                    if (!isEligible) {
                        log.debug("Filtering out Vendor ID: {} - active: {}, store_open: {}, email_verified: {}", 
                            v.getId(), v.getIsActive(), v.getIsStoreOpen(), v.getEmailVerified());
                    }
                    return isEligible;
                })
                .collect(Collectors.toList());
        log.debug("Found {} active vendors after status filtering", activeVendors.size());

        List<Vendor> capableVendors = activeVendors.stream()
                .filter(v -> supportsJobRequirements(v, paperSize, isColor))
                .collect(Collectors.toList());
        log.debug("Found {} eligible vendors after capability filtering", capableVendors.size());
        return capableVendors;
    }

    private boolean supportsJobRequirements(Vendor vendor, PaperSize paperSize, boolean isColor) {
        String capabilitiesJson = vendor.getPrinterCapabilities();
        if (capabilitiesJson == null || capabilitiesJson.isBlank()) {
            log.debug("Vendor ID: {} has no capabilities set. Assuming TRUE.", vendor.getId());
            return true;
        }
        // TODO: Implement full JSON parsing logic here based on the agreed-upon structure.
        return true;
    }

    private VendorMatch calculateVendorMatch(Vendor vendor, Double customerLat, Double customerLon, boolean isColor, boolean isDoubleSided, int totalPages, int copies) {
        try {
            double distance = calculateDistance(customerLat, customerLon, vendor.getLatitude(), vendor.getLongitude());
            if (distance > 20.0) return null;

            BigDecimal pricePerPage = getVendorPricePerPage(vendor, isColor, isDoubleSided);
            BigDecimal totalPrice = calculateTotalPrice(pricePerPage, totalPages, copies);
            double matchScore = calculateMatchScore(distance, totalPrice);

            return new VendorMatch(vendor, distance, pricePerPage, totalPrice, matchScore);
        } catch (Exception e) {
            return null;
        }
    }

    private double calculateDistance(double lat1, double lon1, double lat2, double lon2) {
        final double EARTH_RADIUS_KM = 6371.0;
        double dLat = Math.toRadians(lat2 - lat1);
        double dLon = Math.toRadians(lon2 - lon1);
        double a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(Math.toRadians(lat1)) * Math.cos(Math.toRadians(lat2)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        double c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return EARTH_RADIUS_KM * c;
    }

    private double calculateMatchScore(double distance, BigDecimal totalPrice) {
        return distance * 0.7 + totalPrice.doubleValue() * 0.3;
    }

    public List<Map<String, Object>> getQuoteForJob(MultipartFile file, String paperSizeStr, boolean isColor, boolean isDoubleSided, int copies, double customerLatitude, double customerLongitude) throws Exception {
        log.debug("Entering getQuoteForJob");
        log.debug("Job Params: paperSize={}, isColor={}, copies={}", paperSizeStr, isColor, copies);

        PaperSize paperSize = PaperSize.fromString(paperSizeStr);
        validateJobRequest(file, paperSize, copies, customerLatitude, customerLongitude);

        FileType fileType = fileStorageService.detectFileType(file);
        int totalPages = countDocumentPages(file, fileType);
        log.debug("Detected File Type: {}, Total Pages: {}", fileType, totalPages);

        List<Vendor> eligibleVendors = getEligibleVendors(paperSize, isColor);

        if (eligibleVendors.isEmpty()) {
            log.debug("No eligible vendors found. Returning empty list.");
            return new ArrayList<>();
        }

        log.debug("Mapping {} eligible vendors to quotes...", eligibleVendors.size());
        List<Map<String, Object>> quotes = eligibleVendors.stream()
            .map(vendor -> {
                try {
                    double distance = calculateDistance(customerLatitude, customerLongitude, vendor.getLatitude(), vendor.getLongitude());
                    log.debug("Processing Vendor ID: {}, Distance: {}", vendor.getId(), distance);
                    if (distance > 20.0) {
                        log.debug("Vendor is too far. Skipping.");
                        return null;
                    }

                    BigDecimal pricePerPage = getVendorPricePerPage(vendor, isColor, isDoubleSided);
                    if (pricePerPage == null) {
                        log.debug("Vendor ID: {} has NULL price for this job type. Skipping.", vendor.getId());
                        return null;
                    }
                    BigDecimal totalPrice = calculateTotalPrice(pricePerPage, totalPages, copies);
                    log.debug("Vendor ID: {}, Price: {}", vendor.getId(), totalPrice);

                    Map<String, Object> quote = new HashMap<>();
                    quote.put("vendorId", vendor.getId());
                    quote.put("businessName", vendor.getBusinessName());
                    quote.put("distance", String.format("%.2f km", distance));
                    quote.put("price", totalPrice.setScale(2, RoundingMode.HALF_UP).toString());
                    quote.put("address", vendor.getBusinessAddress());
                    quote.put("rating", 5.0); // Placeholder
                    return quote;
                } catch (Exception e) {
                    log.warn("ERROR processing vendor ID: {} - {}", vendor.getId(), e.getMessage());
                    return null;
                }
            })
            .filter(Objects::nonNull)
            .sorted(Comparator.comparingDouble(q -> Double.parseDouble(((String) ((Map<String, Object>) q).get("distance")).replace(" km", "")))
                              .thenComparing(q -> new BigDecimal((String) ((Map<String, Object>) q).get("price"))))
            .collect(Collectors.toList());

        log.debug("Exiting getQuoteForJob, returning {} quotes.", quotes.size());
        return quotes;
    }

    public PrintJob startPrinting(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);
        if (job.getStatus() != JobStatus.ACCEPTED) throw new RuntimeException("Job must be accepted before printing");
        
        job.setStatus(JobStatus.PRINTING);
        job.setPrintingAt(LocalDateTime.now());
        PrintJob savedJob = printJobRepository.save(job);
        
        // 🚀 AUTOMATIC STATUS PROGRESSION
        // Calculate estimated printing time based on job complexity
        int estimatedPrintingMinutes = calculatePrintingTime(job);
        
        // Schedule automatic transition to READY status
        ScheduledFuture<?> autoPrintingTask = taskScheduler.schedule(
            () -> autoProgressToReady(jobId),
            Instant.now().plusSeconds(estimatedPrintingMinutes * 60)
        );
        
        // Store the scheduled task so it can be cancelled if needed
        scheduledTasks.put(jobId, autoPrintingTask);
        
        // 🔧 FIXED: Ensure customer data is fully loaded before async notification
        // Reload the job with customer data to prevent Hibernate session issues
        PrintJob jobWithCustomer = printJobRepository.findById(savedJob.getId())
            .orElse(savedJob);
        
        // 🖨️ Enhanced notification for printing start (WebSocket + Email!)
        notificationService.notifyCustomerJobPrinting(jobWithCustomer, estimatedPrintingMinutes);
        
        return savedJob;
    }
    
    /**
     * Automatically progress job from PRINTING to READY
     * 🔧 FIXED: Properly handle Hibernate lazy loading in scheduled task context
     */
    @Transactional
    public void autoProgressToReady(Long jobId) {
        try {
            // 🚀 USE EXPLICIT LOADING to avoid LazyInitializationException
            Optional<PrintJob> jobOpt = printJobRepository.findById(jobId);
            if (jobOpt.isPresent()) {
                PrintJob job = jobOpt.get();
                
                // Only auto-progress if still in PRINTING status
                if (job.getStatus() == JobStatus.PRINTING) {
                    job.setStatus(JobStatus.READY);
                    job.setReadyAt(LocalDateTime.now());
                    PrintJob savedJob = printJobRepository.save(job);
                    
                    // Create a data transfer object with all necessary information
                    Map<String, Object> jobData = new HashMap<>();
                    jobData.put("id", savedJob.getId());
                    jobData.put("trackingCode", savedJob.getTrackingCode());
                    
                    // Explicitly fetch customer and vendor data within this transaction
                    if (savedJob.getCustomer() != null) {
                        User customer = userRepository.findById(savedJob.getCustomer().getId()).orElse(null);
                        if (customer != null) {
                            jobData.put("customerEmail", customer.getEmail());
                            jobData.put("customerName", customer.getName());
                        }
                    }
                    
                    if (savedJob.getVendor() != null) {
                        Vendor vendor = vendorRepository.findById(savedJob.getVendor().getId()).orElse(null);
                        if (vendor != null) {
                            jobData.put("vendorName", vendor.getBusinessName());
                            jobData.put("vendorAddress", vendor.getBusinessAddress());
                        }
                    }
                    
                    // 🎯 Send email directly using explicit parameters to avoid Hibernate proxy issues
                    if (jobData.containsKey("customerEmail")) {
                        try {
                            // Use the existing sendJobReadyForPickupEmail but with loaded data
                            // Create a minimal email notification directly
                            emailService.sendDirectJobReadyEmail(
                                (String) jobData.get("customerEmail"),
                                (String) jobData.get("customerName"),
                                savedJob.getTrackingCode(),
                                savedJob.getOriginalFileName(),
                                savedJob.getPrintSpecsSummary(),
                                savedJob.getTotalPrice().toString(),
                                (String) jobData.get("vendorName"),
                                (String) jobData.get("vendorAddress")
                            );
                            log.debug("Auto-progression email sent for job: {}", savedJob.getTrackingCode());
                        } catch (Exception e) {
                            log.warn("Failed to send auto-progression email for job {}: {}", savedJob.getTrackingCode(), e.getMessage());
                        }
                    }
                    
                    // Still send WebSocket notification (works with entity reference)
                    notificationService.notifyCustomerOfStatusUpdate(
                        savedJob.getTrackingCode(),
                        "READY",
                        "Great news! Your print job is ready for pickup!"
                    );
                    
                    // Schedule auto-completion after 24 hours if not picked up
                    ScheduledFuture<?> autoCompleteTask = taskScheduler.schedule(
                        () -> autoCompleteJob(jobId),
                        Instant.now().plusSeconds(24 * 60 * 60) // 24 hours
                    );
                    scheduledTasks.put(jobId, autoCompleteTask);
                }
            }
        } catch (Exception e) {
            log.error("Error in autoProgressToReady for job {}: {}", jobId, e.getMessage(), e);
        } finally {
            scheduledTasks.remove(jobId);
        }
    }
    
    /**
     * Automatically complete job after extended period (24 hours) if not manually completed
     */
    private void autoCompleteJob(Long jobId) {
        try {
            Optional<PrintJob> jobOpt = printJobRepository.findById(jobId);
            if (jobOpt.isPresent()) {
                PrintJob job = jobOpt.get();
                
                // Only auto-complete if still in READY status
                if (job.getStatus() == JobStatus.READY) {
                    job.setStatus(JobStatus.COMPLETED);
                    job.setCompletedAt(LocalDateTime.now());
                    printJobRepository.save(job);
                    
                    // ✅ Enhanced final notification (WebSocket + Email!)
                    notificationService.notifyCustomerJobCompleted(job);
                }
            }
        } catch (Exception e) {
            log.error("Error in autoCompleteJob for job {}: {}", jobId, e.getMessage());
        } finally {
            scheduledTasks.remove(jobId);
        }
    }
    
    /**
     * Calculate estimated printing time based on job complexity
     */
    private int calculatePrintingTime(PrintJob job) {
        int baseTimeMinutes = 2; // Base time for any job
        int timePerPage = job.getIsColor() ? 1 : 0; // 1 minute per color page, 0 for B&W
        int timeForCopies = (job.getCopies() - 1) * 1; // 1 minute per additional copy
        int totalPages = job.getTotalPages() * job.getCopies();
        
        // More pages = more time
        int pageTimeMinutes = totalPages * (job.getIsColor() ? 1 : 0) + (totalPages / 5);
        
        int totalTime = baseTimeMinutes + pageTimeMinutes + timeForCopies;
        
        // Minimum 1 minute, maximum 30 minutes
        return Math.min(Math.max(totalTime, 1), 30);
    }

    public PrintJob markJobReady(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);
        if (job.getStatus() != JobStatus.PRINTING) throw new RuntimeException("Job must be printing before marking ready");
        job.setStatus(JobStatus.READY);
        job.setReadyAt(LocalDateTime.now());
        PrintJob savedJob = printJobRepository.save(job);
        
        // 🎯 FIXED: Send notification when manually marked as ready
        // ✅ Enhanced notification that job is ready for pickup (WebSocket + Email!)
        // This is THE MOST IMPORTANT notification - customers get emails even when offline!
        notificationService.notifyCustomerJobReadyForPickup(savedJob);
        
        // Cancel any pending auto-progression task since it's now manually ready
        ScheduledFuture<?> pendingTask = scheduledTasks.remove(jobId);
        if (pendingTask != null && !pendingTask.isDone()) {
            pendingTask.cancel(false);
        }
        
        // Schedule auto-completion after 24 hours if not picked up
        ScheduledFuture<?> autoCompleteTask = taskScheduler.schedule(
            () -> autoCompleteJob(jobId),
            Instant.now().plusSeconds(24 * 60 * 60) // 24 hours
        );
        scheduledTasks.put(jobId, autoCompleteTask);
        
        return savedJob;
    }

    public PrintJob completeJob(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);
        if (job.getStatus() != JobStatus.READY) throw new RuntimeException("Job must be ready before completing");
        job.setStatus(JobStatus.COMPLETED);
        job.setCompletedAt(LocalDateTime.now());
        PrintJob savedJob = printJobRepository.save(job);
        
        // 🎯 FIXED: Send notification when manually completed
        // ✅ Enhanced final notification (WebSocket + Email!)
        notificationService.notifyCustomerJobCompleted(savedJob);
        
        // Cancel any pending auto-completion task since it's now manually completed
        ScheduledFuture<?> pendingTask = scheduledTasks.remove(jobId);
        if (pendingTask != null && !pendingTask.isDone()) {
            pendingTask.cancel(false);
        }
        
        return savedJob;
    }

    public Optional<PrintJob> getJobByTrackingCode(String trackingCode) {
        return printJobRepository.findByTrackingCode(trackingCode);
    }

    public List<PrintJob> getCustomerJobHistory(User customer) {
        return printJobRepository.findByCustomerOrderByCreatedAtDesc(customer);
    }

    public List<PrintJob> getVendorJobQueue(Vendor vendor) {
        return printJobRepository.findPendingJobsByVendor(vendor);
    }

    public String getJobFileStreamingUrl(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);
        return fileStorageService.getStreamingUrlForPrinting(job.getS3ObjectKey());
    }
    
    public java.io.InputStream getJobFileStream(Long jobId, Vendor vendor) {
        PrintJob job = getJobForVendor(jobId, vendor);
        return fileStorageService.getFileStream(job.getS3ObjectKey());
    }
    
    public PrintJob getJobById(Long jobId) {
        return printJobRepository.findById(jobId)
                .orElseThrow(() -> new RuntimeException("Print job not found: " + jobId));
    }

    private PrintJob getJobForVendor(Long jobId, Vendor vendor) {
        System.out.println("=== DEBUGGING JOB ACCESS ====");
        System.out.println("Requested job ID: " + jobId);
        System.out.println("Authenticated vendor: ID=" + vendor.getId() + ", Name='" + vendor.getBusinessName() + "', Email='" + vendor.getEmail() + "', StoreOpen=" + vendor.getIsStoreOpen());
        
        PrintJob job = printJobRepository.findById(jobId).orElseThrow(() -> {
            System.out.println("ERROR: Print job not found: " + jobId);
            return new RuntimeException("Print job not found: " + jobId);
        });
        
        // Add detailed logging for debugging
        System.out.println("Job " + jobId + " details: vendor_id=" + (job.getVendor() != null ? job.getVendor().getId() : "NULL") + 
                ", vendor_name='" + (job.getVendor() != null ? job.getVendor().getBusinessName() : "NULL") + "'" +
                ", status=" + job.getStatus() + 
                ", tracking_code=" + job.getTrackingCode() +
                ", created_at=" + job.getCreatedAt());
        
        // Check if job has no vendor assigned (this might be the issue)
        if (job.getVendor() == null) {
            System.out.println("CRITICAL: Job " + jobId + " has no vendor assigned! This indicates job was not properly assigned during creation.");
            System.out.println("Job status: " + job.getStatus() + ", Customer: " + (job.getCustomer() != null ? job.getCustomer().getEmail() : "NULL"));
            
            // Get all vendors to see what's available
            List<Vendor> allVendors = vendorRepository.findAll();
            System.out.println("Available vendors in database: " + allVendors.size());
            for (Vendor v : allVendors) {
                System.out.println("  Vendor ID=" + v.getId() + ", Name='" + v.getBusinessName() + "', Active=" + v.getIsActive() + ", StoreOpen=" + v.getIsStoreOpen() + ", EmailVerified=" + v.getEmailVerified());
            }
            
            throw new RuntimeException("Access denied - job " + jobId + " has no vendor assigned. This indicates a system error during job creation.");
        }
        
        if (!job.getVendor().getId().equals(vendor.getId())) {
            System.out.println("VENDOR MISMATCH for job " + jobId + ": job belongs to vendor_id=" + job.getVendor().getId() + " ('" + job.getVendor().getBusinessName() + "') but request from vendor_id=" + vendor.getId() + " ('" + vendor.getBusinessName() + "')");
            throw new RuntimeException("Access denied - job " + jobId + " may not belong to your vendor account");
        }
        
        System.out.println("SUCCESS: Job " + jobId + " access granted for vendor " + vendor.getId() + " ('" + vendor.getBusinessName() + "')");
        System.out.println("=== END DEBUGGING ====");
        return job;
    }
    
    /**
     * Debug/fix method to reassign jobs to the correct vendor
     * This can be used to fix orphaned jobs or incorrect vendor assignments
     */
    @Transactional
    public PrintJob reassignJobToVendor(Long jobId, Vendor vendor) {
        log.info("Attempting to reassign job {} to vendor {} ('{}')", jobId, vendor.getId(), vendor.getBusinessName());
        
        PrintJob job = printJobRepository.findById(jobId).orElseThrow(() -> 
            new RuntimeException("Print job not found: " + jobId));
        
        log.info("Current job {} assignment: vendor_id={}, vendor_name='{}', status={}", 
                jobId,
                job.getVendor() != null ? job.getVendor().getId() : "NULL",
                job.getVendor() != null ? job.getVendor().getBusinessName() : "NULL",
                job.getStatus());
        
        // Update the job's vendor assignment
        job.setVendor(vendor);
        
        // If the job doesn't have proper pricing, calculate it based on the new vendor
        if (job.getPricePerPage() == null || job.getPricePerPage().compareTo(BigDecimal.ZERO) == 0) {
            BigDecimal pricePerPage = getVendorPricePerPage(vendor, job.getIsColor(), job.getIsDoubleSided());
            BigDecimal totalPrice = calculateTotalPrice(pricePerPage, job.getTotalPages(), job.getCopies());
            job.setPricePerPage(pricePerPage);
            job.setTotalPrice(totalPrice);
            log.info("Updated pricing for job {}: price_per_page={}, total_price={}", jobId, pricePerPage, totalPrice);
        }
        
        PrintJob savedJob = printJobRepository.save(job);
        log.info("Successfully reassigned job {} to vendor {} ('{}')", jobId, vendor.getId(), vendor.getBusinessName());
        
        return savedJob;
    }

    private void validateJobRequest(MultipartFile file, PaperSize paperSize, int copies, Double customerLatitude, Double customerLongitude) {
        if (file == null || file.isEmpty() || file.getSize() > 50 * 1024 * 1024) throw new IllegalArgumentException("Invalid file");
        if (paperSize == null || copies < 1 || copies > 100) throw new IllegalArgumentException("Invalid print settings");
        if (customerLatitude == null || customerLongitude == null || customerLatitude < -90 || customerLatitude > 90 || customerLongitude < -180 || customerLongitude > 180) throw new IllegalArgumentException("Invalid location");
    }

    private String generateUniqueTrackingCode() {
        String trackingCode;
        do {
            trackingCode = "PJ" + String.format("%06d", random.nextInt(1000000));
        } while (printJobRepository.existsByTrackingCode(trackingCode));
        return trackingCode;
    }

    private BigDecimal getVendorPricePerPage(Vendor vendor, boolean isColor, boolean isDoubleSided) {
        if (isColor) return isDoubleSided ? vendor.getPricePerPageColorDoubleSided() : vendor.getPricePerPageColorSingleSided();
        else return isDoubleSided ? vendor.getPricePerPageBWDoubleSided() : vendor.getPricePerPageBWSingleSided();
    }

    private BigDecimal calculateTotalPrice(BigDecimal pricePerPage, int totalPages, int copies) {
        return pricePerPage.multiply(new BigDecimal(totalPages)).multiply(new BigDecimal(copies)).setScale(2, RoundingMode.HALF_UP);
    }

    private static class VendorMatch {
        private final Vendor vendor;
        private final double distance;
        private final BigDecimal pricePerPage;
        private final BigDecimal totalPrice;
        private final double matchScore;

        public VendorMatch(Vendor vendor, double distance, BigDecimal pricePerPage, BigDecimal totalPrice, double matchScore) {
            this.vendor = vendor;
            this.distance = distance;
            this.pricePerPage = pricePerPage;
            this.totalPrice = totalPrice;
            this.matchScore = matchScore;
        }

        public Vendor getVendor() { return vendor; }
        public double getDistance() { return distance; }
        public BigDecimal getPricePerPage() { return pricePerPage; }
        public BigDecimal getTotalPrice() { return totalPrice; }
        public double getMatchScore() { return matchScore; }
    }
}