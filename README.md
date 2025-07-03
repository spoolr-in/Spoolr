# PrintWave - Cloud-Connected Print Automation Platform

![Java](https://img.shields.io/badge/Java-21-orange)
![Spring Boot](https://img.shields.io/badge/Spring%20Boot-3.5.3-brightgreen)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue)
![MinIO](https://img.shields.io/badge/MinIO-S3%20Compatible-red)
![License](https://img.shields.io/badge/License-MIT-yellow)

## 🚀 Project Overview

PrintWave is a comprehensive cloud-connected print automation platform that seamlessly connects customers with print shops. The platform consists of three core modules working together to provide instant document printing services.

### 🏗️ System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  PrintWave      │    │  PrintWave      │    │  PrintWave      │
│  Portal         │◄──►│  Core           │◄──►│  Station        │
│  (Web/Mobile)   │    │  (Backend)      │    │  (Print Shop)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
│                      │                      │                 │
│ • User Registration  │ • Authentication    │ • Job Queue     │
│ • Document Upload    │ • Job Management    │ • Print Preview │
│ • Print Options      │ • Vendor Matching   │ • Auto-Print    │
│ • Vendor Selection   │ • Real-time Messaging│ • Status Updates│
│ • Payment Gateway    │ • File Storage      │ • Offline Support│
│ • Status Tracking    │ • Auto-Assignment   │ • Local Cache   │
└──────────────────────┴─────────────────────┴─────────────────┘
```

### 🎯 Core Features

#### PrintWave Core (This Repository)
- **Authentication & Authorization**: JWT-based secure user management
- **Document Management**: MinIO-based file storage with presigned URLs
- **Job Assignment Logic**: Smart vendor matching and auto-assignment
- **Real-time Communication**: WebSocket/MQTT for instant updates
- **Payment Integration**: Razorpay/Cashfree payment processing
- **Vendor Management**: Capability-based filtering and proximity matching
- **Status Tracking**: Real-time job status and ETA updates

## 🛠️ Technology Stack

### Backend (PrintWave Core)
| Component | Technology |
|-----------|------------|
| **Framework** | Spring Boot 3.5.3 |
| **Language** | Java 21 |
| **Database** | PostgreSQL 15+ |
| **File Storage** | MinIO (S3-Compatible) |
| **Messaging** | WebSocket, MQTT |
| **Authentication** | JWT Tokens |
| **Payment** | Razorpay/Cashfree SDK |
| **Build Tool** | Maven |
| **DevTools** | Spring Boot DevTools, Lombok |

### Additional Modules (Future)
| Module | Technology |
|--------|------------|
| **Portal Frontend** | React.js, Next.js |
| **Mobile App** | React Native / Kotlin |
| **Station App** | .NET 6+ WPF |

## 📋 System Requirements

- **Java**: OpenJDK 21 or later
- **PostgreSQL**: Version 15 or later
- **MinIO**: Latest stable version
- **Maven**: 3.6 or later
- **Memory**: Minimum 2GB RAM
- **Storage**: 1GB for development

## 🚀 Quick Start

### Prerequisites
```bash
# Install Java 21
sudo apt update
sudo apt install openjdk-21-jdk

# Install PostgreSQL
sudo apt install postgresql postgresql-contrib

# Install Maven
sudo apt install maven
```

### Setup Database
```bash
# Start PostgreSQL service
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Create database and user
sudo -u postgres createdb printwave_db
sudo -u postgres createuser --interactive printwave_user
```

### Setup MinIO (Local Development)
```bash
# Download and start MinIO
wget https://dl.min.io/server/minio/release/linux-amd64/minio
chmod +x minio
./minio server ./minio-data --console-address ":9001"
```

### Run Application
```bash
# Clone repository
git clone <repository-url>
cd PrintWaveApp

# Configure application.properties
cp src/main/resources/application.properties.example src/main/resources/application.properties
# Edit database and MinIO configurations

# Build and run
mvn clean install
mvn spring-boot:run
```

## 📊 Data Flow Architecture

```
User Upload → Portal → Core API → Validate → Store Metadata → MinIO
     ↓                                                        ↓
Select Options → Choose Vendor → Assignment Logic → Queue Job → Notify Station
     ↓                                                        ↓
Payment → Confirm → WebSocket/MQTT → Station Accepts → Print Queue
     ↓                                                        ↓
Status Updates ← Real-time Sync ← Job Progress ← Print Complete
```

## 🔐 Security Features

- **HTTPS/WSS**: Encrypted transport layer
- **JWT Authentication**: Stateless token-based auth
- **Role-based Access**: Customer, Vendor, Admin roles
- **File Security**: Temporary presigned URLs
- **Auto-cleanup**: Files deleted post-printing
- **Rate Limiting**: API abuse prevention

## 📱 API Documentation

### Authentication Endpoints
```
POST /api/auth/register    - User registration
POST /api/auth/login       - User authentication
POST /api/auth/refresh     - Token refresh
POST /api/auth/logout      - User logout
```

### Job Management Endpoints
```
POST /api/jobs/create      - Create print job
GET  /api/jobs/{id}        - Get job details
GET  /api/jobs/user/{id}   - Get user jobs
PUT  /api/jobs/{id}/status - Update job status
```

### File Management Endpoints
```
POST /api/files/upload     - Get presigned upload URL
GET  /api/files/{id}       - Get file download URL
DELETE /api/files/{id}     - Delete file
```

### WebSocket Endpoints
```
/ws/jobs          - Job status updates
/ws/stations      - Station communication
/ws/notifications - Real-time notifications
```

## 🏢 Project Structure

```
src/main/java/com/printwave/core/
├── config/          # Configuration classes
├── controller/      # REST controllers
├── dto/            # Data transfer objects
├── entity/         # JPA entities
├── repository/     # Data repositories
├── service/        # Business logic
├── security/       # Security configuration
├── websocket/      # WebSocket handlers
└── util/           # Utility classes

src/main/resources/
├── application.properties    # App configuration
├── application-dev.properties
├── application-prod.properties
└── db/migration/   # Database migrations
```

## 🔄 Development Workflow

1. **Setup Phase**: Database, MinIO, dependencies
2. **Core Development**: Entities, repositories, services
3. **API Development**: Controllers, validation, documentation
4. **Security Integration**: JWT, role-based access
5. **File Management**: Upload, storage, cleanup
6. **Real-time Features**: WebSocket, MQTT integration
7. **Testing**: Unit tests, integration tests
8. **Deployment**: Docker, cloud deployment

## 🚧 Current Status

- [x] Project initialization
- [x] Spring Boot setup
- [x] Technology stack selection
- [ ] Database schema design
- [ ] Authentication system
- [ ] File upload integration
- [ ] Job management APIs
- [ ] WebSocket messaging
- [ ] Payment integration
- [ ] Vendor assignment logic

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💪🏽 Team

- **Atharva Wakodikar**: Bachelor's student studying at Symbiosis Skills and Professional University Pune, majoring in Computer Science.

  [<img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" />](https://www.linkedin.com/in/athyadw45/)
  [<img src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" />](https://github.com/Athyadw45)


- **Vyankatesh Kulkarni**: Bachelor's student studying at Symbiosis Skills and Professional University Pune, majoring in Cyber Security.

  [<img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" />](https://www.linkedin.com/in/vyankatesh-kulkarni-9a1934251/)
  [<img src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" />](https://github.com/VyankateshKulkarni13)


- **Suraj Hiregowda**: Bachelor's student studying at Symbiosis Skills and Professional University Pune, majoring in Computer Science.

  [<img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" />](https://www.linkedin.com/in/6132suraj/)
  [<img src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" />](https://github.com/Suraj-132)️
