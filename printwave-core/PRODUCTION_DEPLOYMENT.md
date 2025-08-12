# 🚀 **PRODUCTION DEPLOYMENT GUIDE**

## 📦 **Container-Based Production Deployment**

You can use the **SAME Docker containers** for production! Here's how:

### **🎯 Approach: Same Containers + Production Environment**

```bash
# Development
docker-compose up -d

# Production (same containers, different config)
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 🔧 **What Changes for Production:**

### **1. Environment Variables Only**
- ✅ Same PostgreSQL container → different database name + stronger password
- ✅ Same MinIO container → different access keys + production bucket
- ✅ Same PrintWave container → production JWT secret + SMTP settings

### **2. Data Volumes**
```yaml
# Development volumes
postgres_data:/var/lib/postgresql/data

# Production volumes (completely separate)
postgres_prod_data:/var/lib/postgresql/data
```

### **3. Network Configuration**
- Development: `localhost:8080`  
- Production: `https://yourdomain.com` (with reverse proxy)

## 📋 **Step-by-Step Production Setup**

### **Step 1: Create Production Environment File**
```bash
# Create .env.prod file
cp .env .env.prod

# Edit production values
nano .env.prod
```

### **Step 2: Production Environment Variables**
```env
# .env.prod - PRODUCTION VALUES
POSTGRES_PASSWORD=super_secure_prod_password_here
JWT_SECRET=production_jwt_secret_256_bits_long
EMAIL_USERNAME=noreply@yourdomain.com
EMAIL_PASSWORD=production_email_app_password
MINIO_ACCESS_KEY=prod_minio_access_key
MINIO_SECRET_KEY=prod_minio_super_secure_secret
```

### **Step 3: Deploy with Production Override**
```bash
# Load production environment
export $(cat .env.prod | xargs)

# Deploy with production config
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
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

## 🚀 **Quick Production Deploy Commands**

```bash
# 1. Clone repository on server
git clone https://github.com/your-repo/printwave-core.git
cd printwave-core

# 2. Set up production environment
cp .env.example .env.prod
nano .env.prod  # Edit with production values

# 3. Deploy production stack
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# 4. Check status
docker-compose ps

# 5. View logs
docker-compose logs -f printwave-core
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

Would you like me to create the production docker-compose override file for this approach?
