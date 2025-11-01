# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

**Spoolr** (formerly PrintWave) is a cloud-connected print automation platform - an "Uber for Printing" service that connects customers with local print vendors. The platform consists of three core components:

1. **spoolr-core**: Spring Boot 3.5.3 backend (Java 21) with RESTful APIs, WebSocket support, and real-time job matching
2. **spoolr-station**: WPF desktop application (.NET 9.0) for print vendors with automated printing and document preview
3. **spoolr-frontend**: Next.js customer portal (excluded from this documentation)

**Critical Branding Note**: Use "Spoolr" for all user-facing content, but keep technical infrastructure names (API endpoints, database names, environment variables) unchanged as "printwave".

## Commands

### Backend (spoolr-core)

**Development:**
```powershell
# Navigate to core directory
cd spoolr-core

# Load environment variables (Windows)
.\setLocalEnv.bat

# Run with Maven
mvn spring-boot:run

# Build
mvn clean install

# Run tests
mvn test
```

**Docker Deployment:**
```powershell
cd spoolr-core

# Start all services (PostgreSQL, MinIO, Spring Boot)
docker compose up -d

# View logs
docker compose logs -f spoolr-core

# Stop services
docker compose down

# Rebuild after code changes
docker compose up -d --build
```

**Service URLs (Development):**
- Backend API: `http://localhost:8080`
- PostgreSQL: `localhost:5433`
- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001` (admin/password)

### Station App (spoolr-station)

**Development:**
```powershell
# Navigate to station directory
cd spoolr-station\SpoolrStation

# Run application
dotnet run

# Build
dotnet build

# Run with console output (for debugging)
cd ..\..
.\run_spoolr_with_console.bat
```

## Architecture

### System Flow

```
Customer (Portal) → Core API (Job Creation) → Vendor Matching Algorithm
                                             ↓
                    WebSocket ← Job Offer → Station App (Vendor)
                                             ↓
                                    Accept/Print/Complete
                                             ↓
                         Real-time Status Updates → Customer
```

### Core Backend Architecture (spoolr-core)

**Package Structure:**
- `com.spoolr.core.controller`: REST endpoints for users, vendors, print jobs, WebSocket
- `com.spoolr.core.service`: Business logic (UserService, VendorService, PrintJobService, NotificationService, FileStorageService, EmailService)
- `com.spoolr.core.entity`: JPA entities (User, Vendor, PrintJob)
- `com.spoolr.core.repository`: Spring Data JPA repositories
- `com.spoolr.core.security`: JWT authentication, Spring Security configuration
- `com.spoolr.core.config`: Application configuration (WebSocket, CORS, Task scheduling)
- `com.spoolr.core.dto`: Data transfer objects for API requests/responses
- `com.spoolr.core.enums`: Job status, user roles, paper sizes

**Key Technologies:**
- **Authentication**: JWT tokens with 24-hour expiration
- **File Storage**: MinIO (S3-compatible) with presigned URLs
- **Real-time**: WebSocket with STOMP protocol
- **Email**: Spring Mail with Gmail SMTP
- **Database**: PostgreSQL 15 with JPA/Hibernate
- **Document Processing**: Apache PDFBox, Apache POI

**Critical Features:**
1. **Automatic Status Progression**: Jobs auto-progress from PRINTING → READY (based on calculated time) → COMPLETED (after 24h)
2. **Dual Notifications**: WebSocket + Email for all status changes
3. **Geographic Matching**: Vendors matched within 20km radius using lat/long coordinates
4. **Job Assignment**: Sequential vendor offers with 90-second timeout windows

### Station App Architecture (spoolr-station)

**Technology Stack:**
- **Framework**: WPF (.NET 9.0-windows)
- **Document Preview**: PdfiumViewer for PDFs, DocumentFormat.OpenXml for DOCX, WebView2 for HTML rendering
- **Printer Integration**: System.Management for Windows printer discovery and control
- **WebSocket**: Microsoft.AspNetCore.SignalR.Client for real-time job offers
- **Resilience**: Polly for HTTP retry policies

**Project Structure:**
- `Services/`: WebSocket, authentication, job management, printer services
- `ViewModels/`: MVVM pattern view models
- `Views/`: XAML UI components
- `Models/`: Data models matching backend DTOs
- `Controls/`: Custom WPF controls
- `WebSocket/`: Real-time communication handlers
- `Configuration/`: App settings and configuration

**Key Features:**
1. **Real-time Job Offers**: Persistent WebSocket connection with 90-second countdown timers
2. **Document Preview**: Native rendering without external apps (PDF, DOCX, images)
3. **Automated Printing**: One-click printing with customer settings pre-applied
4. **Printer Discovery**: Automatic detection of Windows printers and capability extraction
5. **Stream-First**: Documents streamed directly from MinIO without local storage

## Development Workflow

### Making Backend Changes

1. **Entity Changes**: Update JPA entity → Repository → Service → Controller
2. **API Endpoints**: Add controller method → Update corresponding service → Test with curl/Postman
3. **Database**: DDL_AUTO=update in development (changes auto-applied), use migrations for production
4. **WebSocket**: Modify NotificationService for real-time updates

### Making Station App Changes

1. **UI Changes**: Edit XAML in Views/ → Update ViewModel binding
2. **Business Logic**: Modify Services/ → Update ViewModels → UI reflects changes via data binding
3. **WebSocket**: Modify WebSocket/ handlers → Test with backend running

### Testing API Endpoints

**Authentication:**
```powershell
# Register customer
curl -X POST http://localhost:8080/api/users/register -H "Content-Type: application/json" -d '{"name":"Test User","email":"test@example.com","phoneNumber":"1234567890","password":"password123"}'

# Login
curl -X POST http://localhost:8080/api/users/login -H "Content-Type: application/json" -d '{"email":"test@example.com","password":"password123"}'

# Vendor login
curl -X POST http://localhost:8080/api/vendors/login -H "Content-Type: application/json" -d '{"storeCode":"PW0001","password":"password"}'
```

**Job Management:**
```powershell
# Create job (authenticated)
curl -X POST http://localhost:8080/api/jobs/upload -H "Authorization: Bearer [TOKEN]" -F "file=@document.pdf" -F "paperSize=A4" -F "isColor=false" -F "isDoubleSided=true" -F "copies=2" -F "customerLatitude=19.0760" -F "customerLongitude=72.8777"

# Check job status (public)
curl http://localhost:8080/api/jobs/status/PJ123456

# Get vendor queue
curl http://localhost:8080/api/jobs/queue -H "Authorization: Bearer [VENDOR_TOKEN]"
```

### Environment Configuration

**Required Environment Variables (.env in spoolr-core):**
```
# Database
POSTGRES_DB=spoolr_db
POSTGRES_USER=spoolr_user
POSTGRES_PASSWORD=your_password
DB_URL=jdbc:postgresql://postgres:5432/spoolr_db
DB_USERNAME=spoolr_user
DB_PASSWORD=your_password

# MinIO
MINIO_ROOT_USER=admin
MINIO_ROOT_PASSWORD=your_password
MINIO_ACCESS_KEY=admin
MINIO_SECRET_KEY=your_password
MINIO_BUCKET_NAME=spoolr-documents

# JWT
JWT_SECRET=your_secure_jwt_secret_key

# Email
EMAIL_USERNAME=your_email@gmail.com
EMAIL_PASSWORD=your_app_password

# App
SERVER_PORT=8080
DDL_AUTO=update
SHOW_SQL=true
SPRING_PROFILES_ACTIVE=dev
```

## Important Notes

### Geographic Constraints
- Vendors are matched within **20km radius** of customer location
- Always provide realistic coordinates near vendor locations when testing
- Example: Use Bangalore coordinates (12.9716, 77.5946) for vendors registered in Bangalore

### Job Status Lifecycle
```
UPLOADED → PROCESSING → AWAITING_ACCEPTANCE (90s timeout)
    ↓                        ↓
ACCEPTED → PRINTING (auto-calculated time) → READY (auto-progress) → COMPLETED (24h auto)
    ↓           ↓                ↓
VENDOR_REJECTED  CANCELLED  NO_VENDORS_AVAILABLE
```

### WebSocket Channels
- **Job Offers** (Private): `/queue/job-offers-{vendorId}` - Station app subscribes
- **Status Updates** (Public): `/topic/job-status/{trackingCode}` - Customer portal subscribes

### File Storage
- Documents stored in MinIO with presigned URLs (30-minute expiration)
- Files auto-deleted after job completion
- Max file size: 50MB

### Email Notifications
All status changes trigger both WebSocket AND email notifications:
- Job Accepted
- Printing Started (with estimated time)
- Ready for Pickup (critical notification)
- Job Completed

### Testing Credentials (Development)
**Test User:**
- Email: `atharvawakodikar699@gmail.com`
- Password: `password123`

**Test Vendor:**
- Store Code: `PW0002`
- Password: `Atharva@699`

## Common Patterns

### Adding a New API Endpoint

1. Define DTO in `dto/` package
2. Add service method in appropriate service class
3. Create controller endpoint with proper annotations
4. Add security configuration if needed
5. Test with curl/Postman
6. Update PROJECT_DESCRIPTION.md if customer/vendor-facing

### Adding WebSocket Notification

1. Use `NotificationService.sendWebSocketNotification(trackingCode, status, message)`
2. Email automatically sent if job has customer email
3. Both real-time and persistent notifications handled

### Modifying Job Status

1. Update status in `PrintJobService`
2. Call `notificationService.notifyCustomerStatus(job, "STATUS", "message")`
3. WebSocket + Email sent automatically
4. Check if auto-progression timers need scheduling

## Documentation References

- **Full API Documentation**: See `PROJECT_DESCRIPTION.md` in repository root
- **Backend Details**: `spoolr-core/README.md`
- **Station App Vision**: `spoolr-station/STATION_APP_OVERVIEW.md`
- **Production Deployment**: `spoolr-core/PRODUCTION_DEPLOYMENT.md`
