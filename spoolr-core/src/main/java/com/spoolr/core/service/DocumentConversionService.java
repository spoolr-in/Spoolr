package com.spoolr.core.service;

import com.spoolr.core.enums.FileType;
import lombok.extern.slf4j.Slf4j;
import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.pdfbox.pdmodel.PDPage;
import org.apache.pdfbox.pdmodel.PDPageContentStream;
import org.apache.pdfbox.pdmodel.common.PDRectangle;
import org.apache.pdfbox.pdmodel.graphics.image.PDImageXObject;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.concurrent.TimeUnit;

/**
 * Service for converting various document formats to PDF
 * Supports: DOCX, DOC, XLSX, PPTX, JPG, PNG
 * Uses LibreOffice for Office formats and PDFBox for images
 */
@Service
@Slf4j
public class DocumentConversionService {

    @Value("${conversion.libreoffice.path.windows:C:/Program Files/LibreOffice/program/soffice.exe}")
    private String libreOfficeWindowsPath;

    @Value("${conversion.libreoffice.path.linux:/usr/bin/soffice}")
    private String libreOfficeLinuxPath;

    @Value("${conversion.libreoffice.timeout:30000}")
    private long conversionTimeoutMs;

    /**
     * Convert document to PDF based on file type
     * Returns PDF bytes or null if conversion fails
     */
    public byte[] convertToPdf(byte[] documentBytes, FileType fileType, String originalFileName) {
        log.info("Converting {} file to PDF: {}", fileType, originalFileName);

        try {
            if (fileType.isImage()) {
                return convertImageToPdf(documentBytes, fileType);
            } else if (fileType.requiresConversion() || fileType == FileType.DOCX) {
                return convertOfficeToPdf(documentBytes, fileType, originalFileName);
            } else {
                log.warn("File type {} does not require conversion", fileType);
                return documentBytes; // Already PDF
            }
        } catch (Exception e) {
            log.error("Failed to convert {} to PDF: {}", fileType, e.getMessage(), e);
            return null;
        }
    }

    /**
     * Convert image (JPG/PNG) to PDF using Apache PDFBox
     */
    private byte[] convertImageToPdf(byte[] imageBytes, FileType fileType) throws IOException {
        log.debug("Converting image to PDF using PDFBox");

        try (PDDocument document = new PDDocument();
             ByteArrayInputStream imageStream = new ByteArrayInputStream(imageBytes);
             ByteArrayOutputStream outputStream = new ByteArrayOutputStream()) {

            // Read image
            BufferedImage bufferedImage = ImageIO.read(imageStream);
            if (bufferedImage == null) {
                throw new IOException("Failed to read image data");
            }

            // Create PDF page matching image size
            float imageWidth = bufferedImage.getWidth();
            float imageHeight = bufferedImage.getHeight();

            // Convert pixels to points (72 points = 1 inch, typically 96 pixels = 1 inch)
            float pdfWidth = (imageWidth / 96f) * 72f;
            float pdfHeight = (imageHeight / 96f) * 72f;

            // Limit maximum size to A3 (842 x 1191 points)
            float maxWidth = 842f;
            float maxHeight = 1191f;

            if (pdfWidth > maxWidth || pdfHeight > maxHeight) {
                float scale = Math.min(maxWidth / pdfWidth, maxHeight / pdfHeight);
                pdfWidth *= scale;
                pdfHeight *= scale;
            }

            // Create page with image dimensions
            PDPage page = new PDPage(new PDRectangle(pdfWidth, pdfHeight));
            document.addPage(page);

            // Save image temporarily to load into PDFBox
            Path tempImageFile = Files.createTempFile("spoolr_image_", getImageExtension(fileType));
            try {
                Files.write(tempImageFile, imageBytes);

                // Add image to page
                PDImageXObject pdImage = PDImageXObject.createFromFile(tempImageFile.toString(), document);
                try (PDPageContentStream contentStream = new PDPageContentStream(document, page)) {
                    contentStream.drawImage(pdImage, 0, 0, pdfWidth, pdfHeight);
                }

                // Save PDF to byte array
                document.save(outputStream);
                log.info("Image converted to PDF successfully: {} bytes", outputStream.size());
                return outputStream.toByteArray();

            } finally {
                // Cleanup temp file
                Files.deleteIfExists(tempImageFile);
            }
        }
    }

    /**
     * Convert Office documents (DOCX, XLSX, PPTX) to PDF using LibreOffice
     */
    private byte[] convertOfficeToPdf(byte[] documentBytes, FileType fileType, String originalFileName) throws IOException, InterruptedException {
        log.debug("Converting Office document to PDF using LibreOffice");

        // Detect LibreOffice executable path
        String libreOfficePath = detectLibreOfficePath();
        if (libreOfficePath == null) {
            throw new IOException("LibreOffice not found. Please install LibreOffice.");
        }

        log.info("Using LibreOffice at: {}", libreOfficePath);

        // Create temp directory for conversion
        Path tempDir = Files.createTempDirectory("spoolr_conversion_");
        Path inputFile = tempDir.resolve("input" + fileType.getFileExtension());
        Path outputPdf = tempDir.resolve("input.pdf");

        try {
            // Write input file
            Files.write(inputFile, documentBytes);

            // Build LibreOffice command
            ProcessBuilder processBuilder = new ProcessBuilder(
                    libreOfficePath,
                    "--headless",
                    "--convert-to", "pdf",
                    "--outdir", tempDir.toString(),
                    inputFile.toString()
            );

            // Set HOME environment variable for LibreOffice user profile
            processBuilder.environment().put("HOME", "/tmp");

            // Execute conversion
            Process process = processBuilder.start();

            // Read stdout for logging
            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            
            try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
                 BufferedReader errorReader = new BufferedReader(new InputStreamReader(process.getErrorStream()))) {
                String line;
                while ((line = reader.readLine()) != null) {
                    stdout.append(line).append("\n");
                    log.debug("LibreOffice stdout: {}", line);
                }
                while ((line = errorReader.readLine()) != null) {
                    stderr.append(line).append("\n");
                    log.error("LibreOffice stderr: {}", line);
                }
            }

            // Wait for completion with timeout
            boolean finished = process.waitFor(conversionTimeoutMs, TimeUnit.MILLISECONDS);

            if (!finished) {
                process.destroyForcibly();
                throw new IOException("LibreOffice conversion timed out after " + conversionTimeoutMs + "ms");
            }

            int exitCode = process.exitValue();
            if (exitCode != 0) {
                String errorMsg = "LibreOffice conversion failed with exit code: " + exitCode;
                if (stderr.length() > 0) {
                    errorMsg += "\nStderr: " + stderr.toString();
                }
                if (stdout.length() > 0) {
                    errorMsg += "\nStdout: " + stdout.toString();
                }
                throw new IOException(errorMsg);
            }

            // Read converted PDF
            if (!Files.exists(outputPdf)) {
                throw new IOException("LibreOffice did not produce output PDF file");
            }

            byte[] pdfBytes = Files.readAllBytes(outputPdf);
            log.info("Office document converted to PDF successfully: {} bytes", pdfBytes.length);
            return pdfBytes;

        } finally {
            // Cleanup temp files
            try {
                Files.deleteIfExists(inputFile);
                Files.deleteIfExists(outputPdf);
                Files.deleteIfExists(tempDir);
            } catch (IOException e) {
                log.warn("Failed to cleanup temp files: {}", e.getMessage());
            }
        }
    }

    /**
     * Detect LibreOffice installation path based on operating system
     */
    private String detectLibreOfficePath() {
        // Try Linux path first (Docker)
        File linuxPath = new File(libreOfficeLinuxPath);
        if (linuxPath.exists() && linuxPath.canExecute()) {
            return libreOfficeLinuxPath;
        }

        // Try Windows path (Local development)
        File windowsPath = new File(libreOfficeWindowsPath);
        if (windowsPath.exists() && windowsPath.canExecute()) {
            return libreOfficeWindowsPath;
        }

        log.error("LibreOffice not found at Linux path: {} or Windows path: {}", 
                libreOfficeLinuxPath, libreOfficeWindowsPath);
        return null;
    }

    /**
     * Get file extension for image type
     */
    private String getImageExtension(FileType fileType) {
        return switch (fileType) {
            case JPG -> ".jpg";
            case PNG -> ".png";
            default -> ".img";
        };
    }

    /**
     * Check if LibreOffice is available
     */
    public boolean isLibreOfficeAvailable() {
        return detectLibreOfficePath() != null;
    }
}
