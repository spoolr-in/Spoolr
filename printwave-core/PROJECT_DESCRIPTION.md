# PrintWave Core - Project Description

## Overview
PrintWave is a print service management platform that connects customers with local print vendors through a distributed system. The core application manages vendor registration, customer orders, and print job coordination.

## System Architecture
- **Core App**: Backend service handling vendor registration, user management, and business logic
- **Station App**: Vendor-side application for printer management and auto-discovery
- **Customer Interface**: (Future) Customer-facing application for placing print orders

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
  - Fields: `id`, `email`, `name`, `role`, `createdAt`, `updatedAt`
  - JPA annotations: `@Entity`, `@Table`, `@Id`, `@GeneratedValue`
  - Lombok annotations: `@Data` for boilerplate code generation
  - Automatic timestamp management with `@CreationTimestamp` and `@UpdateTimestamp`

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
│   └── repository/
│       └── UserRepository.java
├── test/java/com/printwave/core/
│   ├── PrintwaveCoreApplicationTests.java
│   └── component/
│       └── DatabaseTestRunner.java
└── resources/
    └── application.properties
```

### 🎯 Next Development Steps

#### Immediate (Complete User Layer First)
- [ ] Update `User` entity with verification fields (`phoneNumber`, `emailVerified`, `verificationToken`)
- [ ] Create `UserService` for business logic
- [ ] Create `UserController` with REST endpoints
- [ ] Implement email verification workflow
- [ ] Test complete user management flow

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

---

*This document should be updated after each development session to maintain accurate project state documentation.*
