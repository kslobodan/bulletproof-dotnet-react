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

$token = $loginResponse.token
$refreshToken = $loginResponse.refreshToken
```

### Refresh Token

```powershell
$refreshResponse = Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/refresh" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{
    \"refreshToken\": \"$refreshToken\"
  }"

$token = $refreshResponse.token
$refreshToken = $refreshResponse.refreshToken
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
