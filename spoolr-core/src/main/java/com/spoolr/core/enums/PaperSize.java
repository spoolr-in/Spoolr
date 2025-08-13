package com.spoolr.core.enums;

/**
 * PaperSize Enum - Standard paper sizes supported by PrintWave
 * 
 * Think of this like the paper size dropdown on any printer:
 * - A4: Most common size worldwide (210×297mm) - Letters, documents
 * - A3: Larger format (297×420mm) - Posters, diagrams  
 * - LETTER: US standard (8.5×11 inches) - US documents
 * - LEGAL: US legal size (8.5×14 inches) - Legal documents
 */
public enum PaperSize {
    A4("A4", "210×297mm", "Standard international letter size"),
    A3("A3", "297×420mm", "Large format for posters and diagrams"),
    LETTER("Letter", "8.5×11 inches", "US standard letter size"),
    LEGAL("Legal", "8.5×14 inches", "US legal document size");
    
    private final String displayName;
    private final String dimensions;
    private final String description;
    
    // Constructor to store paper size details
    PaperSize(String displayName, String dimensions, String description) {
        this.displayName = displayName;
        this.dimensions = dimensions;
        this.description = description;
    }
    
    // Getter methods
    public String getDisplayName() {
        return displayName;
    }
    
    public String getDimensions() {
        return dimensions;
    }
    
    public String getDescription() {
        return description;
    }
    
    // Helper methods for business logic
    public boolean isLargeFormat() {
        return this == A3;
    }
    
    public boolean isUSFormat() {
        return this == LETTER || this == LEGAL;
    }
    
    public boolean isInternationalFormat() {
        return this == A4 || this == A3;
    }
    
    // Helper method to get paper size from string (useful for API requests)
    public static PaperSize fromString(String paperSize) {
        for (PaperSize size : PaperSize.values()) {
            if (size.displayName.equalsIgnoreCase(paperSize) || 
                size.name().equalsIgnoreCase(paperSize)) {
                return size;
            }
        }
        throw new IllegalArgumentException("Invalid paper size: " + paperSize);
    }
}
