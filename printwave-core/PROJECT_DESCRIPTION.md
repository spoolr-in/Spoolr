# PrintWave Core - Project Description

## Overview
PrintWave is a print service management platform that connects customers with local print vendors through a distributed system. The core application manages vendor registration, customer orders, and print job coordination.

## System Architecture
- **Core App**: Backend service handling vendor registration, user management, and business logic
- **Portal App**: Customer-facing web application for placing print orders
- **Station App**: Vendor-side application for printer management and auto-discovery

## Application Components

### 1. Core App (Backend API)
**Technology**: Spring Boot, PostgreSQL, JWT Authentication
**Port**: 8080
**Responsibilities**:
- User management (registration, login, JWT authentication)
- Vendor management (business registration, activation keys)
- Print job processing and routing
- File storage and document management
- Payment processing coordination
- Real-time job status updates
- Distance-based vendor matching
- Email notifications (verification, activation)

**Key APIs**:
```java
// User Management
POST /api/users/register
POST /api/users/login
GET /api/users/profile (JWT protected)
POST /api/users/request-password-reset
GET /api/users/reset-password
POST /api/users/reset-password
GET /api/users/dashboard (JWT protected)

// Vendor Management
POST /api/vendors/register
GET /api/vendors/verify-email
POST /api/vendors/station-login (legacy)
POST /api/vendors/first-time-login (NEW - enhanced auth)
POST /api/vendors/login (NEW - store code + password)
POST /api/vendors/change-password (NEW)
POST /api/vendors/reset-password (NEW)
POST /api/vendors/{id}/toggle-store
POST /api/vendors/{id}/update-capabilities

// Print Jobs - Online (Registered Users Only)
POST /api/jobs/upload (JWT required)
GET /api/jobs/history (JWT required)

// Print Jobs - QR Code Anonymous
GET /store/{storeCode}
POST /api/jobs/qr-anonymous-upload
GET /api/jobs/status/{trackingCode}

// Vendor Operations
POST /api/vendors/toggle-store
GET /api/vendors/job-queue
POST /api/jobs/accept
POST /api/jobs/complete
```

### 2. Portal App (Frontend)
**Technology**: React/Vue.js/Angular + Maps Integration
**URL**: https://printwave.com
**Responsibilities**:
- Customer registration and login interface
- Vendor business registration forms
- Document upload and print options selection
- Store location and selection (with maps)
- Payment processing interface
- Order tracking and history
- QR code landing pages
- Responsive design for mobile/desktop

**Key Pages**:
```javascript
// Customer Pages
/login                    // Customer login
/register                 // Customer registration
/dashboard                // Customer dashboard
/upload                   // Document upload (logged in)
/orders                   // Order history
/store/{storeCode}        // QR code landing page

// Vendor Pages
/vendor/register          // Vendor registration
/vendor/dashboard         // Vendor management
/vendor/profile           // Business profile

// Anonymous Pages
/                         // Homepage
/about                    // About page
/contact                  // Contact page
```

### 3. Station App (Vendor Desktop App)
**Technology**: Electron/Java Desktop App
**Responsibilities**:
- Vendor authentication via activation key
- Printer auto-discovery and capability detection
- Real-time job queue management
- Store open/close toggle
- Job acceptance and completion
- Print job processing and status updates
- Local printer communication
- Sync printer capabilities to Core API

**Key Features**:
```javascript
// Authentication
- Login with activation key
- Secure connection to Core API

// Printer Management
- Auto-detect connected printers
- Report printer capabilities (paper sizes, color, duplex)
- Monitor printer status (online/offline)
- Send capabilities to Core API

// Job Management
- Real-time job queue display
- Job details view (document, options, price)
- Accept/reject job functionality
- Print job execution
- Mark jobs as completed

// Store Management
- Open/close store toggle
  - Field: `isStoreOpen` (true/false)
  - Last toggled: `storeStatusUpdatedAt` (timestamp)
- Store status synchronization
- Earnings and statistics display
```

## Complete User Workflow ("Uber for Printing")

### Phase 1: User Management (COMPLETE)
- User registration, login, email verification
- Password reset functionality
- JWT authentication and security
- Protected user endpoints (profile, dashboard)

### Phase 2: Vendor Management (COMPLETE)
- Vendor registration with business details
- Two-step verification (email → activation key)
- Station app authentication
- Store management (open/close toggle)
- Printer capabilities management
- Location-based vendor setup

### Phase 3: Print Job Management (NEXT)
- Print job entity and repository creation
- Document management system
- Key APIs for job upload, history, and tracking
- Job matching algorithm

### Phase 4: Integration Features
- Payment coordination
- Real-time messaging
- Station app communication protocols
- Performance optimization

## 🐳 Docker Setup for PrintWave

### 📁 File Structure
```
printwave-core/
├── src/                          # Your Java source code
│   └── main/java/com/printwave/core/
├── target/                       # Built JAR files (after mvn package)
│   └── printwave-core.jar
├── Dockerfile                    # 🆕 Instructions to build PrintWave Core image
├── docker-compose.yml            # 🆕 Orchestrates all services
├── .dockerignore                 # 🆕 Files to ignore when building Docker image
├── .env                         # Environment variables (existing)
├── pom.xml                      # Maven configuration (existing)
└── README.md                    # Project documentation (existing)
```

### 🏗️ Complete Docker Architecture

### Our 3 Services:

```
┌─────────────────────────────────────────────────────────────┐
│                    DOCKER COMPOSE                          │
├─────────────────────────────────────────────────────────────┤
│  Service 1: printwave-core                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  🏗️  Built from OUR Dockerfile                        │  │
│  │  📦  Spring Boot App (Java 17)                       │  │
│  │  🌐  Port: 8080                                      │  │
│  │  🔗  Connects to: postgres + minio                   │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  Service 2: postgres                                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  📦  Official PostgreSQL Image (no Dockerfile)       │  │
│  │  🗄️   Database Storage                                │  │
│  │  🌐  Port: 5432                                      │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  Service 3: minio                                           │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  📦  Official MinIO Image (no Dockerfile)            │  │
│  │  ☁️   File Storage (S3-like)                         │  │
│  │  🌐  API Port: 9000, Console: 9001                  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 📄 File Contents

#### 1. **Dockerfile** (for PrintWave Core only)
```dockerfile
# Multi-stage build
FROM maven:3.9.4-openjdk-17 AS build
WORKDIR /app
COPY pom.xml .
COPY src ./src
RUN mvn clean package -DskipTests

FROM openjdk:17-jdk-slim
WORKDIR /app
COPY --from=build /app/target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
```

#### 2. **docker-compose.yml** (orchestrates all services)
```yaml
version: '3.8'

services:
  # 🗄️ Database Service (uses official image)
  postgres:
    image: postgres:15
    container_name: printwave-postgres
    environment:
      POSTGRES_DB: printwave_db
      POSTGRES_USER: printwave_user
      POSTGRES_PASSWORD: printwave_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  # ☁️ File Storage Service (uses official image)
  minio:
    image: minio/minio:latest
    container_name: printwave-minio
    environment:
      MINIO_ACCESS_KEY: printwave-access-key
      MINIO_SECRET_KEY: printwave-secret-key
    ports:
      - "9000:9000"    # API
      - "9001:9001"    # Web Console
    volumes:
      - minio_data:/data
    command: server /data --console-address ":9001"

  # 🚀 Our Spring Boot App (uses our Dockerfile)
  printwave-core:
    build: .                    # 👈 This uses our Dockerfile
    container_name: printwave-core
    environment:
      # Database connection
      DB_URL: jdbc:postgresql://postgres:5432/printwave_db
      DB_USERNAME: printwave_user
      DB_PASSWORD: printwave_password
      
      # MinIO connection
      MINIO_URL: http://minio:9000
      MINIO_ACCESS_KEY: printwave-access-key
      MINIO_SECRET_KEY: printwave-secret-key
    ports:
      - "8080:8080"
    depends_on:
      - postgres
      - minio

# 💾 Data persistence
volumes:
  postgres_data:
  minio_data:
```

#### 3. **.dockerignore** (what NOT to include in Docker image)
```
target/
*.log
.git/
.env
README.md
docker-compose.yml
Dockerfile
```

### 🔄 How It All Works Together

#### Step 1: Build Process
```bash
docker-compose up --build
```
1. **Docker Compose reads docker-compose.yml**
2. **For postgres**: Downloads official PostgreSQL image
3. **For minio**: Downloads official MinIO image  
4. **For printwave-core**: Builds custom image using our Dockerfile
5. **Starts all services** and connects them

#### Step 2: Network Communication
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ Spring Boot │───▶│ PostgreSQL  │    │    MinIO    │
│   :8080     │    │    :5432    │    │ :9000/:9001 │
│             │    │             │    │             │
│ printwave-  │◀───┤ postgres    │    │   minio     │
│    core     │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘
```
#### Step 3: Access Points
- **Spring Boot API**: http://localhost:8080
- **MinIO Console**: http://localhost:9001 (web interface)
- **PostgreSQL**: localhost:5432 (via database tools like DBeaver)

### 🎯 What We Need vs What's Ready

#### ✅ Ready to Use (Official Images)
- **PostgreSQL**: Just configure and run
- **MinIO**: Just configure and run

#### 🛠️ Docker Setup Status
- [x] **Dockerfile**: ✅ CREATED - Multi-stage build with Java 21 Temurin
- [ ] **docker-compose.yml**: To orchestrate all services
- [ ] **.dockerignore**: To optimize build performance

### 🚀 Development Workflow
```bash
# 1. Start all services
docker-compose up -d

# 2. Check logs
docker-compose logs printwave-core

# 3. Access services
# - Spring Boot: http://localhost:8080
# - MinIO Console: http://localhost:9001

# 4. Stop all services
docker-compose down
```

This setup gives us:
- ✅ **Consistent environment** across all machines
- ✅ **Easy setup** for new team members
- ✅ **Production-ready** deployment structure
- ✅ **Isolated services** that can scale independently

_Refer to the PROJECT_DESCRIPTION.md for further details on setting up and managing the Docker environment._


### 1. Registered Customer Flow (Online Portal)
**Requirements**: Must create account and login
**Payment**: Online payment required
**Risk**: Low (customer details + pre-payment)

```
🌐 Customer visits printwave.com
🔐 Must login or create account (no anonymous option)
📄 Uploads document (PDF/Word/Image)
📍 System detects location or customer enters address
🏪 Shows nearby stores with capabilities and ratings
🎯 Selects preferred store
⚙️ Configures print options (paper size, color, sides, copies)
💰 Sees calculated price
💳 Pays online with saved payment method
📧 Receives email confirmation with tracking info
📱 Gets push notification when job is ready
🚗 Drives to store
📱 Shows order confirmation to staff
📄 Collects printed documents
⭐ Rates store experience
📊 Order saved to customer history
```

### 2. Anonymous Customer Flow (QR Code Only)
**Requirements**: Must be physically present at store
**Payment**: Pay at store (cash/card)
**Risk**: Low (customer physically present)

```
🏪 Customer physically at ABC Print Shop
📱 Scans QR code on store counter
🌐 Opens: https://printwave.com/store/PW0001
📝 Sees options: [Sign In] [Create Account] [Continue Without Account]
📱 Clicks "Continue Without Account"
📄 Uploads document
⚙️ Selects print options
💰 Sees price calculation
🎫 Receives tracking code: "PJ123456"
📋 Job instantly appears in store's Station app
⏱️ Waits 5-10 minutes while document prints
🎫 Shows tracking code "PJ123456" to staff
💳 Pays at store (cash/card)
📄 Collects printed document
✅ Complete (no account needed)
```

### 3. Vendor Registration & Setup Flow
**Platform**: Portal web application
**Process**: Two-step verification system

```
🏪 Print shop owner visits printwave.com/vendor/register
📝 Fills business registration form:
   - Business name, contact person, phone
   - Full address and location coordinates
   - Pricing structure (B&W/Color, Single/Double sided)
   - Business hours and policies
📧 Submits form → receives email verification
✅ Clicks email verification link
📧 Receives activation email containing:
   - Activation key: "PW-ABC123-XYZ789"
   - Station app download link
   - Setup instructions
💻 Downloads Station app for desktop
🔑 Enters activation key in Station app
📱 Station app connects to Core API
🖨️ Station app auto-discovers connected printers
📋 Reports printer capabilities to Core:
   - Paper sizes (A4, A3, Letter, Legal)
   - Color support (Yes/No)
   - Duplex capability (Yes/No)
   - Print speeds and specifications
💰 Confirms pricing structure
🔘 Clicks "Open Store" to start receiving jobs
📋 Store appears in customer searches
✅ Ready to receive print jobs
```

### 4. Daily Vendor Operations Flow
**Platform**: Station desktop application
**Process**: Real-time job management

```
🌅 Vendor arrives at shop
💻 Opens Station app on desktop
🔑 Logs in (stays logged in)
🔘 Clicks "Open Store" button
📡 App connects to Core API
📋 Job queue shows: "No pending jobs"

⏰ 10:30 AM - New job notification!
📱 Notification popup: "New job received: PJ123456"
📋 Job details display:
   - Customer: Anonymous (QR code) or "John Smith" (registered)
   - Document: "Report.pdf" (3 pages)
   - Options: A4, Color, Double-sided
   - Price: $2.50
   - Estimated time: 5 minutes

🖨️ Vendor reviews job and clicks "Accept"
📄 Document automatically sent to selected printer
🖨️ Printer starts printing
📋 Job status updates to "Printing"

⏱️ 5 minutes later - printing complete
📄 Vendor collects printed pages
📋 Clicks "Ready for Pickup"
📱 System notifies customer (if registered)

👤 Customer arrives at store
🎫 Shows tracking code "PJ123456" or order confirmation
💳 Customer pays $2.50 (if anonymous) or shows "Paid Online"
📄 Vendor hands over documents
📋 Clicks "Job Completed"
💰 Earnings updated: +$2.25 (after 10% platform fee)
📊 Daily statistics updated
```

## Customer Strategy: Anonymous vs Registered

### Anonymous Customers (QR Code Only)
**Allowed**: Only when physically present at store
**Reasoning**: Eliminates vendor risk of abandoned print jobs

**Restrictions**:
- ❌ **No Online Anonymous**: Cannot upload from home/office
- ❌ **No Remote Printing**: Must be at physical store location
- ✅ **QR Code Only**: Anonymous option appears only after QR scan
- ✅ **Immediate Pickup**: Customer waits while document prints

**Benefits**:
- 🚀 **Zero Friction**: No signup required for walk-in customers
- 🔒 **Vendor Protection**: Customer is physically present
- 💳 **Pay on Pickup**: No advance payment complexity
- 📱 **Mobile Friendly**: Works on any smartphone camera

### Registered Customers (Full Service)
**Required**: For all online/remote printing
**Reasoning**: Provides accountability and reduces abandonment

**Features**:
- 🌐 **Online Ordering**: Upload documents from anywhere
- 💳 **Pre-payment**: Pay online before printing
- 📧 **Notifications**: Email and push notifications
- 📊 **Order History**: Track all previous jobs
- ⭐ **Reviews**: Rate and review print shops
- 💾 **Saved Preferences**: Store favorite print settings

**Benefits**:
- 📈 **Higher Revenue**: Pre-paid jobs guarantee payment
- 🔒 **Lower Risk**: Customer details and payment secured
- 🎯 **Better Matching**: Personalized store recommendations
- 💌 **Communication**: Direct customer communication channel

### Next APIs to Build

#### Customer APIs:
- POST /api/jobs/upload - Upload document & create job
- GET /api/jobs/history - Customer order history
- GET /api/jobs/status/{trackingCode} - Track job status
- POST /api/jobs/qr-anonymous-upload - QR code uploads

#### Vendor APIs:
- GET /api/vendors/job-queue - Pending jobs for vendor
- POST /api/jobs/accept - Accept a print job
- POST /api/jobs/complete - Mark job as completed
- POST /api/jobs/reject - Reject a job

#### Public APIs:
- GET /store/{storeCode} - QR code landing page
- GET /api/vendors/nearby - Find vendors by location

### API Strategy

**Separate Endpoints for Clear Logic**:
```java
// Anonymous (QR Code Only)
GET /store/{storeCode}                    // QR landing page
POST /api/jobs/qr-anonymous-upload        // Anonymous upload
GET /api/jobs/status/{trackingCode}       // Anonymous tracking

// Registered Users (Full Service)
POST /api/jobs/upload                     // Authenticated upload
GET /api/jobs/history                     // User order history
POST /api/jobs/reorder                    // Reorder previous job
```

**Frontend Flow Control**:
```javascript
// Online Portal: Always require login
if (accessType === 'online') {
    requireAuthentication();
}

// QR Code: Show anonymous option
if (accessType === 'qr_code') {
    showOptions([
        'Sign In',
        'Create Account', 
        'Continue Without Account'
    ]);
}
```
   - Sees print preview with all settings applied
   - Confirms and prints document
   - Updates job status to completed

### Core App Responsibilities:
- **User Management**: Authentication, registration, password reset
- **Vendor Management**: Business registration, activation key generation
- **Job Matching**: Match customers with vendors based on capabilities and location
- **File Storage**: Secure document storage and retrieval
- **Payment Processing**: Handle online payments
- **Real-time Communication**: Status updates between Portal and Station apps
- **Printer Capability Management**: Store and query printer capabilities

## Vendor Registration Flow
1. **Core Registration**: Vendor registers with business details via Core App
2. **Email Activation**: System sends activation key + Station app download link
3. **Station Activation**: Vendor logs into Station app using activation key
4. **Auto-Discovery**: Station app scans and reports printer capabilities to Core

## Current Development Status

### ✅ Completed Features

#### 1. Development Environment Setup
- **Database**: PostgreSQL installed and configured
- **Database Management**: DBeaver setup for visual database administration
- **Project Structure**: Spring Boot application with Maven build system

#### 2. Database Configuration
- **Environment Management**: 
  - `.env` file for sensitive configuration (git-ignored)
  - `.env.example` as template for environment variables
  - `setLocalEnv.sh` script to load environment variables
- **Database Connection**: 
  - PostgreSQL integration via Spring Data JPA
  - Environment-based configuration (no hardcoded values)
  - Successful connection verification

#### 3. Entity Layer
- **User Entity** (`src/main/java/com/printwave/core/entity/User.java`):
  - Fields: `id`, `email`, `name`, `password`, `phoneNumber`, `role`, `emailVerified`, `verificationToken`, `passwordResetToken`, `passwordResetExpiry`, `createdAt`, `updatedAt`
  - JPA annotations: `@Entity`, `@Table`, `@Id`, `@GeneratedValue`
  - Lombok annotations: `@Data` for boilerplate code generation
  - Automatic timestamp management with `@CreationTimestamp` and `@UpdateTimestamp`
  - Password reset functionality with token-based security

- **UserRole Enum** (`src/main/java/com/printwave/core/enums/UserRole.java`):
  - Values: `CUSTOMER`, `VENDOR`, `ADMIN`
  - Used for role-based access control

#### 4. Repository Layer
- **UserRepository** (`src/main/java/com/printwave/core/repository/UserRepository.java`):
  - Extends `JpaRepository<User, Long>`
  - Built-in CRUD operations
  - Custom query methods:
    - `findByEmail()` returns `Optional<User>`
    - `findByRole()` returns `List<User>`
    - `existsByEmail()` returns `boolean`

#### 5. Service Layer
- **UserService** (`src/main/java/com/printwave/core/service/UserService.java`):
  - Business logic for user operations
  - Methods:
    - `registerUser(User user)` - User registration with password hashing
    - `verifyEmail(String token)` - Email verification
    - `loginUser(String email, String password)` - User authentication
    - `requestPasswordReset(String email)` - Generate password reset token
    - `resetPassword(String token, String newPassword)` - Reset password
  - Features:
    - BCrypt password encryption
    - Duplicate email validation
    - Verification token generation
    - Password reset with time-limited tokens
    - Transactional database operations
    - Integrated email notifications

- **EmailService** (`src/main/java/com/printwave/core/service/EmailService.java`):
  - Email notification service using Gmail SMTP
  - Methods:
    - `sendVerificationEmail(User user)` - Send email verification (async)
    - `sendPasswordResetEmail(User user)` - Send password reset email (async)
  - Features:
    - Gmail SMTP integration
    - Professional email templates
    - Secure environment-based configuration
    - Asynchronous email sending for fast API responses
    - Non-blocking background email processing

#### 6. Controller Layer
- **UserController** (`src/main/java/com/printwave/core/controller/UserController.java`):
  - REST API endpoints for user operations
  - Endpoints:
    - `POST /api/users/register` - User registration
    - `GET /api/users/verify?token=xyz` - Email verification
    - `POST /api/users/login` - User authentication
    - `POST /api/users/request-password-reset` - Request password reset
    - `GET /api/users/reset-password?token=xyz` - Password reset HTML form (email links)
    - `POST /api/users/reset-password` - Reset password API (JSON body)
  - Features:
    - RESTful API design
    - Comprehensive error handling
    - JSON request/response format
    - HTTP status code management
    - Secure DTO-based input validation
    - No sensitive data in URL parameters
    - Consistent JSON API structure
    - Dual endpoint pattern for password reset (GET + POST)
    - HTML form generation for email link handling
    - Frontend-ready API design

#### 7. Data Transfer Objects (DTOs)
- **LoginRequest** (`src/main/java/com/printwave/core/dto/LoginRequest.java`):
  - Secure login request structure
  - Fields: `email`, `password`
  - Prevents password exposure in URL parameters

- **PasswordResetEmailRequest** (`src/main/java/com/printwave/core/dto/PasswordResetEmailRequest.java`):
  - Password reset request structure
  - Fields: `email`
  - Consistent JSON API design

- **PasswordResetRequest** (`src/main/java/com/printwave/core/dto/PasswordResetRequest.java`):
  - Password reset completion structure
  - Fields: `token`, `newPassword`
  - Secure password handling without URL exposure

- **ProfileResponse** (`src/main/java/com/printwave/core/dto/ProfileResponse.java`):
  - User profile response structure
  - Fields: `id`, `email`, `name`, `phoneNumber`, `role`, `emailVerified`, `message`
  - Used by protected profile endpoint

- **DashboardResponse** (`src/main/java/com/printwave/core/dto/DashboardResponse.java`):
  - User dashboard response structure
  - Fields: `message`, `email`, `userId`, `details`, `welcomeMessage`, `totalOrders`, `accountStatus`
  - Enhanced with personalized dashboard information

#### 8. JWT Security Implementation
- **JwtUtil** (`src/main/java/com/printwave/core/util/JwtUtil.java`):
  - JWT token generation and validation utility
  - Methods: `generateToken()`, `validateToken()`, `extractEmail()`, `extractRole()`, `extractUserId()`
  - Uses HMAC-SHA256 algorithm with configurable secret and expiration
  - Secure token creation with user claims (email, role, userId)

- **JwtAuthenticationFilter** (`src/main/java/com/printwave/core/security/JwtAuthenticationFilter.java`):
  - Spring Security filter for automatic JWT validation
  - Intercepts requests and validates Authorization header
  - Extracts user information and sets Spring Security context
  - Adds user attributes to request for easy access in controllers

- **SecurityConfig** (`src/main/java/com/printwave/core/security/SecurityConfig.java`):
  - Spring Security configuration for JWT authentication
  - Defines public vs protected endpoints
  - Configures stateless session management
  - Enables method-level security with `@PreAuthorize`
  - CORS configuration for frontend integration

#### 9. Protected Endpoints
- **GET /api/users/profile** - Get current user profile (requires JWT)
  - Returns: `ProfileResponse` with user details
  - Access: `@PreAuthorize("hasRole('CUSTOMER')")` 
  - Features: Secure user data retrieval, excludes sensitive information

- **GET /api/users/dashboard** - User dashboard (requires JWT)
  - Returns: `DashboardResponse` with personalized dashboard data
  - Access: `@PreAuthorize("hasRole('CUSTOMER')")` 
  - Features: Personalized welcome message, account status, order count

#### 10. Testing Infrastructure
- **DatabaseTestRunner** (`src/test/java/com/printwave/core/component/DatabaseTestRunner.java`):
  - `CommandLineRunner` implementation for repository testing
  - Creates sample users and verifies database operations
  - Located in test directory to prevent production execution
  - Validates entity persistence and query functionality

#### 6. Database Schema
- **Users Table**: Automatically created by Hibernate
  - Verified in DBeaver with proper data persistence
  - Proper foreign key relationships ready for expansion

### 🔄 Current Architecture

```
src/
├── main/java/com/printwave/core/
│   ├── PrintwaveCoreApplication.java (Main Spring Boot class with @EnableAsync)
│   ├── controller/
│   │   ├── UserController.java (with protected endpoints)
│   │   └── VendorController.java (✅ NEW - Phase 2)
│   ├── dto/
│   │   ├── ApiResponse.java (✅ NEW - Phase 2)
│   │   ├── ChangePasswordRequest.java (✅ NEW - Session 12)
│   │   ├── DashboardResponse.java
│   │   ├── ErrorResponse.java (✅ NEW - Session 13)
│   │   ├── FirstTimeLoginRequest.java (✅ NEW - Session 12)
│   │   ├── LoginRequest.java
│   │   ├── LoginResponse.java
│   │   ├── PasswordResetEmailRequest.java
│   │   ├── PasswordResetRequest.java
│   │   ├── PrinterCapabilitiesRequest.java (✅ NEW - Phase 2)
│   │   ├── ProfileResponse.java
│   │   ├── ResetPasswordRequest.java (✅ NEW - Session 12)
│   │   ├── StationLoginRequest.java (✅ NEW - Phase 2)
│   │   ├── StoreStatusRequest.java (✅ NEW - Phase 2)
│   │   ├── VendorLoginRequest.java (✅ NEW - Session 12)
│   │   ├── VendorLoginResponse.java (✅ NEW - Phase 2)
│   │   └── VendorRegistrationRequest.java (✅ NEW - Phase 2)
│   ├── entity/
│   │   ├── User.java
│   │   └── Vendor.java (✅ NEW - Phase 2)
│   ├── enums/
│   │   └── UserRole.java
│   ├── repository/
│   │   ├── UserRepository.java
│   │   └── VendorRepository.java (✅ NEW - Phase 2)
│   ├── security/
│   │   ├── JwtAuthenticationFilter.java
│   │   └── SecurityConfig.java
│   ├── service/
│   │   ├── EmailService.java (with @Async methods)
│   │   ├── UserService.java
│   │   └── VendorService.java (✅ NEW - Phase 2)
│   └── util/
│       └── JwtUtil.java
├── test/java/com/printwave/core/
│   ├── PrintwaveCoreApplicationTests.java
│   └── component/
│       └── DatabaseTestRunner.java
└── resources/
    └── application.properties
```

#### Vendor Entity Features (✅ Complete):
- **Business Information**: Name, contact person, phone, address, city, state, zip
- **Location Coordinates**: Latitude, longitude for distance calculations
- **Pricing Structure**: 4 price points (B&W/Color × Single/Double sided)
- **Two-Step Verification**: Email verification → activation key workflow
- **QR Code Support**: Store code generation and URL creation
- **Store Status**: Manual open/close toggle with timestamp tracking
- **Station App Integration**: Connection status and last login tracking
- **Printer Capabilities**: JSON storage for flexible printer information
- **Helper Methods**: Business logic for order readiness and registration status

#### VendorRepository Features (✅ Complete):
- **Email Operations**: `findByEmail()`, `existsByEmail()` for registration and lookup
- **Verification Workflow**: `findByVerificationToken()` for email verification process
- **Station Authentication**: `findByActivationKey()` for Station app login
- **QR Code System**: `findByStoreCode()`, `existsByStoreCode()` for QR code workflow
- **CRUD Operations**: Inherited from JpaRepository (save, findById, findAll, delete)

#### Auto-Generation Strategy (✅ Planned):
- **Store Code**: System-generated unique codes (e.g., "PW0001", "PW0002")
  - Format: "PW" + 4-digit sequential number
  - Ensures uniqueness and professional appearance
  - Used for QR code URLs: `https://printwave.com/store/PW0001`
- **Activation Key**: System-generated secure keys (e.g., "PW-ABC123-XYZ789")
  - Format: "PW-" + 6 random alphanumeric + "-" + 6 random alphanumeric
  - Cryptographically secure for Station app authentication
  - Sent via email after successful email verification

### 🎯 Next Development Steps

## Database Design Strategy

### User Management (Current Focus)
- **User Table**: Represents customers using the Portal
- **Fields**: `id`, `email`, `name`, `password`, `phoneNumber`, `role`, `emailVerified`, `verificationToken`, `passwordResetToken`, `passwordResetExpiry`
- **Authentication**: Email/password login via Portal
- **Role**: Always CUSTOMER (User = Customer for this project)

### Vendor Management (Future)
- **Vendor Table**: Completely separate table for print shop vendors
- **Fields**: `id`, `email`, `password`, `businessName`, `address`, `activationKey`, `printerCapabilities`, `servicePricing`
- **Authentication**: Separate login system for Station app (not Portal)
- **Station App Login**: Uses activation key or separate vendor credentials
- **Independence**: No relationship to User table - vendors are separate entities
- **Reasoning**: Clear separation between customer and vendor workflows

### Print Job Management (Future)
- **PrintJob Table**: Customer print requests with all specifications
- **Document Table**: File metadata and storage references
- **JobStatus Table**: Track job progress through workflow

## Development Plan (Step-by-Step)

### Phase 1: Complete User Layer (COMPLETE ✅)
- [x] Update `User` entity with verification and password reset fields
- [x] Create `UserService` for business logic
- [x] Create `UserController` with REST endpoints
- [x] Implement email service for notifications
- [x] Add secure DTO layer for API requests
- [x] Implement asynchronous email processing
- [x] Test complete user management flow
- [x] Add JWT authentication and security
- [x] Implement JWT security filter and Spring Security configuration
- [x] Add protected user endpoints (profile, dashboard)
- [x] Role-based access control with @PreAuthorize

### Phase 2: Vendor Layer (IN PROGRESS 🔄)

#### Vendor Entity Design (PLANNING)
Vendor registration happens on **Portal (Frontend)**, authentication via **Station App**.

**Vendor Registration Flow:**
1. **Portal Registration**: Vendor fills business details form
2. **Email Verification**: System sends verification email, vendor clicks link
3. **Activation Email**: After verification, system sends activation key + Station app download link
4. **Station App Setup**: Vendor downloads app, logs in with activation key
5. **Printer Discovery**: Station app auto-discovers printers and syncs capabilities

**Portal Registration Fields:**
- Business details (name, contact person, phone, address)
- Location coordinates (latitude, longitude) for distance calculations
- Pricing structure (B&W/Color, Single/Double sided)
- Account management (email, verification token, activation key)

**Vendor Entity Architecture:**
- **No Role Field**: Vendor entity represents print shops only (unlike User entity)
- **Separate Authentication**: Vendors use activation keys, not email/password
- **Different Purpose**: User = Customer authentication, Vendor = Business management
- **Future Tiers**: Can add VendorTier enum later if needed (Standard, Premium, Enterprise)

**Station App Integration:**
- Login using activation key (sent via email)
- Manual store open/close toggle
- Printer capability auto-discovery
- Real-time job queue management

**Location-Based Store Matching:**
- Customer location provided per print job (not stored in User entity)
- Distance calculation: "as the crow flies" initially
- Search radius: 5km → 10km → 20km (if insufficient results)
- Only show stores that are: Open + Have Required Capabilities + Within Range

**Store Selection Methods:**
1. **Manual**: Customer sees filtered list, picks preferred store
2. **Automatic**: Broadcast to all qualifying stores, first to accept gets job

#### Implementation Tasks:
- [x] Create `Vendor` entity with business details (COMPLETE ✅)
  - Business details (name, contact, phone, address, location)
  - Pricing structure (B&W/Color, Single/Double sided)
  - Two-step verification (email verification → activation key)
  - QR code support (store code, URL generation)
  - Store status management (isStoreOpen, storeStatusUpdatedAt)
  - Station app integration (connection status)
  - Printer capabilities (JSON storage)
  - Helper methods (isReadyForOrders, isRegistrationComplete)
- [x] Create `VendorRepository` for database operations (COMPLETE ✅)
  - Email-based queries (findByEmail, existsByEmail)
  - Verification token lookup (findByVerificationToken)
  - Activation key authentication (findByActivationKey)
  - QR code operations (findByStoreCode, existsByStoreCode)
  - CRUD operations from JpaRepository
- [x] Create `VendorService` for vendor operations (COMPLETE ✅)
  - Vendor registration with two-step verification
  - Email verification and activation key generation
  - Station app authentication
  - Store status management
  - Printer capability updates
  - Location-based vendor matching
  - Distance calculation algorithms
- [x] Create `VendorController` for vendor API endpoints (COMPLETE ✅)
  - POST /api/vendors/register - Vendor registration
  - GET /api/vendors/verify-email - Email verification
  - POST /api/vendors/station-login - Station app authentication
  - POST /api/vendors/{id}/toggle-store - Store status management
  - POST /api/vendors/{id}/update-capabilities - Printer management
- [x] Implement activation key generation and validation (COMPLETE ✅)
- [x] Add printer capability management (COMPLETE ✅)
- [x] Implement pricing structure (COMPLETE ✅)

### Phase 3: Print Job Management (NEXT - READY TO IMPLEMENT)

#### 🎯 What is PrintJob?
A **PrintJob** is a digital order ticket that contains everything needed to print a document:
```
📋 PrintJob = One Customer's Print Order
├── 👤 Who ordered it? (Customer/Anonymous)
├── 🏪 Which store will print it? (Vendor)
├── 📄 What document? (File reference in MinIO/S3)
├── ⚙️ How to print it? (Color, pages, copies)
├── 💰 How much does it cost? (Price)
├── 📊 What's the status? (Uploaded → Printing → Ready)
└── 🎫 Tracking code (PJ123456)
```

#### 🔄 Upload Files vs Job Upload
- **File Upload** = Storing the actual PDF/document file in cloud storage
- **Job Upload** = Creating a print order that REFERENCES that file
- **Our API**: `POST /api/jobs/upload` handles BOTH in one request

#### ☁️ Cloud Storage Strategy (MinIO)
**Why MinIO instead of local storage:**
- ✅ Server performance (no file bloat)
- ✅ Scalability (millions of files)
- ✅ Reliability (no data loss)
- ✅ Cost-effective (free alternative to AWS S3)

**Storage Structure:**
```
Bucket: printwave-documents
├── 2024/01/15/job_123_resume.pdf
├── 2024/01/15/job_124_photo.jpg
└── 2024/01/16/job_125_report.docx
```

#### 🔗 JPA Relations Explained
```java
// One Customer can have MANY PrintJobs
User customer → List<PrintJob> jobs

// Each PrintJob belongs to ONE Customer and ONE Vendor
PrintJob → User customer (who ordered)
PrintJob → Vendor vendor (who will print)

// One Vendor can have MANY PrintJobs
Vendor vendor → List<PrintJob> assignedJobs
```

#### 💰 BigDecimal for Money
- ❌ `double price = 0.1 + 0.2;` // Result: 0.30000000000000004
- ✅ `BigDecimal price = new BigDecimal("0.10").add(new BigDecimal("0.20"));` // Result: 0.30
- **Rule**: Always use BigDecimal for money calculations

#### 📁 Multi-File Upload Strategy
**Case 1: Same Requirements for All Files**
```
Files: [resume.pdf, cover.pdf, certificates.pdf]
Requirements: A4, Color, 2 copies each
Result: 3 separate PrintJobs with same settings
```

**Case 2: Different Requirements per File**
```
File 1: resume.pdf → A4, B&W, 1 copy
File 2: photo.jpg → A4, Color, 5 copies
File 3: report.pdf → A3, B&W, 2 copies
Result: 3 separate PrintJobs with different settings
```

#### 📋 Implementation Tasks:

**Phase 3A: Foundation Setup (Week 1)**
- [ ] Create JobStatus enum (UPLOADED, PROCESSING, MATCHED, ACCEPTED, PRINTING, READY, COMPLETED, CANCELLED, REJECTED)
- [ ] Create PaperSize enum (A4, A3, LETTER, LEGAL)
- [ ] Create FileType enum (PDF, DOCX, JPG, PNG)
- [ ] Create PrintJob entity with all relationships and fields
- [ ] Create PrintJobRepository with custom queries
- [ ] Test database schema creation

**Phase 3B: Cloud Storage Setup (Week 2)**
- [ ] Add MinIO dependency to pom.xml
- [ ] Set up MinIO with Docker (docker-compose.yml)
- [ ] Create MinIOConfig configuration class
- [ ] Create FileStorageService for upload/download operations
- [ ] Create bucket and test file upload
- [ ] Implement file URL generation with expiry

**Phase 3C: Job Upload APIs (Week 3)**
- [ ] Create PrintJobService with business logic
- [ ] Implement single file upload endpoint: `POST /api/jobs/upload`
- [ ] Add file validation (size, type, content)
- [ ] Create job tracking code generation
- [ ] Add price calculation logic
- [ ] Test complete upload workflow

**Phase 3D: Multi-File Upload (Week 4)**
- [ ] Implement batch upload (same requirements): `POST /api/jobs/batch-upload-same`
- [ ] Implement batch upload (different requirements): `POST /api/jobs/batch-upload-different`
- [ ] Create JobRequirement DTO for flexible requirements
- [ ] Add batch validation and error handling
- [ ] Test multi-file upload scenarios

**Phase 3E: Job Matching & Vendor APIs (Week 5)**
- [ ] Create VendorMatchingService for job assignment
- [ ] Implement distance-based vendor filtering
- [ ] Add capability-based vendor matching
- [ ] Create vendor job queue endpoint: `GET /api/vendors/job-queue`
- [ ] Add job status update endpoints for vendors
- [ ] Test complete vendor workflow

**Phase 3F: Customer Job Management (Week 6)**
- [ ] Create customer job history endpoint: `GET /api/jobs/history`
- [ ] Add job status tracking endpoint: `GET /api/jobs/status/{trackingCode}`
- [ ] Implement anonymous job tracking (QR code workflow)
- [ ] Add job cancellation functionality
- [ ] Create job reorder functionality
- [ ] Test complete customer workflow

#### 🏗️ Entity Architecture:

**PrintJob Entity (Core)**
```java
@Entity
@Table(name = "print_jobs")
public class PrintJob {
    // Core Identity
    @Id @GeneratedValue private Long id;
    @Column(unique = true) private String trackingCode; // PJ123456
    
    // Relationships
    @ManyToOne private User customer;    // null for anonymous
    @ManyToOne private Vendor vendor;    // assigned vendor
    
    // File Information (MinIO)
    private String originalFileName;     // "resume.pdf"
    private String storedFileName;       // "job_123_resume.pdf"
    private String s3BucketName;        // "printwave-documents"
    private String s3ObjectKey;         // "2024/01/15/job_123_resume.pdf"
    @Enumerated(EnumType.STRING) private FileType fileType;
    private Long fileSizeBytes;
    
    // Print Specifications
    @Enumerated(EnumType.STRING) private PaperSize paperSize;
    private Boolean isColor;
    private Boolean isDoubleSided;
    private Integer copies;
    
    // Pricing (BigDecimal for precision)
    @Column(precision = 10, scale = 2) private BigDecimal pricePerPage;
    @Column(precision = 10, scale = 2) private BigDecimal totalPrice;
    private Integer totalPages;
    
    // Status & Timestamps
    @Enumerated(EnumType.STRING) private JobStatus status;
    @CreationTimestamp private LocalDateTime createdAt;
    private LocalDateTime matchedAt;
    private LocalDateTime completedAt;
    
    // Helper Methods
    public boolean isAnonymous() { return customer == null; }
    public boolean isReadyForPickup() { return status == JobStatus.READY; }
}
```

**Supporting Enums**
```java
public enum JobStatus {
    UPLOADED, PROCESSING, MATCHED, ACCEPTED, 
    PRINTING, READY, COMPLETED, CANCELLED, REJECTED
}

public enum PaperSize {
    A4("210x297mm"), A3("297x420mm"), 
    LETTER("216x279mm"), LEGAL("216x356mm");
}

public enum FileType {
    PDF("application/pdf"), 
    DOCX("application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
    JPG("image/jpeg"), PNG("image/png");
}
```

#### 🚀 API Endpoints (Planned):

**Customer APIs:**
```java
// Single file upload
POST /api/jobs/upload
{
  file: MultipartFile,
  paperSize: "A4",
  isColor: true,
  isDoubleSided: false,
  copies: 2
}

// Multi-file upload (same requirements)
POST /api/jobs/batch-upload-same
{
  files: MultipartFile[],
  paperSize: "A4",
  isColor: true,
  copies: 1
}

// Multi-file upload (different requirements)
POST /api/jobs/batch-upload-different
{
  files: MultipartFile[],
  requirements: "[{paperSize:'A4',isColor:true,copies:1},{paperSize:'A3',isColor:false,copies:2}]"
}

// Customer job history
GET /api/jobs/history (JWT required)

// Job status tracking
GET /api/jobs/status/{trackingCode} (public - works for anonymous)
```

**Vendor APIs:**
```java
// Get assigned jobs
GET /api/vendors/job-queue (JWT required)

// Job status updates
POST /api/jobs/{jobId}/accept    // Vendor accepts job
POST /api/jobs/{jobId}/printing  // Started printing
POST /api/jobs/{jobId}/ready     // Ready for pickup
POST /api/jobs/{jobId}/complete  // Customer picked up
POST /api/jobs/{jobId}/reject    // Vendor rejects job
```

**Anonymous APIs (QR Code):**
```java
// QR landing page
GET /store/{storeCode}

// Anonymous upload (only from QR page)
POST /api/jobs/qr-anonymous-upload
{
  storeCode: "PW0001",
  file: MultipartFile,
  paperSize: "A4",
  isColor: false,
  copies: 1
}
```

#### 🔧 MinIO Setup (Docker):
```yaml
# docker-compose.yml
version: '3.8'
services:
  minio:
    image: minio/minio
    ports:
      - "9000:9000"    # API
      - "9001:9001"    # Console
    environment:
      MINIO_ACCESS_KEY: printwave-key
      MINIO_SECRET_KEY: printwave-secret
    command: server /data --console-address ":9001"
    volumes:
      - ./minio-data:/data
```

#### 📱 Frontend Integration Examples:

**JavaScript Single Upload:**
```javascript
const uploadFile = async (file, requirements) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('paperSize', requirements.paperSize);
    formData.append('isColor', requirements.isColor);
    
    const response = await fetch('/api/jobs/upload', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${jwt_token}` },
        body: formData
    });
    
    return await response.json(); // {trackingCode: "PJ123456", ...}
};
```

**JavaScript Multi-Upload:**
```javascript
const uploadMultipleFiles = async (files, requirements) => {
    const formData = new FormData();
    files.forEach(file => formData.append('files', file));
    formData.append('paperSize', requirements.paperSize);
    
    const response = await fetch('/api/jobs/batch-upload-same', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${jwt_token}` },
        body: formData
    });
    
    return await response.json(); // {trackingCodes: ["PJ123456", "PJ123457"]}
};
```

#### 🎯 Learning Concepts (For Beginners):

**New Technologies:**
- **MinIO**: S3-compatible object storage (free cloud storage)
- **MultipartFile**: Spring Boot file upload handling
- **JPA Relations**: Database entity relationships (@ManyToOne, @OneToMany)
- **BigDecimal**: Precise decimal calculations for money
- **Enums**: Fixed set of values (JobStatus, PaperSize, FileType)
- **Docker Compose**: Container orchestration for MinIO

**Design Patterns:**
- **Service Layer**: Business logic separation (PrintJobService, FileStorageService)
- **Repository Pattern**: Data access abstraction (PrintJobRepository)
- **DTO Pattern**: Data transfer objects for API requests/responses
- **Builder Pattern**: For complex object construction
- **Strategy Pattern**: Different upload strategies (single/multi-file)

**Best Practices:**
- **Cloud Storage**: Never store files locally in production
- **Money Handling**: Always use BigDecimal for financial calculations
- **File Validation**: Check file type, size, content before processing
- **Error Handling**: Comprehensive validation and user-friendly error messages
- **Security**: JWT authentication, file access control
- **Scalability**: Separate jobs for each file, cloud storage

#### 🚀 Phase 3 Status: READY TO IMPLEMENT
- **Foundation**: User and Vendor entities complete
- **Authentication**: JWT system working
- **Database**: PostgreSQL configured
- **Email**: Service ready for notifications
- **Next Step**: Create JobStatus enum and PrintJob entity

### Phase 4: Integration Features
- [ ] Payment gateway integration
- [ ] Real-time messaging (WebSocket/MQTT)
- [ ] Station app communication protocols
- [ ] Advanced job matching algorithms
- [ ] Performance optimization

## Frontend Integration & Development Coordination

### Current Development Status
- **Backend (Core)**: Phase 1 complete, Phase 2 in progress
- **Frontend (Portal)**: Being developed by team member
- **Station App**: Planned for Phase 4

### Frontend Integration Notes

#### User Management APIs (Phase 1 - COMPLETE)
**Available for Frontend Integration:**
- `POST /api/users/register` - User registration
- `POST /api/users/login` - Authentication (returns JWT)
- `GET /api/users/profile` - Protected profile (requires JWT)
- `GET /api/users/dashboard` - Protected dashboard (requires JWT)
- Password reset workflow (email + form)

**JWT Authentication Pattern:**
```javascript
// Frontend login
const response = await fetch('/api/users/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});
const { token } = await response.json();

// Use token for protected endpoints
fetch('/api/users/profile', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

#### Vendor Management APIs (Phase 2 - IN PROGRESS)
**Planned for Frontend Integration:**
- `POST /api/vendors/register` - Business registration
- `GET /api/vendors/verify-email` - Email verification
- Vendor profile management
- Activation key generation and email sending

**Vendor Entity Structure (Final):**
```java
@Entity
public class Vendor {
    // Core Identity
    private Long id;
    private String email;
    private String activationKey;           // For Station app login
    
    // Business Details (Portal Registration)
    private String businessName;
    private String contactPersonName;
    private String phoneNumber;
    private String businessAddress;
    private String city;
    private String state;
    private String zipCode;
    
    // Location (Portal Registration)
    private Double latitude;
    private Double longitude;
    
    // Pricing (Portal Registration)
    private BigDecimal pricePerPageBWSingleSided;
    private BigDecimal pricePerPageBWDoubleSided;
    private BigDecimal pricePerPageColorSingleSided;
    private BigDecimal pricePerPageColorDoubleSided;
    
    // Email Verification (Two-Step Process)
    private Boolean emailVerified;          // Step 1: Email verification
    private String verificationToken;       // Token for email verification
    private Boolean activationKeySent;      // Step 2: Activation key sent?
    
    // Store Status (Station App Control)
    private Boolean isStoreOpen;
    private LocalDateTime storeStatusUpdatedAt;
    private Boolean stationAppConnected;
    
    // Printer Capabilities (Station App Updates)
    private String printerCapabilities;     // JSON string
    
    // Account Management
    private Boolean isActive;
    private LocalDateTime registeredAt;
    private LocalDateTime lastLoginAt;
    
    // NO ROLE FIELD - vendor is always a vendor
}
```

**Vendor Registration Form Fields (for Frontend):**
```javascript
// Portal vendor registration form
{
  email: string,
  businessName: string,
  contactPersonName: string,
  phoneNumber: string,
  businessAddress: string,
  city: string,
  state: string,
  zipCode: string,
  latitude: number,        // From map interface
  longitude: number,       // From map interface
  pricePerPageBWSingleSided: number,
  pricePerPageBWDoubleSided: number,
  pricePerPageColorSingleSided: number,
  pricePerPageColorDoubleSided: number
}
```

#### Station App Integration (Phase 4 - PLANNED)
**Planned APIs:**
- `POST /api/vendors/station-login` - Activation key login
- `GET /api/vendors/job-queue` - Pending print jobs
- `POST /api/vendors/toggle-store` - Open/close store
- `POST /api/vendors/update-capabilities` - Printer capabilities
- `POST /api/jobs/accept` - Accept print job
- `POST /api/jobs/complete` - Mark job completed

#### Maps Integration - Development Responsibilities

**Backend (Core API) Responsibilities:**
- ✅ Store location coordinates (latitude, longitude) in Vendor entity
- ✅ Create API endpoints to receive coordinates from frontend
- ✅ Implement distance calculation between customer and vendor locations
- ✅ Filter vendors by distance ("as the crow flies" calculation)
- ✅ Provide location-based vendor search functionality

**Frontend (Portal) Responsibilities:**
- Map interface for vendor location selection during registration
- Interactive map with draggable pins for precise location setting
- Customer location input (current location or address entry)
- Display nearby vendors on customer-facing map
- Integration with free mapping services (OpenStreetMap + Leaflet.js recommended)

**Maps API Options (Frontend Implementation):**
1. **OpenStreetMap + Leaflet.js** - 100% Free (Recommended)
2. **Mapbox** - Free tier: 50,000 map loads/month
3. **Google Maps** - $200 free credit/month, then paid
4. **MapTiler** - Free tier: 100,000 map loads/month

**Location Data Flow:**
```javascript
// Frontend collects coordinates via map interaction
const coordinates = { latitude: 40.7128, longitude: -74.0060 };

// Sends to backend API
fetch('/api/vendors/register', {
  body: JSON.stringify({
    businessName: "ABC Print Shop",
    latitude: coordinates.latitude,
    longitude: coordinates.longitude,
    // ... other vendor details
  })
});
```

**Backend receives and stores coordinates - no map integration needed in backend**

### Migration Guide for Production

### Current Email Link Flow (Development)
1. **Password Reset Email** → Contains link: `http://localhost:8080/api/users/reset-password?token=abc123`
2. **User Clicks Link** → `GET /api/users/reset-password?token=abc123`
3. **Backend Returns** → HTML form with token pre-filled
4. **User Submits Form** → `POST /api/users/reset-password` (JSON)
5. **Password Reset** → Success message displayed

### Frontend Integration (Production)

#### Option 1: Redirect to Frontend (Recommended)
```java
@GetMapping("/reset-password")
public ResponseEntity<Void> redirectToFrontend(@RequestParam String token) {
    return ResponseEntity.status(HttpStatus.FOUND)
        .header("Location", "https://yourapp.com/reset-password?token=" + token)
        .build();
}
```

#### Option 2: API-First Approach
```javascript
// Frontend extracts token from URL
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

// Frontend handles form submission
fetch('/api/users/reset-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        token: token,
        newPassword: newPassword
    })
});
```

### Migration Steps for Production

1. **Build Frontend Application**
   - Create password reset page: `/reset-password`
   - Extract token from URL parameters
   - Create form for new password input
   - Call POST API on form submission

2. **Update Backend GET Endpoint**
   - Replace HTML form generation with redirect
   - Point to frontend URL: `https://yourapp.com/reset-password?token=xyz`

3. **Update Email Templates**
   - Change links to production frontend URL
   - Update from `http://localhost:8080` to `https://yourapp.com`

4. **Keep POST API Unchanged**
   - Frontend will use existing `POST /api/users/reset-password`
   - No changes needed to core password reset logic

### Benefits of Current Approach
- **Works Immediately**: No frontend dependency
- **Professional UI**: Clean HTML form with styling
- **API Ready**: POST endpoint ready for frontend integration
- **Flexible Migration**: Easy switch to redirect pattern
- **Backward Compatible**: Can maintain HTML form as fallback

## Authentication Architecture

### Current User Authentication (Portal)
```java
// Users represent customers using the Portal
POST /api/users/login
{
  "email": "customer@example.com",
  "password": "password123"
}
// Returns JWT token for Portal access
```

### Future Vendor Authentication (Station App)
```java
// Vendors use separate authentication system
POST /api/vendors/login
{
  "email": "vendor@printshop.com",
  "password": "password123"
}
// OR activation key login
POST /api/vendors/station-login
{
  "activationKey": "abc123def456"
}
// Returns JWT token for Station app access
```

### Key Design Decisions
- **User = Customer**: All users in User table represent Portal customers
- **Separate Vendor Table**: Vendors are completely independent entities
- **Different Login Systems**: Portal vs Station app authentication
- **No Role Confusion**: User table always contains customers
- **Clear Separation**: No shared authentication between customers and vendors

#### User Verification Strategy
**Phase 1 (Current)**: Email Verification
- Free email verification using SMTP
- Simple token-based verification
- Good learning foundation

**Phase 2 (Future)**: SMS OTP
- Add phone verification with services like Twilio (~$0.01/SMS)
- Enhanced security for business accounts

**Phase 3 (Advanced)**: OAuth Integration
- Google/GitHub login options
- Professional user experience
- More complex but powerful

#### After User Layer Completion
- [ ] **Vendor Entity**: Business details, activation keys, printer capabilities
- [ ] **Vendor Service & Controller**: Vendor registration workflow
- [ ] **PrintJob Entity**: Customer orders, status tracking, vendor assignment
- [ ] **Document Entity**: File metadata, storage references

#### Integration Features
- [ ] Email service for user & vendor notifications
- [ ] Station app authentication via activation keys
- [ ] Printer auto-discovery integration

### 🛠️ Development Commands

#### Environment Setup
```bash
# Load environment variables
source ./setLocalEnv.sh

# Run application
./mvnw spring-boot:run

# Run from IDE with environment
# Use IntelliJ terminal: source ./setLocalEnv.sh && ./mvnw spring-boot:run
```

#### Testing
```bash
# Run all tests
./mvnw test

# Run specific test with database operations
./mvnw test -Dtest=DatabaseTestRunner
```

### 📋 API Testing Guide (Postman)

#### Prerequisites
1. **Start Application**: `./mvnw spring-boot:run`
2. **Base URL**: `http://localhost:8080`
3. **Content-Type**: `application/json` for all POST requests

#### JWT Security Testing

**Test 1: Try Protected Endpoint Without Token (Should Fail)**
- **Method**: `GET`
- **URL**: `/api/users/profile`
- **Headers**: None
- **Expected**: `401 Unauthorized`

**Test 2: Login to Get JWT Token**
- **Method**: `POST`
- **URL**: `/api/users/login`
- **Body**: `{"email": "user@example.com", "password": "password123"}`
- **Expected**: JWT token in response
- **Note**: Copy the `token` value for next tests

**Test 3: Access Protected Profile with JWT**
- **Method**: `GET`
- **URL**: `/api/users/profile`
- **Headers**: `Authorization: Bearer YOUR_JWT_TOKEN`
- **Expected**: User profile data

**Test 4: Access Protected Dashboard with JWT**
- **Method**: `GET`
- **URL**: `/api/users/dashboard`
- **Headers**: `Authorization: Bearer YOUR_JWT_TOKEN`
- **Expected**: Personalized dashboard data

**Test 5: Try Invalid Token**
- **Method**: `GET`
- **URL**: `/api/users/profile`
- **Headers**: `Authorization: Bearer invalid-token`
- **Expected**: `401 Unauthorized`

#### Public Endpoints Test Sequence

**1. User Registration**
- **Method**: `POST`
- **URL**: `/api/users/register`
- **Body**:
```json
{
  "email": "test@example.com",
  "password": "password123",
  "name": "Test User"
}
```
- **Expected**: `"User registered successfully. Please check your email for verification."`
- **Note**: Role is automatically set to CUSTOMER

**2. Email Verification**
- **Method**: `GET`
- **URL**: `/api/users/verify?token=YOUR_TOKEN_FROM_EMAIL`
- **Expected**: `"Email verified successfully! You can now login."`

**3. User Login**
- **Method**: `POST`
- **URL**: `/api/users/login`
- **Body**:
```json
{
  "email": "test@example.com",
  "password": "password123"
}
```
- **Expected**: `"Login successful! Welcome Test User"`

**4. Password Reset Request**
- **Method**: `POST`
- **URL**: `/api/users/request-password-reset`
- **Body**:
```json
{
  "email": "test@example.com"
}
```
- **Expected**: `"Password reset email sent. Please check your email."`

**5. Password Reset Form (Browser)**
- **Method**: `GET`
- **URL**: `/api/users/reset-password?token=YOUR_RESET_TOKEN`
- **Expected**: HTML form for password reset

**6. Password Reset API**
- **Method**: `POST`
- **URL**: `/api/users/reset-password`
- **Body**:
```json
{
  "token": "YOUR_RESET_TOKEN_FROM_EMAIL",
  "newPassword": "newPassword123"
}
```
- **Expected**: `"Password reset successful! You can now login with your new password."`

**7. Login with New Password**
- **Method**: `POST`
- **URL**: `/api/users/login`
- **Body**:
```json
{
  "email": "test@example.com",
  "password": "newPassword123"
}
```
- **Expected**: `"Login successful! Welcome Test User"`

#### Common Issues
- **Email not received**: Check application logs, verify `.env` credentials
- **Token not found**: Check logs for generated token, tokens expire after 15 minutes
- **Spring Security blocking**: Ensure security is disabled in `application.properties`
- **Response times**: Should be ~200ms thanks to async email processing

### 📋 Configuration Files

#### Environment Variables (.env)
```
DB_URL=jdbc:postgresql://localhost:5432/printwave_db
DB_USERNAME=printwave_user
DB_PASSWORD=[your_password]
```

#### Spring Configuration (application.properties)
- Database URL: `${DB_URL}`
- Username: `${DB_USERNAME}`
- Password: `${DB_PASSWORD}`
- JPA: Hibernate auto-DDL enabled for development

### 🔧 Technology Stack
- **Framework**: Spring Boot 3.x
- **Database**: PostgreSQL
- **ORM**: Spring Data JPA with Hibernate
- **Build Tool**: Maven
- **Development Tools**: Lombok for boilerplate reduction
- **Database Management**: DBeaver

### 📝 Development Notes
- Following professional best practices with proper separation of concerns
- Environment-based configuration for security
- Comprehensive testing approach with isolated test components
- Clean entity design with proper JPA relationships
- Git-friendly setup with sensitive data protection

---

## Development Log

### Session 1 (Initial Setup)
- Set up PostgreSQL database and DBeaver
- Configured Spring Boot with environment variables
- Created User entity with UserRole enum
- Implemented UserRepository with custom queries
- Added comprehensive testing with DatabaseTestRunner
- Verified database connectivity and data persistence
- Created comprehensive PROJECT_DESCRIPTION.md for continuity
- Decided on verification strategy: Email first, then SMS OTP, then OAuth
- Planned to complete User layer before moving to Vendor entity

### Session 2 (Password Reset Enhancement)
- Enhanced User entity with password reset functionality
- Added `passwordResetToken` field for secure token storage
- Added `passwordResetExpiry` field for token expiration security
- Updated PROJECT_DESCRIPTION.md with new field documentation
- Prepared foundation for password reset service implementation

### Session 3 (Project Architecture Planning)
- Defined complete "Uber for Printing" workflow
- Detailed customer journey from upload to collection
- Specified vendor journey including printer auto-discovery
- Planned database design strategy with separate User/Vendor tables
- Created comprehensive 4-phase development plan
- Documented printer capability management and job queue system
- Established foundation for Station app integration
- Ready to begin UserService implementation

### Session 4 (UserService Implementation)
- Created UserService class with business logic layer
- Implemented registerUser() method with User entity as input
- Added BCrypt password hashing for security
- Implemented duplicate email validation
- Added verification token generation using UUID
- Used @Transactional annotation for database consistency
- Updated PROJECT_DESCRIPTION.md with service layer documentation

### Session 5 (Complete User Layer Implementation)
- Added verifyEmail() method to UserService
- Implemented loginUser() method with authentication
- Added password reset functionality (requestPasswordReset, resetPassword)
- Created EmailService with Gmail SMTP integration
- Added spring-boot-starter-mail dependency to pom.xml
- Integrated email notifications in UserService methods
- Created UserController with complete REST API endpoints
- Added comprehensive error handling and HTTP responses
- Updated PROJECT_DESCRIPTION.md with complete user layer documentation
- Phase 1 (User Layer) nearly complete - only JWT authentication remaining

### Session 6 (Security & Performance Optimization)
- Temporarily disabled Spring Security for API testing
- Fixed database schema conflicts by dropping and recreating users table
- Successfully tested all API endpoints with Postman
- Identified security vulnerabilities with URL parameters containing passwords
- Created comprehensive DTO layer for secure API requests:
  - LoginRequest.java for secure login authentication
  - PasswordResetEmailRequest.java for password reset requests
  - PasswordResetRequest.java for password reset completion
- Refactored all endpoints to use JSON body instead of URL parameters
- Enhanced API security by preventing sensitive data exposure in URLs
- Identified email sending performance bottleneck (4.5s response times)
- Implemented asynchronous email processing with @Async annotation
- Added @EnableAsync to main application for background email processing
- Achieved significant performance improvement (4.5s → 200ms response times)
- Comprehensive testing of all endpoints with improved security and performance
- Updated PROJECT_DESCRIPTION.md with security and performance enhancements
- Phase 1 (User Layer) complete except for JWT authentication

### Session 7 (Email Link Integration & Frontend Preparation)
- Identified issue with password reset email links after DTO implementation
- Email links contained URL parameters but endpoint expected JSON body
- Implemented dual endpoint pattern for password reset:
  - GET /api/users/reset-password?token=xyz - Returns HTML form for email links
  - POST /api/users/reset-password - JSON API for frontend/programmatic access
- Created professional HTML form with embedded CSS styling
- Added JavaScript for form submission using fetch API
- Implemented token pre-filling in hidden form fields
- Added comprehensive error handling for invalid/expired tokens
- Documented frontend integration and production migration strategies
- Provided clear migration path from HTML forms to frontend redirects
- Enhanced user experience with immediate functionality and future flexibility
- Updated PROJECT_DESCRIPTION.md with dual endpoint documentation and migration guide
- Added comprehensive Postman API testing guide with step-by-step instructions
- Documented complete test sequence from registration to password reset
- Included common issues and troubleshooting steps for developers

### Session 8 (Architecture Clarification & Design Decisions)
- Clarified User vs Vendor table architecture and authentication systems
- Decided User table represents Portal customers only (User = Customer)
- Planned separate Vendor table for print shop vendors (independent entities)
- Established different login systems: Portal (email/password) vs Station (activation key)
- Removed role confusion by treating User table as customer-only
- Documented clear separation between customer and vendor workflows
- Updated PROJECT_DESCRIPTION.md with authentication architecture section
- Ready to implement JWT authentication for User (customer) layer

### Session 9 (JWT Security Implementation - Complete Phase 1)
- **JWT Utility Implementation**: Created `JwtUtil.java` with token generation and validation
- **JWT Security Filter**: Implemented `JwtAuthenticationFilter.java` for automatic token validation
- **Spring Security Configuration**: Created `SecurityConfig.java` for JWT authentication setup
- **Protected Endpoints**: Added `/api/users/profile` and `/api/users/dashboard` with JWT protection
- **Role-Based Access Control**: Implemented `@PreAuthorize("hasRole('CUSTOMER')")` annotations
- **Professional DTOs**: Created `ProfileResponse.java` and `DashboardResponse.java` for structured responses
- **Security Testing**: Comprehensive JWT security testing via Postman
- **Documentation Updates**: Updated PROJECT_DESCRIPTION.md with complete JWT implementation details
- **Phase 1 Complete**: User layer with full JWT authentication system is now production-ready
- **Architecture Enhancement**: Added security package with proper separation of concerns
- **Ready for Phase 2**: Vendor layer implementation with separate authentication system

### Session 10 (Phase 2 Planning - Vendor Layer Design)
- **Vendor Entity Architecture**: Designed comprehensive vendor entity with business details
- **Location-Based Matching**: Planned coordinate-based store discovery system
- **Pricing Structure**: Designed flexible pricing (B&W/Color, Single/Double sided)
- **Store Status Management**: Planned manual open/close toggle for real-time store availability
- **Distance Calculation**: Decided on "as the crow flies" approach with expandable search radius
- **Store Selection Methods**: Planned manual selection + automatic broadcast system
- **Customer Location Strategy**: Decided to handle per-order location (not stored in User entity)
- **Frontend Coordination**: Added comprehensive frontend integration documentation
- **API Planning**: Documented planned vendor endpoints for Portal and Station app
- **Maps Integration Planning**: Clarified backend vs frontend responsibilities for location features
- **Maps API Research**: Evaluated free options (OpenStreetMap recommended for frontend)
- **Payment Strategy**: Decided to keep vendor entity simple, add payment fields later
- **Role Architecture**: Decided NO role field in Vendor entity (separate from User roles)
- **Registration Flow**: Designed two-step verification (email verification → activation key)
- **Anonymous vs Registered Strategy**: Designed QR-only anonymous printing to reduce vendor risk
- **Complete Application Flow**: Documented detailed workflows for all user types and components
- **Three-App Architecture**: Detailed Core API, Portal Frontend, and Station App responsibilities
- **Risk Mitigation**: Anonymous only for QR codes (customer present), registered required for online
- **Development Coordination**: Enhanced documentation for team collaboration
- **Phase 2 Status**: Ready to implement Vendor entity with final specifications

### Session 11 (Phase 2 Implementation - Complete Vendor Layer)
- **VendorService Implementation**: Created comprehensive vendor service with business logic
  - Vendor registration with two-step verification workflow
  - Email verification and activation key generation
  - Station app authentication with comprehensive login response
  - Store status management and printer capability updates
  - Location-based vendor matching with distance calculation
  - Flexible vendor readiness checks for testing vs production
- **VendorController Implementation**: Created complete REST API endpoints
  - POST /api/vendors/register - Business registration with all details
  - GET /api/vendors/verify-email - Email verification workflow
  - POST /api/vendors/station-login - Station app authentication with full vendor info
  - POST /api/vendors/{id}/toggle-store - Store status management
  - POST /api/vendors/{id}/update-capabilities - Printer capability updates
- **DTO Layer Enhancement**: Created comprehensive DTOs for vendor operations
  - VendorRegistrationRequest - Complete business registration form
  - VendorLoginResponse - Comprehensive Station app login response
  - StationLoginRequest, StoreStatusRequest, PrinterCapabilitiesRequest
  - ApiResponse - Clean success/error message handling
- **Email Service Enhancement**: Extended existing EmailService with vendor methods
  - sendVendorVerificationEmail() - Professional verification emails
  - sendVendorActivationEmail() - Activation key delivery with setup instructions
- **Security Configuration**: Updated Spring Security for vendor endpoints
  - Added vendor endpoints to permitted public URLs
  - Maintained separation between user and vendor authentication
- **Testing & Validation**: Complete API testing with Postman
  - Successful vendor registration workflow
  - Email verification and activation key generation
  - Station app login with comprehensive vendor information
  - Store management and printer capability updates
- **Phase 2 Complete**: Vendor management system fully functional and ready for frontend integration
- **Architecture Ready**: Foundation prepared for Phase 3 (Print Job Management)

### Session 12 (Enhanced Vendor Authentication - Password-Based Login)
- **Problem Identification**: Colleague suggested improving UX by allowing password-based authentication
  - First-time login: activation key + password setup
  - Subsequent logins: store code + password (easier than activation key)
- **Entity Enhancement**: Added password authentication fields to Vendor entity
  - `passwordHash` - BCrypt hashed password for security
  - `passwordSet` - Boolean flag to track if vendor has set up password
- **Service Layer Implementation**: Added new authentication methods to VendorService
  - `firstTimeLoginWithPasswordSetup()` - Activation key + password setup
  - `loginWithStoreCodeAndPassword()` - Store code + password authentication
  - `changePassword()` - Allow vendors to change their password
  - `resetPasswordWithActivationKey()` - Password reset using activation key
- **Controller Layer Enhancement**: Added new authentication endpoints to VendorController
  - POST /api/vendors/first-time-login - First login with password setup
  - POST /api/vendors/login - Regular login with store code + password
  - POST /api/vendors/change-password - Change existing password
  - POST /api/vendors/reset-password - Reset password using activation key
- **DTO Layer Expansion**: Created new DTOs for enhanced authentication
  - FirstTimeLoginRequest - Activation key + new password
  - VendorLoginRequest - Store code + password
  - ChangePasswordRequest - Current password + new password
  - ResetPasswordRequest - Activation key + new password
- **Security Integration**: Updated Spring Security configuration
  - Added new authentication endpoints to permitted URLs
  - Maintained secure password handling with BCrypt
- **User Experience Improvement**: Enhanced authentication flow
  - First login: "Enter activation key + set password"
  - Subsequent logins: "Store code (PW0001) + password"
  - Password reset: Use activation key as backup
- **Backward Compatibility**: Maintained existing activation key system
  - Old `/api/vendors/station-login` still works
  - New system provides enhanced UX while keeping security
- **Testing & Validation**: Successfully compiled and ready for testing
  - All new endpoints properly secured
  - Comprehensive error handling implemented
  - Ready for Station App integration
- **Enhanced Authentication Complete**: Vendor login system significantly improved
  - Better user experience with familiar login pattern
  - Secure password storage with BCrypt
  - Flexible password management (change/reset)
  - Store code as username (easy to remember)
  - Activation key as backup for password reset

### Session 13 (Authentication Error Response Fix)
- **Problem Identification**: Authentication errors returned full VendorLoginResponse with null fields
  - Error case: `{"vendorId": null, "businessName": null, ..., "message": "Error: Invalid password"}`
  - User requested clean error responses with only error message
- **ErrorResponse DTO**: Created new clean error response structure
  - `ErrorResponse.java` - Simple error response with success flag and message
  - Static factory method `ErrorResponse.of(message)` for easy creation
  - Consistent structure: `{"success": false, "message": "error message", "error": "error message"}`
- **Controller Enhancement**: Fixed authentication endpoints to return proper error responses
  - Updated `POST /api/vendors/first-time-login` to return `ErrorResponse` on failure
  - Updated `POST /api/vendors/login` to return `ErrorResponse` on failure
  - Changed return type from `ResponseEntity<VendorLoginResponse>` to `ResponseEntity<?>` for flexibility
- **Clean Error Handling**: Authentication errors now return clean, minimal responses
  - Success: Full `VendorLoginResponse` with all vendor details
  - Error: Simple `ErrorResponse` with only error message
  - No more null fields in error responses
  - Better API design for frontend consumption
- **User Experience Improvement**: API responses now provide clear distinction between success and error cases
  - Frontend can easily identify error responses by checking `success` field
  - Error messages are clean and actionable
  - No unnecessary null data in error responses
- **Authentication Error Fix Complete**: Clean error responses implemented for all authentication endpoints
  - Maintains backward compatibility for success cases
  - Improved error handling for better frontend integration
  - Professional API design with consistent error structure

---

*This document should be updated after each development session to maintain accurate project state documentation.*
