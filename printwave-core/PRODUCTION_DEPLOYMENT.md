# 🚀 **SPOOLR PRODUCTION DEPLOYMENT GUIDE**

## 📦 **Container-Based Production Deployment (Following Your ObjectVault Pattern)**

You can use the **SAME Docker containers** for Spoolr production, just like your ObjectVault setup!

### **🎯 Database Strategy: Same Container, Different Database**

**Answer to "Same DB or Different DB?"**
- ✅ **Same PostgreSQL container** (same image)
- ✅ **Different database name** (`spoolr_production` vs `printwave_db`) 
- ✅ **Different user** (`spoolr_prod_user` vs `printwave_user`)
- ✅ **Completely separate data volumes** (no mixing of dev/prod data)
- ✅ **Stronger production passwords**

### **🎯 Your Deployment Pattern (Same as ObjectVault):**

```bash
# Development
docker-compose up -d

# Spoolr Production (same containers, different config)
docker-compose -f docker-compose.prod.yml up -d

# CI/CD Updates (only app container, like ObjectVault)
docker-compose -f docker-compose.prod.yml stop spoolr-core
docker-compose -f docker-compose.prod.yml pull spoolr-core  
docker-compose -f docker-compose.prod.yml up -d spoolr-core
```

## 🔧 **What Changes for Production:**

### **1. Environment Variables Only**
- ✅ Same PostgreSQL container → `spoolr_production` database + stronger password
- ✅ Same MinIO container → production access keys + `spoolr-production-documents` bucket  
- ✅ Same Spoolr container → production JWT secret + production SMTP settings

### **2. Data Volumes (Completely Separate)**
```yaml
# Development volumes
postgres_data:/var/lib/postgresql/data
minio_data:/data

# Spoolr Production volumes (isolated)
spoolr_postgres_prod_data:/var/lib/postgresql/data
spoolr_minio_prod_data:/data
```

### **3. Container Names**
```yaml
# Development
container_name: printwave-core

# Production  
container_name: spoolr-core-prod
container_name: spoolr-postgresql-prod
container_name: spoolr-minio-prod
```

### **4. Network Configuration**
- Development: `localhost:8080`  
- Spoolr Production: `https://api.spoolr.com` (with reverse proxy)

## 🎯 **Automated CI/CD Setup (Like Your ObjectVault)**

We've created the **exact same CI/CD pattern** as your ObjectVault project:

### **1. CI Pipeline (`.github/workflows/ci.yml`)**
- ✅ **Build & Test** Spoolr application
- ✅ **Create Docker image** and push to DockerHub
- ✅ **Trigger on** every push to `main` branch

### **2. CD Pipeline (`.github/workflows/cd.yml`)**
- ✅ **SSH to production server** (like your GCP setup)
- ✅ **Stop only spoolr-core container** (keeps database running!)
- ✅ **Pull latest image** and restart
- ✅ **Zero downtime** for database and MinIO

### **3. Environment Setup (`setProductionEnv.sh`)**
```bash
#!/bin/bash
# 🚀 SPOOLR PRODUCTION ENVIRONMENT SETUP

export POSTGRES_PASSWORD="super_secure_spoolr_prod_password_2024"
export JWT_SECRET="spoolr_production_jwt_secret_key_256_bits_very_secure_2024_version"
export EMAIL_USERNAME="noreply@spoolr.com"
export EMAIL_PASSWORD="your_production_email_app_password_here"
export MINIO_ACCESS_KEY="spoolr_prod_minio_access_key"
export MINIO_SECRET_KEY="spoolr_prod_minio_secret_key_very_secure"
export BASE_URL="https://api.spoolr.com"
```

## 🌐 **Server Requirements**

### **Minimum Server Specs:**
- **CPU**: 2 cores
- **RAM**: 4GB (8GB recommended)
- **Storage**: 50GB SSD
- **OS**: Ubuntu 20.04+ or CentOS 7+

### **Recommended Cloud Providers:**
- **DigitalOcean Droplet**: $20/month (4GB RAM, 2 vCPUs)
- **AWS EC2 t3.medium**: ~$30/month
- **Vultr High Frequency**: $12/month (2GB RAM, 1 vCPU)
- **Linode Nanode**: $10/month (2GB RAM, 1 vCPU)

## 📊 **Advantages of Container Approach:**

### **✅ Benefits:**
- 💰 **Cost Effective**: No managed database fees
- 🔄 **Consistent**: Same containers in dev/prod
- 🚀 **Fast Setup**: One command deployment
- 🎯 **Full Control**: Complete stack ownership
- 📦 **Portable**: Move between servers easily

### **⚠️ Considerations:**
- 🔧 **Backup Management**: You handle database backups
- 📊 **Monitoring**: Set up your own monitoring
- 🔒 **Security Updates**: Keep containers updated

## 🛡️ **Production Security Checklist**

### **1. Firewall Configuration**
```bash
# Only allow necessary ports
ufw allow 22    # SSH
ufw allow 80    # HTTP
ufw allow 443   # HTTPS
ufw deny 5432   # Block direct PostgreSQL access
ufw deny 9000   # Block direct MinIO access
ufw enable
```

### **2. SSL/TLS Setup (using nginx proxy)**
```yaml
# docker-compose.prod.yml addition
nginx:
  image: nginx:alpine
  ports:
    - "80:80"
    - "443:443"
  volumes:
    - ./nginx.conf:/etc/nginx/nginx.conf
    - ./ssl:/etc/nginx/ssl
```

### **3. Backup Strategy**
```bash
# Database backup script
docker exec postgresql pg_dump -U printwave_prod_user printwave_production > backup.sql

# MinIO backup
docker exec minio mc mirror /data/printwave-production-documents /backup/minio
```

## 🚀 **Production Deployment Steps**

### **First-Time Server Setup:**
```bash
# 1. Clone repository on production server
git clone https://github.com/yourusername/spoolr-core.git  
cd spoolr-core

# 2. Set up production environment
chmod +x setProductionEnv.sh
nano setProductionEnv.sh  # Update with real production credentials

# 3. Initial deployment
. ./setProductionEnv.sh
docker-compose -f docker-compose.prod.yml up -d

# 4. Check status
docker-compose -f docker-compose.prod.yml ps

# 5. View logs
docker-compose -f docker-compose.prod.yml logs -f spoolr-core
```

### **Automated Updates (via CI/CD):**
```bash
# Every git push to main automatically:
# 1. Builds Spoolr app ✅
# 2. Pushes to DockerHub ✅  
# 3. SSHs to server ✅
# 4. Stops only spoolr-core container (DB keeps running!) ✅
# 5. Pulls new image ✅
# 6. Restarts spoolr-core ✅

# Just push your code:
git add .
git commit -m "Update Spoolr"
git push origin main
# → Automatic deployment! 🎉
```

## 📈 **Scaling Options**

### **Single Server (Recommended Start)**
- All containers on one server
- Good for 1,000-10,000 users
- Cost: $20-50/month

### **Multi-Server (Future Growth)**
- Database on separate server
- App servers behind load balancer
- Good for 10,000+ users
- Cost: $100-500/month

---

## 🎯 **RECOMMENDATION FOR YOU:**

**Start with Single Server Container Deployment:**

1. ✅ **Use same containers** (PostgreSQL, MinIO, PrintWave)
2. ✅ **Different environment variables** for production
3. ✅ **Separate data volumes** to avoid mixing dev/prod data
4. ✅ **Add nginx reverse proxy** for SSL and domain mapping
5. ✅ **Set up automated backups**

This approach gives you:
- 🚀 **Quick deployment** (30 minutes setup)
- 💰 **Low cost** ($20-30/month total)
- 🔄 **Easy scaling** when you grow
- 🛡️ **Full control** over your stack

## 📋 **Setup Checklist - Ready to Go Live:**

### **🔧 What You Need to Update:**

#### **1. DockerHub Setup (2 minutes)**
- [ ] Replace `yourusername/spoolr-core:latest` in CI workflow with your DockerHub username
- [ ] Create DockerHub repository: `spoolr-core`
- [ ] Get DockerHub access token

#### **2. Production Server Setup (30 minutes)**
- [ ] Get a server (DigitalOcean $20/month recommended)
- [ ] Set up GitHub repository secrets:
  - `SERVER_HOST`: Your server IP address
  - `SERVER_USERNAME`: SSH username (usually `root` or `ubuntu`)
  - `SSH_PRIVATE_KEY`: SSH private key for server access
  - `DOCKERHUB_USERNAME`: Your DockerHub username
  - `DOCKERHUB_TOKEN`: DockerHub access token

#### **3. Update Production Credentials (5 minutes)**
- [ ] Edit `setProductionEnv.sh` with real production passwords
- [ ] Update email settings for production SMTP
- [ ] Set your actual domain in `BASE_URL`

#### **4. Deploy! (1 command)**
```bash
git push origin main
# → CI/CD pipeline automatically deploys to production! 🚀
```

---

## ✅ **Files Created for You:**

```
spoolr-core/
├── docker-compose.prod.yml        # ✅ Production containers config
├── setProductionEnv.sh            # ✅ Production environment variables
├── .github/workflows/ci.yml       # ✅ CI pipeline (build & push)
├── .github/workflows/cd.yml       # ✅ CD pipeline (deploy)
└── PRODUCTION_DEPLOYMENT.md       # ✅ This deployment guide
```

## 🎉 **Benefits of This Setup:**

### **✅ Same as Your ObjectVault Success:**
- 🔄 **Proven pattern** you already know works
- 💰 **Cost effective**: ~$20/month for full production stack  
- 🚀 **Automated deployment**: Push code = automatic deployment
- 🗄️ **Data safety**: Database never destroyed during updates
- 📦 **Simple**: Same containers, different environment
- 🔧 **Easy rollback**: Keep previous images for quick rollbacks

### **🛡️ Production-Ready Features:**
- ✅ Health checks on all containers
- ✅ Automatic restarts if containers crash
- ✅ Separate production data volumes  
- ✅ Production-grade passwords and secrets
- ✅ Ready for SSL/domain setup

---

## 🚀 **YOU'RE 90% READY FOR PRODUCTION!**

Your Spoolr deployment setup follows the **exact same pattern** as your successful ObjectVault deployment. Just update the credentials and server details, and you're live! 🎉
