# Test script to manually reassign job to current vendor

# You need to replace this with a fresh JWT token from the Station app
$jwtToken = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJHdXJ1cHJhc2FkIFplcm94Iiwicm9sZSI6IlZFTkRPUiIsInZlbmRvcklkIjoxLCJpYXQiOjE3MzcwNjY0MjUsImV4cCI6MTczNzE1MjgyNX0.kL1jWWcHZ0fUIXWTXo_xeeFhU6Bwmy8vV3w_tQJ_lF0"
$jobId = 71
$baseUrl = "http://localhost:8080"

Write-Host "Testing job reassignment for job $jobId..." -ForegroundColor Yellow

try {
    # Test the reassignment API
    $headers = @{
        "Authorization" = "Bearer $jwtToken"
        "Content-Type" = "application/json"
    }
    
    $reassignUrl = "$baseUrl/api/jobs/$jobId/reassign"
    Write-Host "POST $reassignUrl" -ForegroundColor Cyan
    
    $response = Invoke-WebRequest -Uri $reassignUrl -Method POST -Headers $headers -ErrorAction Stop
    
    Write-Host "Reassignment successful!" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Green
    
    # Now test the streaming URL
    Write-Host "`nTesting streaming URL..." -ForegroundColor Yellow
    $streamingUrl = "$baseUrl/api/jobs/$jobId/file"
    Write-Host "GET $streamingUrl" -ForegroundColor Cyan
    
    $streamResponse = Invoke-WebRequest -Uri $streamingUrl -Method GET -Headers $headers -ErrorAction Stop
    
    Write-Host "Streaming URL request successful!" -ForegroundColor Green
    Write-Host "Status: $($streamResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($streamResponse.Content)" -ForegroundColor Green
    
} catch {
    Write-Host "Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Error response: $responseBody" -ForegroundColor Red
    }
}