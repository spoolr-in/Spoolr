# 🔐 PrintWave Core - Secure Backend API

**Spoolr** (formerly PrintWave) is a comprehensive printing service platform backend that connects customers with local print vendors, creating an "Uber for Printing" solution.

![Java](https://img.shields.io/badge/Java-21-orange)
![Spring Boot](https://img.shields.io/badge/Spring%20Boot-3.5.3-brightgreen)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue)
![MinIO](https://img.shields.io/badge/MinIO-S3%20Compatible-red)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)
![Security](https://img.shields.io/badge/Security-Environment%20Variables-green)

## 🚨 **IMPORTANT: REBRANDING NOTICE**

**PrintWave → Spoolr Transition**
- **User-Facing Brand**: "Spoolr" (all UI, emails, marketing)
- **Technical Infrastructure**: "PrintWave" (APIs, database, containers)
- **Tagline**: "Spoolr - Print Anywhere, Anytime"

## 🎯 **What's New - Security Enhanced**

✅ **Environment Variable Security Implementation**
- All hardcoded credentials moved to `.env` file
- Secure Docker Compose configuration
- Production-ready credential management
- Git security with `.env` file exclusion

✅ **Automatic Status Progression**
- Smart printing time calculation
- Auto PRINTING → READY progression
- Auto READY → COMPLETED (24h timeout)

✅ **Enhanced Email Notification System**
- Dual notifications (WebSocket + Email)
- Professional branded email templates
- Async email processing with proper error handling

✅ **Clean Code Implementation**
- Replaced all debug print statements with proper SLF4J logging
- Improved error handling and debugging capabilities

## 🚀 **Quick Start (Secure Setup)**

### Prerequisites
- Docker & Docker Compose
- Git

### 1. Clone & Navigate
```bash
git clone <repository-url>
cd PrintWaveApp/printwave-core
```

### 2. 🔐 **Security Setup (CRITICAL)**
```bash
# Copy environment template
cp .env .env

# Edit with your secure credentials
nano .env  # or your preferred editor
```

**⚠️ SECURITY CRITICAL:** Update these values in `.env`:

```bash
# 🗄️ Database Credentials (CHANGE THESE!)
POSTGRES_PASSWORD=your_super_secure_db_password_here
DB_PASSWORD=your_super_secure_db_password_here

# 📁 Storage Credentials (CHANGE THESE!)
MINIO_ROOT_PASSWORD=your_ultra_secure_minio_password_here
MINIO_SECRET_KEY=your_ultra_secure_minio_password_here

# 📧 Email Credentials (REQUIRED)
EMAIL_USERNAME=your_email@gmail.com
EMAIL_PASSWORD=your_gmail_app_password_here

# 🔑 Security Token (GENERATE NEW!)
JWT_SECRET=your_super_secure_64_character_jwt_secret_here
```

### 3. Generate Secure Passwords
```bash
# Generate secure database password
openssl rand -base64 32

# Generate secure MinIO password  
openssl rand -base64 32

# Generate secure JWT secret (64 chars)
openssl rand -base64 48
```

### 4. Setup Gmail App Password
1. Enable 2FA on your Gmail account
2. Go to [Google App Passwords](https://myaccount.google.com/apppasswords)
3. Generate password for "Spoolr"
4. Use this as `EMAIL_PASSWORD`

### 5. Start Services
```bash
# Start all services with environment variables
docker compose up -d

# Check status
docker compose ps

# View logs
docker compose logs -f printwave-core
```

## 🌐 **Access Points**

| Service | URL | Credentials |
|---------|-----|-------------|
| **Core API** | http://localhost:8080 | - |
| **API Docs** | http://localhost:8080/swagger-ui.html | - |
| **MinIO Console** | http://localhost:9001 | admin / [from .env] |
| **PostgreSQL** | localhost:5433 | [from .env] |

## 🔒 **Security Features**

### Environment Variable Security
- ✅ **No hardcoded credentials** in code
- ✅ **`.env` file** for sensitive data
- ✅ **Git ignored** sensitive files
- ✅ **Docker Compose** environment variable injection
- ✅ **Template system** with `.env.example`

### Application Security
- 🔐 **JWT Authentication** with configurable secrets
- 🛡️ **Role-based access** (Customer, Vendor, Admin)
- 🔄 **Automatic session management**
- 📧 **Secure email notifications** with async processing
- 📁 **Secure file storage** with MinIO S3-compatible storage

### Production Security
- 🚫 **No debug code** in production builds
- 📝 **Proper logging** instead of print statements
- ⏰ **Automatic cleanup** of expired jobs and files
- 🔄 **Health checks** for all services

## 📧 **Email Notification System**

### Features
- **Dual System**: WebSocket (real-time) + Email (persistent)
- **Professional Templates**: Branded Spoolr email templates
- **Automatic Triggers**: Job status changes trigger both notifications
- **Async Processing**: Non-blocking email delivery

### Email Types
- ✅ **Job Accepted**: "A vendor accepted your job"
- 🖨️ **Printing Started**: "Your job is being printed (Est: X min)"  
- 📬 **Ready for Pickup**: "Your job is ready!" (CRITICAL)
- ✅ **Job Completed**: "Thank you for using Spoolr"

## 🤖 **Automatic Job Progression**

### Smart Workflow
```
Manual: Print → [Auto Timer] → Ready → [24h Timer] → Completed
                      ↓                    ↓
                Customer Notified    Customer Notified
```

### Benefits
- **Less Manual Work**: Vendors just click "Print"
- **Better UX**: Customers get accurate timing estimates
- **Auto Cleanup**: Jobs don't sit abandoned forever
- **Smart Timing**: 2-30 minute estimates based on job complexity

## 📚 **API Documentation**

### Core Endpoints
```http
# Authentication
POST /api/users/register     # Customer registration
POST /api/users/login        # Customer login
POST /api/vendors/login      # Vendor login

# Jobs (Customer)
POST /api/jobs/quote         # Get vendor quotes
POST /api/jobs/upload        # Create print job
GET  /api/jobs/history       # Order history
GET  /api/jobs/status/{code} # Track job

# Jobs (Vendor)  
GET  /api/jobs/queue         # Get job queue
POST /api/jobs/{id}/accept   # Accept job
POST /api/jobs/{id}/print    # Start printing
POST /api/jobs/{id}/ready    # Mark ready (optional - auto)
POST /api/jobs/{id}/complete # Complete job
```

### WebSocket Real-Time
```javascript
// Customer job tracking
/topic/job-status/{trackingCode}

// Vendor job offers
/queue/job-offers-{vendorId}
```

For complete API documentation, see [PROJECT_DESCRIPTION.md](PROJECT_DESCRIPTION.md).

## 🏗️ **System Architecture**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Spoolr        │    │   PrintWave     │    │   Spoolr        │
│   Portal        │◄──►│   Core API      │◄──►│   Station       │
│   (Customer)    │    │   (Backend)     │    │   (Vendor)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
│                      │                      │                 │
│ • Document Upload    │ • JWT Auth          │ • Job Queue     │
│ • Vendor Selection   │ • Job Management    │ • Real-time Offers│
│ • Real-time Status   │ • Smart Matching    │ • Print Preview │
│ • Email Notifications│ • Email Service     │ • Status Updates│
└──────────────────────┴─────────────────────┴─────────────────┘
            WebSocket + HTTP              WebSocket + HTTP
```

## 🗂️ **Project Structure**

```
printwave-core/
├── 🔐 .env                    # Environment variables (SECRET - not committed)
├── 📋 .env.example            # Environment template
├── 🐳 docker-compose.yml      # Docker services (uses .env)
├── 📚 PROJECT_DESCRIPTION.md  # Complete API documentation
├── 🚀 README.md              # This file
├── 📝 PRODUCTION_CLEANUP_CHECKLIST.md  # Production readiness
├── 
├── src/main/java/com/printwave/core/
│   ├── config/         # Configuration classes
│   ├── controller/     # REST API controllers
│   ├── entity/         # Database entities
│   ├── repository/     # Data access layer
│   ├── service/        # Business logic
│   ├── security/       # JWT & security config
│   └── enums/          # Application enums
│
├── src/main/resources/
│   ├── application.properties     # Spring configuration
│   └── application-docker.properties
│
└── target/            # Maven build output
```

## 🛠️ **Development**

### Local Development (Outside Docker)
```bash
# Start only infrastructure services
docker compose up postgres minio -d

# Update .env for local development
DB_URL=jdbc:postgresql://localhost:5433/printwave_db
MINIO_ENDPOINT=http://localhost:9000

# Run Spring Boot locally
./mvnw spring-boot:run
```

### Adding New Environment Variables
1. Add to `.env.example` with placeholder values
2. Add to `.env` with real values
3. Add to `docker-compose.yml` in environment section
4. Update README.md documentation

### Testing
```bash
# Run all tests
./mvnw test

# Run with specific profile
./mvnw test -Dspring.profiles.active=test
```

## 📊 **Monitoring & Logs**

### Application Logs
```bash
# Real-time logs
docker compose logs -f printwave-core

# All service logs  
docker compose logs -f

# Specific timeframe
docker compose logs --since 1h printwave-core
```

### Health Checks
```bash
# Check service health
curl http://localhost:8080/actuator/health

# Database connectivity
docker compose exec postgres pg_isready -U ${POSTGRES_USER}

# MinIO connectivity  
curl http://localhost:9000/minio/health/live
```

## 🚨 **Troubleshooting**

### Common Issues

**1. Environment Variables Not Loaded**
```bash
# Check if .env file exists
ls -la .env

# Verify Docker Compose can read variables
docker compose config
```

**2. Database Connection Failed**
```bash
# Check PostgreSQL logs
docker compose logs postgres

# Test connection manually
docker compose exec postgres psql -U ${POSTGRES_USER} -d ${POSTGRES_DB}
```

**3. Email Notifications Not Working**
```bash
# Check application logs for email errors
docker compose logs printwave-core | grep -i email

# Verify Gmail app password
# - Must be Gmail account with 2FA enabled
# - Must use App Password, not regular password
```

**4. MinIO Access Denied**
```bash
# Check MinIO logs
docker compose logs minio

# Verify MinIO credentials
docker compose exec minio mc config host ls
```

**5. Port Conflicts**
```bash
# Check what's using ports
sudo lsof -i :8080
sudo lsof -i :5433  
sudo lsof -i :9000

# Stop conflicting services or change ports in docker-compose.yml
```

## 🔄 **Updates & Maintenance**

### Updating Application
```bash
# Pull latest changes
git pull origin main

# Rebuild with new code
docker compose down
docker compose up --build -d
```

### Database Backup
```bash
# Create backup
docker compose exec postgres pg_dump -U ${POSTGRES_USER} ${POSTGRES_DB} > backup_$(date +%Y%m%d).sql

# Restore backup
docker compose exec -i postgres psql -U ${POSTGRES_USER} ${POSTGRES_DB} < backup.sql
```

### Rotating Credentials
```bash
# Generate new passwords
openssl rand -base64 32

# Update .env file
# Restart services
docker compose restart
```

## 🌟 **Production Deployment**

### Security Checklist
- [ ] Strong unique passwords for all services
- [ ] JWT secret rotated and secured
- [ ] Email credentials configured
- [ ] HTTPS/TLS certificates installed
- [ ] Firewall configured for required ports only
- [ ] Database backups automated
- [ ] Log rotation configured
- [ ] Environment variables secured (not in code)

### Environment Preparation
```bash
# Production .env template
cp .env .env.production

# Use secure password generation
openssl rand -base64 32 > db_password.txt
openssl rand -base64 48 > jwt_secret.txt

# Configure production email service
# Configure production domain URLs
# Configure production SSL certificates
```

## 📞 **Support & Resources**

### Documentation
- **API Reference**: [PROJECT_DESCRIPTION.md](PROJECT_DESCRIPTION.md)
- **Production Guide**: [PRODUCTION_CLEANUP_CHECKLIST.md](PRODUCTION_CLEANUP_CHECKLIST.md)
- **Parent Project**: [../README.md](../README.md)

### Getting Help
- Check application logs first: `docker compose logs printwave-core`
- Review troubleshooting section above
- Check environment variable configuration
- Contact development team

### Contributing
1. Fork repository
2. Create feature branch with security in mind
3. Never commit `.env` file or credentials
4. Test with fresh `.env.example` copy
5. Update documentation for new environment variables

---

## 🏆 **Security Achievements**

✅ **Zero Hardcoded Credentials**  
✅ **Environment Variable Security**  
✅ **Git Security (.env ignored)**  
✅ **Docker Security (variable injection)**  
✅ **Production Ready Configuration**  
✅ **Clean Code (no debug prints)**  
✅ **Proper Logging Implementation**  
✅ **Secure Email Processing**  

---

**Spoolr** - *Print Anywhere, Anytime* 🚀

*Secure. Scalable. Simple.*
# GitHub Actions Test
# Actions test Tuesday 12 August 2025 08:01:47 PM IST
