package com.printwave.core.config;

import io.minio.MinioClient;
import lombok.Getter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * MinIOConfig - Configuration class for MinIO object storage

 * Think of this as the "connection settings" for our cloud storage.
 * It tells Spring Boot how to connect to MinIO (which is like a digital filing cabinet).

 * MinIO is an S3-compatible storage service that stores files in "buckets"
 * - Bucket = Like a folder that holds all our documents
 * - Object = Individual files (PDFs, images, etc.)
 * - Key = The path/name of the file in the bucket
 */
@Configuration
public class MinIOConfig {

    /**
     * -- GETTER --
     *  Helper method to get endpoint for generating file URLs
     */
    // These values come from environment variables (docker-compose.yml)
    @Getter
    @Value("${MINIO_ENDPOINT:http://localhost:9000}")
    private String endpoint;
    
    @Value("${MINIO_ACCESS_KEY:spoolr_admin}")
    private String accessKey;
    
    @Value("${MINIO_SECRET_KEY:spoolr_minioadmin@2025}")
    private String secretKey;
    
    /**
     * Create MinioClient bean for dependency injection

     * This creates a "client" object that our application can use
     * to talk to MinIO storage (upload files, download files, etc.)

     * Bean annotation means Spring will manage this object and
     * inject it wherever we need it (like in FileStorageService)
     */
    @Bean
    public MinioClient minioClient() {
        return MinioClient.builder()
                .endpoint(endpoint)
                .credentials(accessKey, secretKey)
                .build();
    }
    
    /**
     * Bucket name configuration
     * This is the name of the "folder" where we store all print documents
     * -- GETTER --
     *  Helper method to get bucket name

     */
    @Getter
    @Value("${MINIO_BUCKET_NAME:spoolr-documents}")
    private String bucketName;
    
    @Bean
    public String minioBucketName() {
        return bucketName;
    }

}
