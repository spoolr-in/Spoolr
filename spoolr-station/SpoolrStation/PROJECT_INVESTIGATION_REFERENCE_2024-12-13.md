# SpoolrStation Project Investigation Reference
## Complete Context for Access Denied Issue Resolution

**Date:** December 13, 2024  
**Status:** CRITICAL DISCOVERY - Authentication fix didn't resolve issue, backend never receives streaming URL requests  
**Next Session Focus:** Debug why authenticated DocumentService still gets Access Denied

---

## 🎯 CURRENT PROBLEM SUMMARY

### Primary Issue
SpoolrStation app shows "Access Denied" errors when trying to preview print job documents, despite proper authentication and job ownership.

### Root Cause Analysis
1. **Authentication Issue (FIXED):** DocumentPreviewWindow was creating a new, unauthenticated DocumentService instead of using the authenticated one from MainViewModel
2. **PDF Rendering Issue (PENDING):** PdfiumViewer 2.13.0 library has compatibility issues with .NET 9.0, causing `InvalidOperationException`

---

## 📁 PROJECT STRUCTURE

### Core Application
```
SpoolrStation/
├── MainWindow.xaml/cs              # Main application window
├── App.xaml/cs                     # Application entry point
├── Models/                         # Data models
│   ├── DocumentModels.cs           # Document-related models
│   ├── PrintJobModels.cs          # Print job models
│   └── UserModels.cs              # User/auth models
├── Services/                       # Business logic services
│   ├── AuthService.cs             # Authentication handling
│   ├── DocumentService.cs         # Document streaming/preview
│   ├── PdfDocumentRenderer.cs     # PDF rendering (PdfiumViewer)
│   ├── ServiceProvider.cs         # Dependency injection
│   └── Interfaces/                # Service contracts
├── ViewModels/                     # MVVM ViewModels
│   ├── MainViewModel.cs           # Main window logic
│   └── DocumentPreviewViewModel.cs # Preview window logic
└── Views/                          # Additional windows/views
    └── DocumentPreviewWindow.xaml/cs # Document preview window
```

### Backend (Spoolr Core)
```
spoolr-core/                        # Java Spring Boot backend
├── docker-compose.yml             # Container orchestration
├── src/main/java/com/spoolr/
│   ├── controller/PrintJobController.java
│   ├── service/PrintJobService.java
│   └── config/SecurityConfig.java  # JWT authentication
└── Database: PostgreSQL (port 5432)
```

---

## 🔧 ENVIRONMENT SETUP

### Development Environment
- **OS:** Windows 11
- **IDE:** Visual Studio / VS Code
- **Working Directory:** `D:\Spoolr Project\Spoolr\spoolr-station\SpoolrStation`
- **User:** vdkul

### Technology Stack
- **Frontend:** WPF (.NET 9.0), C#
- **Backend:** Java Spring Boot
- **Database:** PostgreSQL
- **Authentication:** JWT tokens
- **PDF Rendering:** PdfiumViewer 2.13.0 (COMPATIBILITY ISSUE)
- **Document Streaming:** HttpClient with temporary URLs

### Running Services
- **Backend:** `docker-compose up` (http://localhost:8080)
- **Database:** PostgreSQL container (localhost:5432)
- **Frontend:** `dotnet run` from SpoolrStation directory

---

## 🐛 DETAILED ISSUE INVESTIGATION

### 1. Initial Symptoms
```
Error: "Access denied - job may not belong to your vendor account"
- Job 75 assigned to vendor 19
- User logged in as vendor 19
- JWT token valid and not expired
- Backend authentication working (Postman tests successful)
```

### 2. Authentication Flow Analysis

#### Correct Flow
1. User logs in → JWT token generated with vendor ID
2. Token stored in AuthService.CurrentSession
3. MainViewModel creates DocumentService with authenticated AuthService
4. Preview request includes JWT token in Authorization header

#### Previous Bug
1. MainViewModel created authenticated DocumentService ✓
2. DocumentPreviewWindow created NEW DocumentService without authentication ❌
3. Preview requests failed with 403 Forbidden

#### Fix Applied (Session Today)
```csharp
// Before (in DocumentPreviewWindow.xaml.cs)
var documentService = ServiceProvider.GetDocumentService(); // No auth

// After 
public DocumentPreviewWindow(DocumentPrintJob job, IDocumentService? documentService = null)
var docService = documentService ?? ServiceProvider.GetDocumentService(); // Use provided auth service

// MainViewModel.cs - pass authenticated service
var previewWindow = new DocumentPreviewWindow(documentJob, documentService);
```

### 3. Secondary Issue: PDF Rendering Compatibility
```
Library: PdfiumViewer 2.13.0
Target: .NET Framework 4.6.1 - 4.8.1
Current: .NET 9.0-windows
Result: InvalidOperationException during PDF page rendering
```

---

## 📊 DEBUGGING EVIDENCE

### Backend Logs Verification
```bash
# Job creation and assignment working correctly
docker logs spoolr-core --since=2m

# Evidence found:
- Job 75 created successfully
- "Sent job offer for job 75 to vendor 19"
- Job accepted and vendor_id updated correctly
- No requests reaching /jobs/75/file endpoint (app crashing before request)
```

### JWT Token Analysis
```
Token Structure: Valid
- Header: {"typ":"JWT","alg":"HS256"}
- Payload: {"vendorId":19,"role":"VENDOR","exp":...}
- Signature: Valid
- Expiry: 24 hours (not expired)
```

### HTTP Client Testing
```bash
# Postman test successful
GET http://localhost:8080/api/jobs/75/file
Authorization: Bearer [JWT_TOKEN]
Result: 200 OK with streaming URL
```

---

## 🔨 FIXES APPLIED TODAY

### 1. Authentication Fix (COMPLETED)
**Files Modified:**
- `Views/DocumentPreviewWindow.xaml.cs` - Added IDocumentService parameter
- `ViewModels/MainViewModel.cs` - Pass authenticated DocumentService to preview window

**Status:** Built successfully, ready for testing

### 2. Error Handling Enhancement (STARTED)
**Target:** `Services/PdfDocumentRenderer.cs`
**Goal:** Handle PdfiumViewer compatibility gracefully
**Status:** Interrupted, needs completion

---

## 🎯 NEXT SESSION ACTION PLAN

### **CRITICAL Priority 1 - Debug Authentication Issue**

**URGENT**: The authentication fix didn't work. We have confirmed:
- ✅ Authentication in Station app works (logged in, has JWT token)
- ✅ Backend job assignment works (jobs created and assigned to vendor 19)
- ❌ Preview still fails with "Access denied - job may not belong to your vendor account"
- ❌ **NO HTTP requests reaching backend** `/jobs/{id}/file` endpoint

### **Immediate Debug Steps**

1. **Complete Debug Logging** (STARTED, needs completion)
   ```csharp
   // Add to Services/DocumentService.cs GetStreamingUrlAsync method:
   Console.WriteLine($"Making HTTP request to: {request.RequestUri}");
   Console.WriteLine($"Authorization header: {request.Headers.Authorization}");
   Console.WriteLine($"Base address: {_httpClient.BaseAddress}");
   ```

2. **Test with Dual Monitoring**
   ```bash
   # Terminal 1 - Backend logs
   docker logs spoolr-core --follow
   
   # Terminal 2 - Run Station app  
   dotnet run
   
   # Watch both terminals during preview click
   ```

3. **Verify HTTP Request Actually Sent**
   - Check if DocumentService is using correct HttpClient base URL
   - Verify JWT token is attached to request headers
   - Confirm request format matches backend expectations

### Secondary Tasks (Priority 2)
4. **Address PdfiumViewer Compatibility**
   - Research .NET 9.0 compatible PDF libraries
   - Consider: PDFsharp, Syncfusion, or native browser PDF viewing

5. **Code Cleanup**
   - Remove debug Console.WriteLine statements
   - Fix compiler warnings about async methods

### Testing Checklist
- [ ] Login works
- [ ] Job acceptance works
- [ ] Preview button triggers request to backend
- [ ] Backend logs show streaming URL request
- [ ] Document preview window opens
- [ ] PDF documents display (or show graceful error)
- [ ] Non-PDF documents work

---

## 🔍 KEY CODE LOCATIONS

### Authentication Components
```csharp
// Main authentication service
Services/AuthService.cs - CurrentSession property

// Document service factory
Services/ServiceProvider.cs:56 - GetDocumentService(AuthService?)

// Preview trigger
ViewModels/MainViewModel.cs:1481 - ExecutePreviewJob()

// Preview window constructor  
Views/DocumentPreviewWindow.xaml.cs:25 - Constructor with IDocumentService parameter
```

### Backend Endpoints
```java
// Authentication filter
src/main/java/.../config/JwtAuthenticationFilter.java

// File streaming endpoint
src/main/java/.../controller/PrintJobController.java - /jobs/{id}/file

// Job ownership validation
src/main/java/.../service/PrintJobService.java - getJobForVendor()
```

---

## 🐞 DEBUGGING TOOLS & COMMANDS

### Frontend Debugging
```bash
# Build and run with verbose output
dotnet build --verbosity normal
dotnet run --verbosity normal 2>&1

# Kill running process if build locked
tasklist | findstr SpoolrStation
taskkill /f /pid [PID]
```

### Backend Debugging
```bash
# Real-time backend logs
docker logs spoolr-core --follow

# Recent logs only
docker logs spoolr-core --since=2m

# Service status
docker-compose ps
```

### Database Debugging
```sql
-- Check job-vendor assignments
SELECT id, vendor_id, status, original_file_name 
FROM print_jobs 
ORDER BY created_at DESC LIMIT 10;

-- Check vendor data
SELECT id, business_name, is_active, is_store_open 
FROM vendors;
```

---

## 📋 KNOWN ISSUES & WORKAROUNDS

### 1. PdfiumViewer Compatibility
**Issue:** Library built for .NET Framework, incompatible with .NET 9.0  
**Symptom:** InvalidOperationException during PDF rendering  
**Workaround:** Graceful error handling, consider library alternatives  

### 2. Build File Locking
**Issue:** dotnet build fails when SpoolrStation.exe is running  
**Solution:** `taskkill /f /pid [PID]` before building

### 3. WebView2 Dependency
**Issue:** DocumentPreviewWindow requires WebView2 runtime  
**Status:** Should be available on development machine

---

## 💡 INVESTIGATION INSIGHTS

### What We Confirmed
1. Backend authentication and job assignment work correctly
2. JWT tokens are properly formatted and valid
3. Problem was in Station app client-side authentication
4. Job-vendor relationships are properly maintained in database

### What We Learned
1. Service provider pattern can create authentication issues if not careful
2. PDF rendering libraries may have framework compatibility issues
3. Debugging distributed systems requires checking both client and server logs
4. WPF preview windows need careful dependency injection

---

## 📞 SUPPORT CONTACTS & RESOURCES

### Documentation
- PdfiumViewer: https://github.com/pvginkel/PdfiumViewer
- .NET 9.0 Migration: https://docs.microsoft.com/en-us/dotnet/core/compatibility/
- WPF WebView2: https://docs.microsoft.com/en-us/microsoft-edge/webview2/

### Alternatives to Consider
- **PDF Libraries:** PDFsharp, iText, Syncfusion PDF Viewer
- **Document Preview:** Browser-based PDF.js, native Edge WebView2 PDF support

---

## 🏁 SESSION SUMMARY

### **Session 1 Progress (Earlier)**
- ✅ Identified authentication bug in DocumentPreviewWindow
- ✅ Applied fix to pass authenticated DocumentService  
- ✅ Built successfully with authentication improvements
- ✅ Started PDF compatibility error handling

### **Session 2 Progress (Current)**
- ✅ Completed PDF error handling with better compatibility error messages
- ✅ Successfully built and tested the authentication fix
- ✅ **CRITICAL DISCOVERY**: Authentication fix did NOT solve the problem
- ✅ Tested with real job (Job 78) and captured detailed error message
- ✅ Confirmed backend services are running and job assignment works
- ✅ **MAJOR INSIGHT**: Backend logs show NO requests to `/jobs/{id}/file` endpoint
- ✅ Started adding comprehensive debug logging to DocumentService

### **Current Status After Testing**
**Issue Confirmed**: Despite authentication fix, still getting "Access denied - job 78 may not belong to your vendor account"

**Error Details from User Testing:**
```
Authentication Status: Logged In ✓
Business: Guruprasad Zerox ✓  
Has JWT Token: Yes ✓
Error: Failed to download document: Could not get streaming URL: 
Access denied - job 78 may not belong to your vendor account
Exception Type: InvalidOperationException
Location: MainViewModel.ExecutePreviewJob() line 1506
```

**Backend Evidence:**
- Jobs 73, 74, 75, 76, 78 created and assigned to vendor 19 ✓
- Job offers sent and accepted successfully ✓
- Station app connects and authenticates ✓
- **CRITICAL**: NO `/jobs/{id}/file` requests appearing in backend logs ❌

**Root Cause Analysis:**
The authentication fix was correctly implemented, but the issue persists because:
1. Either the request isn't reaching the backend at all
2. The request is being rejected by authentication middleware before logging
3. There's a different authentication context issue we haven't identified

### **Files Modified This Session:**
- `Services/PdfDocumentRenderer.cs` - Added InvalidOperationException handling for .NET 9.0 compatibility
- `Services/DocumentService.cs` - Started adding comprehensive debug logging (partial)

### **Next Session Priorities (CRITICAL):**
1. **IMMEDIATE**: Complete debug logging in DocumentService to trace exact request flow
2. **DEBUG**: Add Console.WriteLine statements to track where the request fails
3. **TEST**: Monitor backend logs in real-time during preview request
4. **INVESTIGATE**: Check if there's a second DocumentService being created somewhere
5. **VERIFY**: Ensure JWT token is being sent in the actual HTTP request headers

### **Debugging Strategy for Tomorrow:**
```bash
# Terminal 1 - Monitor backend logs
docker logs spoolr-core --follow

# Terminal 2 - Run Station app with debug output
dotnet run

# Test sequence:
1. Login to Station app
2. Accept a job  
3. Click preview
4. Watch both terminal outputs
5. Confirm if HTTP request reaches backend
```

---

## 📝 CODE CHANGES STATUS

### **Completed Changes**
```
✅ Views/DocumentPreviewWindow.xaml.cs:25
   - Added IDocumentService parameter to constructor
   - Uses provided authenticated DocumentService
   
✅ ViewModels/MainViewModel.cs:1526
   - Passes authenticated DocumentService to preview window
   
✅ Services/PdfDocumentRenderer.cs:51-55
   - Added InvalidOperationException handling for .NET 9.0 compatibility
   - Better error messages for PDF rendering failures
```

### **Partially Applied Changes (NEEDS COMPLETION)**
```
🔄 Services/DocumentService.cs:102-103, 113-120
   - STARTED: Debug logging in GetStreamingUrlAsync methods
   - STATUS: Only Console.WriteLine added, needs completion
   - TODO: Add comprehensive request/response logging
```

### **Build Status**
- ✅ **Last Successful Build**: All changes compile without errors
- ✅ **Runtime Status**: Station app runs and connects to backend
- ❌ **Preview Functionality**: Still failing with access denied error

### **Exact Error Location**
```
File: MainViewModel.cs
Method: ExecutePreviewJob(AcceptedJobModel? job)
Line: 1506 (approximately)
Error: InvalidOperationException - "Failed to download document: Could not get streaming URL: Access denied - job 78 may not belong to your vendor account"
```

---

## 🔍 TOMORROW'S DEBUGGING CHECKLIST

**Before Starting:**
- [ ] Ensure backend services running: `docker ps`
- [ ] Station app not running (to avoid build conflicts)

**Step 1: Complete Debug Logging**
- [ ] Finish adding Console.WriteLine to DocumentService.cs
- [ ] Add HTTP request details logging
- [ ] Add JWT token content logging (without exposing sensitive data)

**Step 2: Test Authentication Flow**  
- [ ] Terminal 1: `docker logs spoolr-core --follow`
- [ ] Terminal 2: `dotnet run`
- [ ] Login → Accept Job → Click Preview
- [ ] Verify if HTTP request appears in backend logs

**Step 3: Root Cause Analysis**
- [ ] If NO request in backend: Issue is in Station app HTTP client setup
- [ ] If request appears but fails: Issue is in backend authentication/authorization
- [ ] Document exact point of failure

**Expected Outcome:**
By end of next session, we should know definitively whether the HTTP request is reaching the backend or failing in the Station app.

---

## 🔄 **SESSION COMPLETION SUMMARY**

### **Key Additions Made Today:**

1. **Session 2 Progress** - Detailed what we accomplished:
   - ✅ Completed PDF error handling 
   - ✅ Tested the authentication fix (but discovered it didn't work)
   - ✅ **CRITICAL DISCOVERY**: Backend never receives the streaming URL requests
   - ✅ Captured exact error details from Job 78

2. **Current Status After Testing** - The real situation:
   - Authentication works (logged in, has JWT token)
   - Backend job assignment works perfectly
   - **CRITICAL**: NO `/jobs/{id}/file` requests appearing in backend logs
   - Error still occurs at MainViewModel.cs line 1506

3. **Code Changes Status** - What's been completed vs. what's partial:
   - ✅ Authentication fix applied (but didn't solve the issue)
   - ✅ PDF compatibility improvements  
   - 🔄 Debug logging partially started (needs completion)

4. **Tomorrow's Debugging Checklist** - Clear action plan:
   - Complete debug logging in DocumentService
   - Monitor backend logs while testing
   - Determine if HTTP requests are reaching backend at all

### **Critical Insight for Tomorrow:**
The authentication fix we implemented was technically correct, but the core issue is that **HTTP requests aren't reaching the backend at all**. This suggests the problem is in the Station app's HTTP client setup, not the authentication logic.

## 📋 **Ready for Tomorrow:**
- ✅ Complete context preserved
- ✅ Exact error details documented  
- ✅ Clear debugging strategy outlined
- ✅ Build status and file locations noted
- ✅ Backend evidence captured

**The reference document now contains everything needed to continue the investigation efficiently tomorrow!** 🚀

---

*This reference document contains complete context for continuing the SpoolrStation access denied investigation. All debugging steps, code changes, and environment details are preserved for seamless continuation.*

---

## 📌 Update (2025-09-16): Exact API used by Preview button and call flow

This section documents the precise API and code paths invoked when a vendor clicks the Preview button in the SpoolrStation app.

### API Endpoint (Spoolr Core)
- Method: GET
- Path: /api/jobs/{jobId}/file
- Auth: Authorization: Bearer {jwt_token}
- Purpose: Returns a 30-minute presigned streaming URL for the job file (hosted in MinIO)

Backend mapping:
- Controller: `src/main/java/com/spoolr/core/controller/PrintJobController.java` → `getJobFileUrl()` (lines ~560-590)
- Service: `src/main/java/com/spoolr/core/service/PrintJobService.java` → `getJobFileStreamingUrl()` (lines ~622-625)
- Storage: `src/main/java/com/spoolr/core/service/FileStorageService.java` → `getStreamingUrlForPrinting()` (lines ~112-127)

Response shape example:
```json path=null start=null
{
  "success": true,
  "streamingUrl": "https://minio/bucket/2024/01/15/job_uuid_filename.pdf?X-Amz-...",
  "expiryMinutes": 30,
  "instructions": "Use this URL to stream the file directly to your printer.",
  "jobId": 12345
}
```

### Station App Call Flow (on Preview)
1) ViewModel triggers preview
- File: `SpoolrStation/ViewModels/MainViewModel.cs`
- Method: `ExecutePreviewJob(...)` (around lines 1481-1562)
- Action: Gets authenticated `DocumentService` and calls `GetDocumentPreviewAsync(documentJob)`

2) DocumentService orchestrates the preview
- File: `SpoolrStation/Services/DocumentService.cs`
- Method: `GetDocumentPreviewAsync(DocumentPrintJob job)` (around lines 319-344)
  - If `job.StreamingUrl` is empty, calls `GetStreamingUrlAsync(job.JobId)`
  - Then calls `StreamDocumentToMemoryAsync(job.StreamingUrl, job.JobId)`

3) DocumentService requests streaming URL from Core API
- File: `SpoolrStation/Services/DocumentService.cs`
- Method: `GetStreamingUrlAsync(long jobId)` → internal overload with retry guard (around lines 101-240)
  - Builds `HttpRequestMessage(HttpMethod.Get, $"/jobs/{jobId}/file")`
  - Sets `Authorization: Bearer {JwtToken}` from `_authService.CurrentSession.JwtToken`
  - Sends request via `_httpClient.SendAsync(request)`
  - On 403 (Forbidden), attempts auto-reassign via `POST /jobs/{jobId}/reassign` once, then retries

4) After URL retrieval, the document is streamed to memory
- File: `SpoolrStation/Services/DocumentService.cs`
- Method: `StreamDocumentToMemoryAsync(string streamingUrl, long jobId)` (around lines 251-312)
  - GET to the presigned MinIO URL
  - Caches bytes in memory and returns `DocumentStreamResult`

### Key insight related to current issue
- The failure is occurring during the Station app call to GET `/api/jobs/{jobId}/file` in `GetStreamingUrlAsync(...)` before the MinIO streaming step.
- Backend logs show no hits to this endpoint during failed preview attempts, indicating the request may be rejected/blocked client-side or by auth middleware before controller logging, or not sent due to client configuration/token issues.

### Verification checklist for this flow
- Confirm `_httpClient.BaseAddress` points to Spoolr Core (e.g., `http://localhost:8080/api`).
- Ensure `Authorization: Bearer {jwt}` header is attached (token present and unexpired).
- Monitor backend logs for `/api/jobs/{id}/file` during preview.
- Validate job-vendor ownership; reassignment helper exists at `POST /api/jobs/{jobId}/reassign`.
