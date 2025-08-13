#!/bin/bash

# 🚀 SPOOLR PRODUCTION ENVIRONMENT SETUP
# This script loads production environment variables
# Similar to your ObjectVault setLocalEnv.sh

echo "🔧 Loading Spoolr production environment variables..."

# 🗄️ Database Configuration
export POSTGRES_PASSWORD="super_secure_spoolr_prod_password_2024"

# 🔐 Security Configuration  
export JWT_SECRET="spoolr_production_jwt_secret_key_256_bits_very_secure_2024_version"

# 📧 Email Configuration (replace with your production email)
export EMAIL_USERNAME="noreply@spoolr.com"
export EMAIL_PASSWORD="your_production_email_app_password_here"

# 📦 MinIO Configuration
export MINIO_ACCESS_KEY="spoolr_prod_minio_access_key"
export MINIO_SECRET_KEY="spoolr_prod_minio_secret_key_very_secure"

# 🌐 Application Configuration
export BASE_URL="https://api.spoolr.com"  # Replace with your actual domain

echo "✅ Spoolr production environment variables loaded successfully!"

# Show loaded variables (without sensitive values)
echo "📋 Environment Configuration:"
echo "  - Database: spoolr_production"
echo "  - Email: $EMAIL_USERNAME"
echo "  - Base URL: $BASE_URL"
echo "  - MinIO Bucket: spoolr-production-documents"
