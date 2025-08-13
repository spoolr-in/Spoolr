package com.spoolr.core.service;

import com.spoolr.core.enums.FileType;
import io.minio.*;
import io.minio.http.Method;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.InputStream;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.concurrent.TimeUnit;

/**
 * FileStorageService - Handles all file storage operations with MinIO
 * 
 * 🎯 HYBRID STREAMING APPROACH (Perfect for India):
 * - Station app gets temporary URL from our API
 * - Station app streams file to MEMORY (not disk storage)
 * - Station app sends from memory directly to printer
 * - Memory cleared after printing (no permanent storage)
 * - Works with ALL printers (USB, WiFi, basic models)
 * 
 * 🚀 User Flow (UNCHANGED):
 * 1. Customer uploads → Core API stores in cloud
 * 2. System matches with best vendor (price/distance/capability) 
 * 3. Vendor sees job in Station App queue
 * 4. Vendor clicks "Accept" → job moves to printing queue
 * 5. Vendor clicks "Print" → Station app streams & prints
 * 6. Customer collects printed document
 * 
 * 🔄 Only the final printing step is optimized!
 */
@Service
public class FileStorageService {

    private final MinioClient minioClient;
    private final String bucketName;

    @Autowired
    public FileStorageService(MinioClient minioClient, String minioBucketName) {
        this.minioClient = minioClient;
        this.bucketName = minioBucketName;
    }

    /**
     * Upload a file to MinIO cloud storage
     * 
     * Customer uploads document → Stored securely in cloud
     * 
     * @param file The file uploaded by customer (PDF, image, etc.)
     * @param fileIdentifier A unique string (e.g., UUID) for file naming
     * @return FileUploadResult containing storage information
     */
    public FileUploadResult uploadFile(MultipartFile file, String fileIdentifier) {
        try {
            // Ensure bucket exists (create if it doesn't)
            createBucketIfNotExists();
            
            // Generate unique file name and organized path
            String originalFileName = file.getOriginalFilename();
            FileType fileType = FileType.fromFileName(originalFileName);
            String uniqueFileName = generateUniqueFileName(originalFileName, fileIdentifier);
            String objectKey = generateObjectKey(uniqueFileName);
            
            // Upload file to MinIO with proper content type
            minioClient.putObject(
                PutObjectArgs.builder()
                    .bucket(bucketName)
                    .object(objectKey)
                    .stream(file.getInputStream(), file.getSize(), -1)
                    .contentType(determineContentType(file, fileType))
                    .build()
            );
            
            // Return upload result with all details needed for PrintJob entity
            return new FileUploadResult(
                originalFileName,
                uniqueFileName,
                objectKey,
                bucketName,
                file.getSize(),
                fileType
            );
            
        } catch (Exception e) {
            throw new RuntimeException("Failed to upload file: " + e.getMessage(), e);
        }
    }
    
    /**
     * 🚀 STATION APP: Get temporary streaming URL for printing
     * 
     * This is what Station App calls when vendor clicks "Print" button:
     * 
     * Station App Process:
     * 1. Call this method to get streaming URL
     * 2. Stream file directly into memory (not saved to disk)
     * 3. Send from memory to printer driver
     * 4. Clear memory after printing
     * 5. No permanent storage on vendor's computer!
     * 
     * Works with ALL Indian printers:
     * - Basic USB printers (HP DeskJet, Canon Pixma, Epson L-series)
     * - WiFi printers (any brand)
     * - Business printers (HP, Canon, Brother)
     * 
     * @param objectKey The file path in cloud storage
     * @return Temporary URL valid for 30 minutes
     */
    public String getStreamingUrlForPrinting(String objectKey) {
        try {
            // Generate temporary URL valid for 30 minutes
            // Long enough for printing, short enough for security
            return minioClient.getPresignedObjectUrl(
                GetPresignedObjectUrlArgs.builder()
                    .method(Method.GET)
                    .bucket(bucketName)
                    .object(objectKey)
                    .expiry(30, TimeUnit.MINUTES) // 30 minutes expiry
                    .build()
            );
        } catch (Exception e) {
            throw new RuntimeException("Failed to generate streaming URL for printing: " + e.getMessage(), e);
        }
    }
    
    /**
     * 🔒 ADMIN: Get file stream for system operations only
     * 
     * This is for system-level operations (like file validation, thumbnails)
     * Station app should use getStreamingUrlForPrinting() instead
     * 
     * @param objectKey The path/name of file in storage
     * @return InputStream to read the file content
     */
    public InputStream getFileStream(String objectKey) {
        try {
            return minioClient.getObject(
                GetObjectArgs.builder()
                    .bucket(bucketName)
                    .object(objectKey)
                    .build()
            );
        } catch (Exception e) {
            throw new RuntimeException("Failed to get file stream: " + e.getMessage(), e);
        }
    }
    
    /**
     * Delete a file from storage
     * Called when print job is completed/cancelled after retention period
     * 
     * @param objectKey The file path in storage
     */
    public void deleteFile(String objectKey) {
        try {
            minioClient.removeObject(
                RemoveObjectArgs.builder()
                    .bucket(bucketName)
                    .object(objectKey)
                    .build()
            );
        } catch (Exception e) {
            throw new RuntimeException("Failed to delete file: " + e.getMessage(), e);
        }
    }
    
    /**
     * Check if a file exists in storage
     * 
     * @param objectKey The file path in storage
     * @return true if file exists, false otherwise
     */
    public boolean fileExists(String objectKey) {
        try {
            minioClient.statObject(
                StatObjectArgs.builder()
                    .bucket(bucketName)
                    .object(objectKey)
                    .build()
            );
            return true;
        } catch (Exception e) {
            return false;
        }
    }
    
    /**
     * Get file metadata (size, type, etc.) without downloading
     * Useful for displaying file info in Station app queue
     * 
     * @param objectKey The file path in storage
     * @return File information
     */
    public FileMetadata getFileInfo(String objectKey) {
        try {
            StatObjectResponse stat = minioClient.statObject(
                StatObjectArgs.builder()
                    .bucket(bucketName)
                    .object(objectKey)
                    .build()
            );
            
            return new FileMetadata(
                objectKey,
                stat.size(),
                stat.contentType(),
                stat.lastModified()
            );
            
        } catch (Exception e) {
            throw new RuntimeException("Failed to get file metadata: " + e.getMessage(), e);
        }
    }
    
    // ===== HELPER METHODS =====
    
    /**
     * Create storage bucket if it doesn't exist
     */
    private void createBucketIfNotExists() {
        try {
            boolean bucketExists = minioClient.bucketExists(
                BucketExistsArgs.builder().bucket(bucketName).build()
            );
            
            if (!bucketExists) {
                minioClient.makeBucket(
                    MakeBucketArgs.builder().bucket(bucketName).build()
                );
            }
        } catch (Exception e) {
            throw new RuntimeException("Failed to create storage bucket: " + e.getMessage(), e);
        }
    }
    
    /**
     * Generate unique file name to avoid conflicts
     * Format: job_UUID_resume.pdf
     * 
     * @param originalFileName Original file name from customer
     * @param fileIdentifier A unique string (e.g., UUID) for file naming
     * @return Unique file name safe for storage
     */
    private String generateUniqueFileName(String originalFileName, String fileIdentifier) {
        // Extract file extension safely
        String extension = "";
        int dotIndex = originalFileName.lastIndexOf('.');
        if (dotIndex > 0 && dotIndex < originalFileName.length() - 1) {
            extension = originalFileName.substring(dotIndex);
        }
        
        // Clean the base name (remove special characters for storage compatibility)
        String baseName = originalFileName.substring(0, dotIndex > 0 ? dotIndex : originalFileName.length());
        baseName = baseName.replaceAll("[^a-zA-Z0-9_-]", "_"); // Replace special chars with underscore
        baseName = baseName.substring(0, Math.min(baseName.length(), 50)); // Limit length
        
        // Create unique name: job_UUID_resume.pdf
        return String.format("job_%s_%s%s", fileIdentifier, baseName, extension);
    }
    
    /**
     * Generate object key (full path in cloud storage)
     * Format: 2024/01/15/job_123_resume.pdf
     * 
     * Organizes files by date for:
     * - Better management and cleanup
     * - Easy backup and archival
     * - Performance optimization
     * 
     * @param fileName The unique file name
     * @return Full storage path with date organization
     */
    private String generateObjectKey(String fileName) {
        LocalDate today = LocalDate.now();
        String datePath = today.format(DateTimeFormatter.ofPattern("yyyy/MM/dd"));
        return String.format("%s/%s", datePath, fileName);
    }
    
    /**
     * Determine proper content type for file
     * Ensures proper handling by browsers and applications
     */
    private String determineContentType(MultipartFile file, FileType fileType) {
        String contentType = file.getContentType();
        
        // Use our enum's MIME type if file type is unknown or generic
        if (contentType == null || contentType.equals("application/octet-stream")) {
            contentType = fileType.getMimeType();
        }
        
        return contentType;
    }

    public FileType detectFileType(MultipartFile file) {
        return FileType.fromFileName(file.getOriginalFilename());
    }
    
    // ===== RESULT CLASSES =====
    
    /**
     * Result of file upload operation
     * Contains all information needed to create PrintJob entity
     */
    public static class FileUploadResult {
        private final String originalFileName;
        private final String storedFileName;
        private final String objectKey;
        private final String bucketName;
        private final Long fileSizeBytes;
        private final FileType fileType;
        
        public FileUploadResult(String originalFileName, String storedFileName, 
                              String objectKey, String bucketName, 
                              Long fileSizeBytes, FileType fileType) {
            this.originalFileName = originalFileName;
            this.storedFileName = storedFileName;
            this.objectKey = objectKey;
            this.bucketName = bucketName;
            this.fileSizeBytes = fileSizeBytes;
            this.fileType = fileType;
        }
        
        // Getters for PrintJob entity creation
        public String getOriginalFileName() { return originalFileName; }
        public String getStoredFileName() { return storedFileName; }
        public String getObjectKey() { return objectKey; }
        public String getBucketName() { return bucketName; }
        public Long getFileSizeBytes() { return fileSizeBytes; }
        public FileType getFileType() { return fileType; }
        
        @Override
        public String toString() {
            return String.format("FileUploadResult{originalFileName='%s', storedFileName='%s', objectKey='%s', fileType=%s, size=%d bytes}", 
                    originalFileName, storedFileName, objectKey, fileType, fileSizeBytes);
        }
    }
    
    /**
     * File metadata for display in Station app
     */
    public static class FileMetadata {
        private final String objectKey;
        private final Long sizeBytes;
        private final String contentType;
        private final java.time.ZonedDateTime uploadedAt;
        
        public FileMetadata(String objectKey, Long sizeBytes, String contentType, java.time.ZonedDateTime uploadedAt) {
            this.objectKey = objectKey;
            this.sizeBytes = sizeBytes;
            this.contentType = contentType;
            this.uploadedAt = uploadedAt;
        }
        
        // Getters
        public String getObjectKey() { return objectKey; }
        public Long getSizeBytes() { return sizeBytes; }
        public String getContentType() { return contentType; }
        public java.time.ZonedDateTime getUploadedAt() { return uploadedAt; }
        
        // Helper methods for Station app display
        public String getFormattedSize() {
            if (sizeBytes < 1024) return sizeBytes + " B";
            if (sizeBytes < 1024 * 1024) return String.format("%.1f KB", sizeBytes / 1024.0);
            return String.format("%.1f MB", sizeBytes / (1024.0 * 1024.0));
        }
    }
}