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
  - Features:
    - BCrypt password encryption
    - Duplicate email validation
    - Verification token generation
    - Transactional database operations

#### 5. Testing Infrastructure
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
│   ├── PrintwaveCoreApplication.java (Main Spring Boot class)
│   ├── entity/
│   │   └── User.java
│   ├── enums/
│   │   └── UserRole.java
│   ├── repository/
│   │   └── UserRepository.java
│   └── service/
│       └── UserService.java
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
- [ ] Create `UserController` with REST endpoints
- [ ] Implement email service for notifications
- [ ] Add JWT authentication and security
- [ ] Test complete user management flow

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

---

*This document should be updated after each development session to maintain accurate project state documentation.*
