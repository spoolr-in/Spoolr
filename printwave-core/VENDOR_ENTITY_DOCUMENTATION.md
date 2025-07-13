# Vendor Entity Documentation - PrintWave Platform

## Overview

The Vendor entity represents print shop vendors within the PrintWave platform. This document outlines the comprehensive structure and fields required for the Vendor entity to support the "Uber for Printing" business model. This is purely documentation and does not affect the current codebase.

## Entity Purpose

The Vendor entity serves as the foundation for:
- Print shop registration and management
- Station app authentication and activation
- Printer capability tracking
- Order matching and assignment
- Business operations and compliance

## Complete Entity Structure

### 1. Basic Identity Fields
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `id` | Long | Primary key, auto-generated | ✓ | ✓ |
| `email` | String | Business email for login and communications | ✓ | ✓ |
| `password` | String | Hashed password for authentication | ✓ | ✗ |
| `businessName` | String | Print shop/business name | ✓ | ✗ |
| `contactPersonName` | String | Owner/manager name | ✓ | ✗ |

### 2. Business Information
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `businessRegistrationNumber` | String | Legal registration number | ✓ | ✓ |
| `taxId` | String | Tax identification number | ✓ | ✓ |
| `businessType` | Enum | PRINT_SHOP, COPY_CENTER, COMMERCIAL_PRINTER | ✓ | ✗ |
| `businessDescription` | Text | Description of services offered | ✗ | ✗ |
| `websiteUrl` | String | Business website URL | ✗ | ✗ |

### 3. Location & Contact Details
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `address` | String | Full business address | ✓ | ✗ |
| `city` | String | City name | ✓ | ✗ |
| `state` | String | State/province | ✓ | ✗ |
| `zipCode` | String | Postal code | ✓ | ✗ |
| `country` | String | Country | ✓ | ✗ |
| `latitude` | Double | For location-based matching | ✗ | ✗ |
| `longitude` | Double | For location-based matching | ✗ | ✗ |
| `phoneNumber` | String | Business phone | ✓ | ✗ |
| `alternatePhoneNumber` | String | Secondary contact | ✗ | ✗ |

### 4. Operating Information
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `operatingHours` | JSON/String | Business hours (Mon-Sun) | ✓ | ✗ |
| `isOpen24Hours` | Boolean | 24/7 operation flag | ✓ | ✗ |
| `holidaySchedule` | JSON/String | Holiday closures | ✗ | ✗ |
| `maxOrderCapacity` | Integer | Daily order limit | ✓ | ✗ |
| `averageProcessingTime` | Integer | Average job completion time (minutes) | ✗ | ✗ |

### 5. Authentication & Security
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `activationKey` | String | Unique key for Station app setup | ✓ | ✓ |
| `isActivated` | Boolean | Account activation status | ✓ | ✗ |
| `emailVerified` | Boolean | Email verification status | ✓ | ✗ |
| `passwordResetToken` | String | For password reset | ✗ | ✗ |
| `passwordResetExpiry` | LocalDateTime | Token expiry | ✗ | ✗ |
| `lastLoginAt` | LocalDateTime | Last login timestamp | ✗ | ✗ |

### 6. Printer Capabilities
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `printerCapabilities` | JSON | Available printer types and features | ✓ | ✗ |
| `supportedPaperSizes` | JSON | A4, A3, Letter, Legal, etc. | ✓ | ✗ |
| `supportedPaperTypes` | JSON | Plain, Photo, Cardstock, etc. | ✓ | ✗ |
| `colorPrintingAvailable` | Boolean | Color printing capability | ✓ | ✗ |
| `duplexPrintingAvailable` | Boolean | Double-sided printing | ✓ | ✗ |
| `bindingServices` | JSON | Spiral, saddle-stitch, etc. | ✗ | ✗ |
| `laminationServices` | Boolean | Lamination availability | ✗ | ✗ |
| `maximumPrintQuantity` | Integer | Max quantity per job | ✓ | ✗ |

### 7. Service Pricing
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `blackWhitePricePerPage` | BigDecimal | B&W printing cost | ✓ | ✗ |
| `colorPricePerPage` | BigDecimal | Color printing cost | ✓ | ✗ |
| `minimumOrderAmount` | BigDecimal | Minimum order value | ✓ | ✗ |
| `rushOrderSurcharge` | BigDecimal | Express service fee | ✗ | ✗ |
| `deliveryCharges` | BigDecimal | Delivery fee (if applicable) | ✗ | ✗ |
| `currency` | String | Currency code (USD, EUR, etc.) | ✓ | ✗ |

### 8. Business Status & Ratings
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `isActive` | Boolean | Business operational status | ✓ | ✗ |
| `isVerified` | Boolean | Admin verification status | ✓ | ✗ |
| `subscriptionTier` | Enum | BASIC, PREMIUM, ENTERPRISE | ✓ | ✗ |
| `averageRating` | Double | Customer rating average | ✗ | ✗ |
| `totalCompletedOrders` | Integer | Order completion count | ✓ | ✗ |
| `totalReviews` | Integer | Review count | ✓ | ✗ |

### 9. Financial Information
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `bankAccountNumber` | String | For payments (encrypted) | ✓ | ✗ |
| `bankName` | String | Bank details | ✓ | ✗ |
| `accountHolderName` | String | Account holder | ✓ | ✗ |
| `routingNumber` | String | Bank routing number | ✓ | ✗ |
| `taxRate` | Double | Applicable tax rate | ✓ | ✗ |
| `commissionRate` | Double | Platform commission | ✓ | ✗ |

### 10. Compliance & Documentation
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `businessLicense` | String | License document path/URL | ✓ | ✗ |
| `insuranceDetails` | String | Insurance information | ✗ | ✗ |
| `certifications` | JSON | Quality certifications | ✗ | ✗ |
| `complianceStatus` | Enum | COMPLIANT, PENDING, NON_COMPLIANT | ✓ | ✗ |

### 11. Operational Preferences
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `autoAcceptOrders` | Boolean | Auto-accept orders flag | ✓ | ✗ |
| `notificationPreferences` | JSON | Email, SMS, Push preferences | ✓ | ✗ |
| `preferredJobTypes` | JSON | Preferred print job types | ✗ | ✗ |
| `blacklistedCustomers` | JSON | Blocked customer IDs | ✗ | ✗ |

### 12. Timestamp Fields
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `createdAt` | LocalDateTime | Account creation | ✓ | ✗ |
| `updatedAt` | LocalDateTime | Last update | ✓ | ✗ |
| `lastActiveAt` | LocalDateTime | Last activity | ✗ | ✗ |
| `verifiedAt` | LocalDateTime | Verification timestamp | ✗ | ✗ |

### 13. Station App Integration
| Field | Type | Description | Required | Unique |
|-------|------|-------------|----------|--------|
| `stationAppVersion` | String | Current app version | ✗ | ✗ |
| `lastSyncAt` | LocalDateTime | Last sync with Station app | ✗ | ✗ |
| `deviceId` | String | Station device identifier | ✗ | ✗ |
| `printerStatus` | JSON | Real-time printer status | ✗ | ✗ |

## JSON Field Structures

### Operating Hours Example
```json
{
  "monday": {"open": "09:00", "close": "18:00"},
  "tuesday": {"open": "09:00", "close": "18:00"},
  "wednesday": {"open": "09:00", "close": "18:00"},
  "thursday": {"open": "09:00", "close": "18:00"},
  "friday": {"open": "09:00", "close": "18:00"},
  "saturday": {"open": "10:00", "close": "16:00"},
  "sunday": {"closed": true}
}
```

### Printer Capabilities Example
```json
{
  "printers": [
    {
      "id": "printer-1",
      "model": "HP LaserJet Pro",
      "type": "LASER",
      "capabilities": ["BW_PRINT", "DUPLEX"],
      "status": "ONLINE"
    },
    {
      "id": "printer-2",
      "model": "Canon PIXMA",
      "type": "INKJET",
      "capabilities": ["COLOR_PRINT", "PHOTO_PRINT"],
      "status": "ONLINE"
    }
  ]
}
```

### Supported Paper Sizes Example
```json
{
  "sizes": [
    {"name": "A4", "width": 210, "height": 297, "unit": "mm"},
    {"name": "A3", "width": 297, "height": 420, "unit": "mm"},
    {"name": "Letter", "width": 8.5, "height": 11, "unit": "in"},
    {"name": "Legal", "width": 8.5, "height": 14, "unit": "in"}
  ]
}
```

### Notification Preferences Example
```json
{
  "email": {
    "enabled": true,
    "events": ["NEW_ORDER", "ORDER_COMPLETED", "PAYMENT_RECEIVED"]
  },
  "sms": {
    "enabled": false,
    "events": []
  },
  "push": {
    "enabled": true,
    "events": ["NEW_ORDER", "URGENT_NOTIFICATION"]
  }
}
```

## Enums Definition

### BusinessType
- `PRINT_SHOP` - Local print shop
- `COPY_CENTER` - Copy and print center
- `COMMERCIAL_PRINTER` - Large commercial printing business
- `OFFICE_SUPPLIES` - Office supply store with printing services
- `LIBRARY` - Library with printing services

### SubscriptionTier
- `BASIC` - Basic features, limited orders
- `PREMIUM` - Enhanced features, higher limits
- `ENTERPRISE` - All features, unlimited orders

### ComplianceStatus
- `COMPLIANT` - Meets all requirements
- `PENDING` - Under review
- `NON_COMPLIANT` - Does not meet requirements

## Relationships

### Future Entity Relationships
- **One-to-Many**: Vendor → PrintJobs
- **One-to-Many**: Vendor → Reviews
- **One-to-Many**: Vendor → Printers (separate entity)
- **One-to-Many**: Vendor → Orders
- **Many-to-Many**: Vendor → ServiceTypes

## Security Considerations

1. **Password**: Always store hashed passwords using BCrypt
2. **Financial Data**: Encrypt sensitive financial information
3. **Personal Data**: Ensure GDPR compliance for personal information
4. **Activation Keys**: Generate secure, unique activation keys
5. **API Security**: Use JWT tokens for Station app authentication

## Implementation Notes

1. **Separate from User Entity**: Vendors are completely separate from User (customer) entities
2. **Station App Integration**: Designed to work with the Station app architecture
3. **Scalability**: Structure supports multiple printers and complex capabilities
4. **Location Services**: Latitude/longitude support for geo-based matching
5. **Flexibility**: JSON fields allow for future expansion without schema changes

## Database Indexes Recommendations

For optimal performance, consider adding indexes on:
- `email` (unique)
- `businessRegistrationNumber` (unique)
- `activationKey` (unique)
- `city`, `state`, `country` (for location searches)
- `isActive`, `isVerified` (for filtering)
- `createdAt`, `updatedAt` (for sorting)

---

*This documentation serves as a blueprint for Phase 2 implementation of the PrintWave platform. It does not affect the current codebase and is purely for planning purposes.*
