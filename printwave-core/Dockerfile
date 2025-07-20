# PrintWave Core - Docker Configuration
# Multi-stage build for optimal image size and performance

# Stage 1: Build stage using Maven with Java 21
FROM maven:3.9.4-eclipse-temurin-21 AS build
WORKDIR /app

# Copy Maven configuration
COPY pom.xml .

# Download dependencies (cached if pom.xml unchanged)
RUN mvn dependency:go-offline -B

# Copy source code
COPY src ./src

# Build the application (skip tests for faster builds)
RUN mvn clean package -DskipTests

# Stage 2: Runtime stage with Java 21 Temurin JRE
FROM eclipse-temurin:21-jre-jammy

# Set working directory
WORKDIR /app

# Create non-root user for security
RUN groupadd -r printwave && useradd -r -g printwave printwave

# Copy built JAR from build stage (specific name from pom.xml)
COPY --from=build /app/target/printwave-core-0.0.1-SNAPSHOT.jar app.jar

# Change ownership to non-root user
RUN chown printwave:printwave app.jar

# Switch to non-root user
USER printwave

# Expose application port
EXPOSE 8080

# Health check endpoint (Spring Boot Actuator)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/actuator/health || exit 1

# JVM optimization for containerized environments
ENTRYPOINT ["java", \
    "-XX:+UseContainerSupport", \
    "-XX:MaxRAMPercentage=75.0", \
    "-XX:+UseG1GC", \
    "-XX:+UseStringDeduplication", \
    "-jar", \
    "app.jar"]

# Alternative simple entrypoint (uncomment if issues with optimizations)
# ENTRYPOINT ["java", "-jar", "app.jar"]
