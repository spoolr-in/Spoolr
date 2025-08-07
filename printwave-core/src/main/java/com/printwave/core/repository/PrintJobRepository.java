package com.printwave.core.repository;

import com.printwave.core.entity.PrintJob;
import com.printwave.core.entity.User;
import com.printwave.core.entity.Vendor;
import com.printwave.core.enums.JobStatus;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

/**
 * PrintJobRepository - Database access layer for print jobs
 * 
 * Think of this as your database query helper.
 * It provides methods to:
 * - Save new print jobs
 * - Find jobs by tracking code, customer, vendor, etc.
 * - Get job history and statistics
 * - Update job status
 */
@Repository
public interface PrintJobRepository extends JpaRepository<PrintJob, Long> {
    
    // ===== BASIC LOOKUP METHODS =====
    
    /**
     * Find a job by its tracking code (for customer tracking)
     * Example: Find job "PJ123456"
     */
    Optional<PrintJob> findByTrackingCode(String trackingCode);
    
    /**
     * Check if a tracking code already exists (to avoid duplicates)
     */
    boolean existsByTrackingCode(String trackingCode);
    
    // ===== CUSTOMER-RELATED QUERIES =====
    
    /**
     * Get all jobs for a specific customer (for order history)
     * Ordered by newest first
     */
    List<PrintJob> findByCustomerOrderByCreatedAtDesc(User customer);
    
    /**
     * Get paginated job history for a customer
     * Useful for "Load More" functionality in frontend
     */
    Page<PrintJob> findByCustomerOrderByCreatedAtDesc(User customer, Pageable pageable);
    
    /**
     * Count total jobs placed by a customer
     */
    long countByCustomer(User customer);
    
    /**
     * Get active jobs for a customer (not completed/cancelled/rejected)
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.customer = :customer AND pj.status IN ('UPLOADED', 'PROCESSING', 'MATCHED', 'ACCEPTED', 'PRINTING', 'READY')")
    List<PrintJob> findActiveJobsByCustomer(@Param("customer") User customer);
    
    // ===== VENDOR-RELATED QUERIES =====
    
    /**
     * Get all jobs assigned to a vendor (for Station app job queue)
     * Only shows jobs that need vendor action
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.vendor = :vendor AND pj.status IN ('MATCHED', 'ACCEPTED', 'PRINTING')")
    List<PrintJob> findPendingJobsByVendor(@Param("vendor") Vendor vendor);
    
    /**
     * Get jobs waiting for vendor acceptance
     */
    List<PrintJob> findByVendorAndStatus(Vendor vendor, JobStatus status);
    
    /**
     * Get completed jobs for a vendor (for earnings calculation)
     */
    List<PrintJob> findByVendorAndStatusOrderByCompletedAtDesc(Vendor vendor, JobStatus status);
    
    /**
     * Count total jobs completed by vendor
     */
    long countByVendorAndStatus(Vendor vendor, JobStatus status);
    
    // ===== STATUS-BASED QUERIES =====
    
    /**
     * Find jobs with specific status
     */
    List<PrintJob> findByStatus(JobStatus status);
    
    /**
     * Find jobs that need to be matched to vendors
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.status = 'UPLOADED' OR pj.status = 'PROCESSING'")
    List<PrintJob> findJobsNeedingMatching();
    
    /**
     * Find jobs ready for pickup (for reminder notifications)
     */
    List<PrintJob> findByStatusAndReadyAtBefore(JobStatus status, LocalDateTime cutoffTime);
    
    // ===== ANONYMOUS JOB QUERIES =====
    
    /**
     * Find anonymous jobs (QR code orders) by vendor
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.customer IS NULL AND pj.vendor = :vendor")
    List<PrintJob> findAnonymousJobsByVendor(@Param("vendor") Vendor vendor);
    
    /**
     * Count anonymous jobs for statistics
     */
    @Query("SELECT COUNT(pj) FROM PrintJob pj WHERE pj.customer IS NULL")
    long countAnonymousJobs();
    
    // ===== TIME-BASED QUERIES =====
    
    /**
     * Find jobs created within a date range
     * Useful for analytics and reporting
     */
    List<PrintJob> findByCreatedAtBetween(LocalDateTime startDate, LocalDateTime endDate);
    
    /**
     * Find jobs created today
     */
    @Query("SELECT pj FROM PrintJob pj WHERE CAST(pj.createdAt AS DATE) = CURRENT_DATE")
    List<PrintJob> findJobsCreatedToday();
    
    /**
     * Find old incomplete jobs (for cleanup)
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.createdAt < :cutoffDate AND pj.status NOT IN ('COMPLETED', 'CANCELLED', 'REJECTED')")
    List<PrintJob> findOldIncompleteJobs(@Param("cutoffDate") LocalDateTime cutoffDate);
    
    // ===== STATISTICS AND ANALYTICS =====
    
    /**
     * Get job count by status for dashboard
     */
    @Query("SELECT pj.status, COUNT(pj) FROM PrintJob pj GROUP BY pj.status")
    List<Object[]> getJobCountByStatus();
    
    /**
     * Calculate total revenue for a vendor
     */
    @Query("SELECT SUM(pj.totalPrice) FROM PrintJob pj WHERE pj.vendor = :vendor AND pj.status = 'COMPLETED'")
    Optional<java.math.BigDecimal> calculateVendorRevenue(@Param("vendor") Vendor vendor);
    
    /**
     * Get popular paper sizes for analytics
     */
    @Query("SELECT pj.paperSize, COUNT(pj) FROM PrintJob pj GROUP BY pj.paperSize ORDER BY COUNT(pj) DESC")
    List<Object[]> getPaperSizeStatistics();
    
    /**
     * Find high-value jobs (for priority handling)
     */
    @Query("SELECT pj FROM PrintJob pj WHERE pj.totalPrice >= :minAmount ORDER BY pj.totalPrice DESC")
    List<PrintJob> findHighValueJobs(@Param("minAmount") java.math.BigDecimal minAmount);
}
