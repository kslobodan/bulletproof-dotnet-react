# API Testing Commands

Quick reference for testing API endpoints manually.

## Prerequisites

1. **Start PostgreSQL**: `docker compose up -d`
2. **Start API**: `cd src/BookingSystem.API; dotnet run`
3. **Verify API running**: Should see "Now listening on: http://localhost:5036"

---

## Day 2: Multi-Tenant Middleware Tests

### Test Tenant Resolution Middleware

**Test 1: Valid X-Tenant-Id header → 200 OK**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/tenant/info" `
  -Headers @{ "X-Tenant-Id" = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" }

# Expected: { tenantId: "aaaa...", isResolved: true, message: "..." }
```

**Test 2: Missing X-Tenant-Id header → 400 Bad Request**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/tenant/info"

# Expected: { statusCode: 400, message: "X-Tenant-Id header is required." }
```

**Test 3: Invalid GUID format → 400 Bad Request**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/tenant/info" `
  -Headers @{ "X-Tenant-Id" = "not-a-valid-guid" }

# Expected: { statusCode: 400, message: "Invalid X-Tenant-Id header format..." }
```

**Test 4: Swagger endpoint bypasses tenant check**

```powershell
# Open in browser:
http://localhost:5036/swagger

# Should load without requiring X-Tenant-Id header
```

---

## Day 3: Authentication Tests

### Register Tenant

```powershell
$registerResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/register-tenant" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{
    "tenantName": "Acme Corp",
    "email": "admin@acme.com",
    "password": "Admin1234",
    "firstName": "Alice",
    "lastName": "Admin",
    "plan": "Pro"
  }'

# Save token for later:
$token = $registerResponse.token
$tenantId = $registerResponse.tenantId
```

### Login

```powershell
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{
    "email": "admin@acme.com",
    "password": "Admin1234"
  }'

$token = $loginResponse.authResult.token
$tenantId = $loginResponse.authResult.tenantId
$refreshToken = $loginResponse.authResult.refreshToken
```

### Refresh Token

**Note**: For comprehensive refresh token testing (token rotation, revocation, security), see **Day 7: Advanced Backend Features Tests** section below.

**Quick test**:

```powershell
$refreshResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/refresh" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{
    \"refreshToken\": \"$refreshToken\"
  }"

$token = $refreshResponse.authResult.token
$refreshToken = $refreshResponse.authResult.refreshToken
```

---

## Day 4: Resources CRUD Tests

### Create Resource

```powershell
$resource = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body '{
    "name": "Conference Room A",
    "resourceType": "MeetingRoom",
    "capacity": 10,
    "description": "Main conference room"
  }'

$resourceId = $resource.id
```

### Get All Resources (Paginated)

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources?pageNumber=1&pageSize=10" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Get Resource by ID

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$resourceId" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Update Resource

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$resourceId" `
  -Method PUT `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body '{
    "name": "Conference Room A (Updated)",
    "capacity": 12,
    "description": "Updated description"
  }'
```

### Delete Resource

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$resourceId" `
  -Method DELETE `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

---

## Day 5: Bookings CRUD Tests

### Create Booking

```powershell
$booking = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body "{
    \"resourceId\": \"$resourceId\",
    \"title\": \"Team Meeting\",
    \"description\": \"Weekly sync\",
    \"startTime\": \"2026-04-20T10:00:00Z\",
    \"endTime\": \"2026-04-20T11:00:00Z\",
    \"notes\": \"Bring laptops\"
  }"

$bookingId = $booking.id
```

### Get All Bookings (with filters)

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?status=Pending&pageNumber=1&pageSize=10" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Cancel Booking

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings/$bookingId/cancel" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Confirm Booking (Admin/Manager only)

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings/$bookingId/confirm" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

---

## Day 6: Audit Logging Tests

### Verify AuditLogs Table Created

```powershell
# Check table structure
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "\d AuditLogs"

# Expected columns: Id, TenantId, EntityName, EntityId, Action, OldValues, NewValues, UserId, Timestamp, IpAddress, Reason
```

### Test Audit Logging with CRUD Operations

**Step 1: Create a resource (triggers audit log)**

```powershell
$resource = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body '{
    "name": "Audit Test Room",
    "resourceType": "MeetingRoom",
    "capacity": 5,
    "description": "Room for audit testing"
  }'

$auditResourceId = $resource.id
```

**Step 2: Update the resource (triggers audit log)**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$auditResourceId" `
  -Method PUT `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body '{
    "name": "Updated Audit Test Room",
    "capacity": 10,
    "description": "Updated for audit test"
  }'
```

**Step 3: Create and cancel a booking (triggers 2 audit logs)**

```powershell
# Create booking
$auditBooking = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body "{
    \"resourceId\": \"$auditResourceId\",
    \"title\": \"Audit Test Booking\",
    \"description\": \"Testing audit logs\",
    \"startTime\": \"2026-08-20T10:00:00Z\",
    \"endTime\": \"2026-08-20T11:00:00Z\"
  }"

$auditBookingId = $auditBooking.id

# Cancel booking
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings/$auditBookingId/cancel" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Step 4: Delete the resource (triggers audit log)**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$auditResourceId" `
  -Method DELETE `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Verify Audit Data in Database

**View all recent audit logs**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT EntityName, EntityId, Action, Timestamp FROM AuditLogs ORDER BY Timestamp DESC LIMIT 10;"
```

**View audit logs for specific entity**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT Action, EntityName, UserId, Timestamp FROM AuditLogs WHERE EntityName = 'Resource' ORDER BY Timestamp DESC;"
```

**View audit logs with JSON values**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT Action, EntityName, NewValues, Timestamp FROM AuditLogs WHERE Action = 'Create' ORDER BY Timestamp DESC LIMIT 5;"
```

**Verify all action types are logged**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT Action, COUNT(*) as Count FROM AuditLogs GROUP BY Action ORDER BY Action;"

# Expected output should include: Cancel, Create, Delete, Update (and possibly Confirm)
```

### Get Audit Logs via API (TenantAdmin only)

**Get paginated audit logs**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auditlogs?pageNumber=1&pageSize=10" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Filter by entity name**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auditlogs?entityName=Resource&pageNumber=1&pageSize=10" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Filter by action type**

```powershell
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auditlogs?action=Create" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Filter by date range**

```powershell
$fromDate = (Get-Date).AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ")
$toDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")

Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auditlogs?fromDate=$fromDate&toDate=$toDate" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

---

## Day 7: Advanced Backend Features Tests

### Refresh Token Flow (Token Rotation)

**Step 1: Login and get refresh token**

```powershell
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{
    "email": "admin@acme.com",
    "password": "Admin1234"
  }'

$token = $loginResponse.authResult.token
$refreshToken = $loginResponse.authResult.refreshToken
$tenantId = $loginResponse.authResult.tenantId
```

**Step 2: Use refresh token to get new tokens**

```powershell
$refreshResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/refresh" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{
    \"refreshToken\": \"$refreshToken\"
  }"

$newToken = $refreshResponse.authResult.token
$newRefreshToken = $refreshResponse.authResult.refreshToken

# Verify tokens are different (token rotation)
Write-Host "Old token != New token: $($token -ne $newToken)"
Write-Host "Old refresh != New refresh: $($refreshToken -ne $newRefreshToken)"
```

**Step 3: Verify old refresh token is revoked (should fail)**

```powershell
try {
    Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/refresh" `
      -Method POST `
      -ContentType "application/json" `
      -Body "{\"refreshToken\": \"$refreshToken\"}"
    Write-Host "❌ ERROR: Old token should be rejected!"
} catch {
    Write-Host "✅ Old token correctly rejected: $($_.Exception.Response.StatusCode)"
}
```

**Step 4: Verify database token rotation**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "
  SELECT
    LEFT(Token, 20) as TokenPrefix,
    RevokedAt IS NOT NULL as IsRevoked,
    LEFT(ReplacedByToken, 20) as ReplacedByPrefix,
    CreatedAt
  FROM RefreshTokens
  ORDER BY CreatedAt DESC
  LIMIT 5;"
```

### Soft Delete Pattern

**Create and soft-delete a resource**

```powershell
# Create resource
$resource = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources" `
  -Method POST `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  } `
  -ContentType "application/json" `
  -Body '{
    "name": "Soft Delete Test Room",
    "resourceType": "MeetingRoom",
    "capacity": 5
  }'

$resourceId = $resource.id

# Delete resource (soft delete)
Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources/$resourceId" `
  -Method DELETE `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Verify resource no longer appears in list
$resources = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$deletedResource = $resources.items | Where-Object { $_.id -eq $resourceId }
Write-Host "Resource in list: $($null -ne $deletedResource) (should be False)"
```

**Verify soft delete in database**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "
  SELECT
    Name,
    IsDeleted,
    DeletedAt
  FROM Resources
  WHERE Id = '$resourceId';"

# Expected: IsDeleted = true, DeletedAt = timestamp
```

### Advanced Booking Queries (Filtering & Sorting)

**Filter by status**

```powershell
$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?status=Confirmed" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

Write-Host "Confirmed bookings: $($bookings.items.Count)"
```

**Filter by resource**

```powershell
$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?resourceId=$resourceId" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Filter by date range**

```powershell
$startDate = "2026-08-01T00:00:00Z"
$endDate = "2026-08-31T23:59:59Z"

$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?startDate=$startDate&endDate=$endDate" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Sort by different columns**

```powershell
# Sort by StartTime descending (most recent first)
$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?orderBy=StartTime&descending=true" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# Sort by Title ascending
$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?orderBy=Title&descending=false" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

**Combined filters**

```powershell
$bookings = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings?status=Confirmed&orderBy=StartTime&descending=true&pageSize=20" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }
```

### Booking Statistics Endpoint

**Get overall statistics**

```powershell
$stats = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings/statistics" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

Write-Host "Total Bookings: $($stats.totalBookings)"
Write-Host "Pending: $($stats.pendingBookings)"
Write-Host "Confirmed: $($stats.confirmedBookings)"
Write-Host "Completed: $($stats.completedBookings)"
Write-Host "Cancelled: $($stats.cancelledBookings)"
```

**Filter statistics by date range**

```powershell
$startDate = "2026-01-01T00:00:00Z"
$endDate = "2026-12-31T23:59:59Z"

$stats = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/bookings/statistics?startDate=$startDate&endDate=$endDate" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# View bookings by resource
$stats.bookingsByResource | ConvertTo-Json
```

**Verify statistics with database aggregation**

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "
  SELECT
    Status,
    COUNT(*) as Count
  FROM Bookings
  WHERE TenantId = '$tenantId' AND IsDeleted = false
  GROUP BY Status;"
```

### Rate Limiting Tests

**Test rate limit enforcement (60 requests per minute)**

```powershell
# Make rapid requests to trigger rate limit
$results = @()
for ($i = 1; $i -le 65; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/resources" `
          -Headers @{
            "Authorization" = "Bearer $token"
            "X-Tenant-Id" = $tenantId
          }
        $results += "Request $i : Success"
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $results += "Request $i : $statusCode (Rate Limited)"
    }
}

$results | Select-Object -Last 10
# Expected: Last few requests should show 429 (Too Many Requests)
```

**Check rate limit headers**

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5036/api/v1/resources" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

# View rate limit headers
$response.Headers.'X-Rate-Limit-Limit'
$response.Headers.'X-Rate-Limit-Remaining'
$response.Headers.'X-Rate-Limit-Reset'
```

### Admin Audit Logs Endpoint

**Note**: See "Day 6: Audit Logging Tests" section above for comprehensive audit log testing.

**Quick test - Get paginated audit logs**

```powershell
$auditLogs = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auditlogs?pageNumber=1&pageSize=10" `
  -Headers @{
    "Authorization" = "Bearer $token"
    "X-Tenant-Id" = $tenantId
  }

$auditLogs.items | Select-Object action, entityName, timestamp | Format-Table
```

---

## Database Verification Commands

### List all tables

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "\dt"
```

### Check roles seed data

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT * FROM roles;"
```

### Check tenants

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT id, name, plan FROM tenants;"
```

### Check resources for a tenant

```powershell
docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT id, name, resourcetype, tenantid FROM resources WHERE tenantid = 'YOUR-TENANT-ID-HERE';"
```

---

## Tips

- **Save variables**: Use PowerShell variables (`$token`, `$tenantId`) to avoid copying/pasting GUIDs
- **Error handling**: Add `-ErrorAction Stop` to catch errors
- **View full response**: Use `| ConvertTo-Json -Depth 10` to see nested objects
- **Swagger UI**: Use `http://localhost:5036/swagger` for interactive testing
