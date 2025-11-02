package com.spoolr.core.util;

import org.springframework.web.multipart.MultipartFile;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;

/**
 * Wrapper class for converted PDF bytes to implement MultipartFile interface
 * Used when converting DOCX/XLSX/PPTX/JPG/PNG to PDF
 */
public class ConvertedPdfFile implements MultipartFile {

    private final byte[] pdfBytes;
    private final String pdfFileName;
    private final String originalFileName;

    public ConvertedPdfFile(byte[] pdfBytes, String pdfFileName, String originalFileName) {
        this.pdfBytes = pdfBytes;
        this.pdfFileName = pdfFileName;
        this.originalFileName = originalFileName;
    }

    @Override
    public String getName() {
        return "file";
    }

    @Override
    public String getOriginalFilename() {
        return pdfFileName;
    }

    @Override
    public String getContentType() {
        return "application/pdf";
    }

    @Override
    public boolean isEmpty() {
        return pdfBytes == null || pdfBytes.length == 0;
    }

    @Override
    public long getSize() {
        return pdfBytes.length;
    }

    @Override
    public byte[] getBytes() throws IOException {
        return pdfBytes;
    }

    @Override
    public InputStream getInputStream() throws IOException {
        return new ByteArrayInputStream(pdfBytes);
    }

    @Override
    public void transferTo(File dest) throws IOException, IllegalStateException {
        try (FileOutputStream fos = new FileOutputStream(dest)) {
            fos.write(pdfBytes);
        }
    }
}
