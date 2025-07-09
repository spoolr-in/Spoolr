# PrintWave Core - Project Description

## Overview
PrintWave is a print service management platform that connects customers with local print vendors through a distributed system. The core application manages vendor registration, customer orders, and print job coordination.

## System Architecture
- **Core App**: Backend service handling vendor registration, user management, and business logic
- **Station App**: Vendor-side application for printer management and auto-discovery
- **Customer Interface**: (Future) Customer-facing application for placing print orders

## Complete User Workflow ("Uber for Printing")

### Customer Journey:
1. **Registration & Login**: Customer signs up on Portal
2. **Upload Document**: Customer uploads PDF/Word file
3. **Select Print Options**: Choose paper size, color, quantity, finishing options
4. **Find Print Shops**: System shows nearby print shops with required capabilities
5. **Choose Shop & Pay**: Customer selects shop and pays online
6. **Job Submission**: Print job with all presets sent to selected shop's Station app
7. **Status Updates**: Customer receives notifications about job progress
8. **Collection**: Customer goes to shop and collects printed documents

### Vendor (Print Shop) Journey:
1. **Business Registration**: Shop owner registers business details on Portal
2. **Email Activation**: Receives email with Station app download link + activation key
3. **Station App Setup**: Downloads, installs, and logs in using activation key
4. **Printer Auto-Discovery**: Station app detects all connected printers and their capabilities
5. **Capability Sync**: Printer capabilities and pricing info sent to Core database
6. **Job Queue Management**: Receives print jobs in Station app queue
7. **Job Processing**: 
   - Views job details with customer's presets
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

#### 8. Testing Infrastructure
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
│   │   └── UserController.java
│   ├── dto/
│   │   ├── LoginRequest.java
│   │   ├── PasswordResetEmailRequest.java
│   │   └── PasswordResetRequest.java
│   ├── entity/
│   │   └── User.java
│   ├── enums/
│   │   └── UserRole.java
│   ├── repository/
│   │   └── UserRepository.java
│   └── service/
│       ├── UserService.java
│       └── EmailService.java (with @Async methods)
├── test/java/com/printwave/core/
│   ├── PrintwaveCoreApplicationTests.java
│   └── component/
│       └── DatabaseTestRunner.java
└── resources/
    └── application.properties
```

### 🎯 Next Development Steps

## Database Design Strategy

### User Management (Current Focus)
- **User Table**: Common fields for all user types (customers, vendors, admins)
- **Fields**: `id`, `email`, `name`, `password`, `role`, `emailVerified`, `verificationToken`, `passwordResetToken`, `passwordResetExpiry`

### Vendor Management (Future)
- **Vendor Table**: Separate table for vendor-specific information
- **Relationship**: Foreign key to User table (`user_id`)
- **Fields**: `activationKey`, `businessDetails`, `printerCapabilities`, `servicePricing`
- **Reasoning**: Separation of concerns, cleaner logic, easier maintenance

### Print Job Management (Future)
- **PrintJob Table**: Customer print requests with all specifications
- **Document Table**: File metadata and storage references
- **JobStatus Table**: Track job progress through workflow

## Development Plan (Step-by-Step)

### Phase 1: Complete User Layer (Current)
- [x] Update `User` entity with verification and password reset fields
- [x] Create `UserService` for business logic
- [x] Create `UserController` with REST endpoints
- [x] Implement email service for notifications
- [x] Add secure DTO layer for API requests
- [x] Implement asynchronous email processing
- [x] Test complete user management flow
- [ ] Add JWT authentication and security

### Phase 2: Vendor Layer
- [ ] Create `Vendor` entity with business details
- [ ] Create `VendorService` for vendor operations
- [ ] Create `VendorController` for vendor API endpoints
- [ ] Implement activation key generation and validation
- [ ] Add printer capability management
- [ ] Implement pricing structure

### Phase 3: Print Job Management
- [ ] Create `PrintJob` entity for job tracking
- [ ] Create `Document` entity for file management
- [ ] Implement file upload and storage
- [ ] Add job matching algorithm (vendor selection)
- [ ] Create job queue management
- [ ] Add real-time status updates

### Phase 4: Integration Features
- [ ] Payment gateway integration
- [ ] Real-time messaging (WebSocket/MQTT)
- [ ] Station app communication protocols
- [ ] Advanced job matching algorithms
- [ ] Performance optimization

## Frontend Integration & Production Migration Guide

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

#### Test Sequence

**1. User Registration**
- **Method**: `POST`
- **URL**: `/api/users/register`
- **Body**:
```json
{
  "email": "test@example.com",
  "password": "password123",
  "name": "Test User",
  "role": "CUSTOMER"
}
```
- **Expected**: `"User registered successfully. Please check your email for verification."`

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

---

*This document should be updated after each development session to maintain accurate project state documentation.*
