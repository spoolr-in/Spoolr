package com.printwave.core.enums;

/**
 * FileType Enum - Supported file formats for printing
 * 
 * Think of this like file types you can print:
 * - PDF: Most common document format (like Adobe Reader files)
 * - DOCX: Microsoft Word documents  
 * - JPG: Photo format (like camera photos)
 * - PNG: Image format (like screenshots)
 */
public enum FileType {
    PDF("PDF Document", "application/pdf", ".pdf", "Portable Document Format - Best for documents"),
    DOCX("Word Document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx", "Microsoft Word document"),
    JPG("JPEG Image", "image/jpeg", ".jpg", "Photo format - Good for pictures"),
    PNG("PNG Image", "image/png", ".png", "Image format - Good for screenshots and graphics");
    
    private final String displayName;
    private final String mimeType;        // Technical identifier for web browsers
    private final String fileExtension;  // File ending like .pdf
    private final String description;
    
    // Constructor to store file type details
    FileType(String displayName, String mimeType, String fileExtension, String description) {
        this.displayName = displayName;
        this.mimeType = mimeType;
        this.fileExtension = fileExtension;
        this.description = description;
    }
    
    // Getter methods
    public String getDisplayName() {
        return displayName;
    }
    
    public String getMimeType() {
        return mimeType;
    }
    
    public String getFileExtension() {
        return fileExtension;
    }
    
    public String getDescription() {
        return description;
    }
    
    // Helper methods for business logic
    public boolean isDocument() {
        return this == PDF || this == DOCX;
    }
    
    public boolean isImage() {
        return this == JPG || this == PNG;
    }
    
    public boolean requiresConversion() {
        // DOCX might need conversion to PDF for printing
        return this == DOCX;
    }
    
    // Helper method to detect file type from filename
    public static FileType fromFileName(String fileName) {
        if (fileName == null || !fileName.contains(".")) {
            throw new IllegalArgumentException("Invalid file name: " + fileName);
        }
        
        String extension = fileName.substring(fileName.lastIndexOf(".")).toLowerCase();
        
        for (FileType type : FileType.values()) {
            if (type.fileExtension.equalsIgnoreCase(extension)) {
                return type;
            }
        }
        
        throw new IllegalArgumentException("Unsupported file type: " + extension);
    }
    
    // Helper method to get file type from MIME type (from web uploads)
    public static FileType fromMimeType(String mimeType) {
        for (FileType type : FileType.values()) {
            if (type.mimeType.equalsIgnoreCase(mimeType)) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unsupported MIME type: " + mimeType);
    }
}
