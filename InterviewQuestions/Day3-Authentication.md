# Day 3: Authentication & Authorization

[← Back to Index](./README.md) | [← Previous: Day 2](./Day2-Infrastructure.md)

---

## Q: "Why did you choose BCrypt for password hashing?"

**A:** "I chose **BCrypt** (BCrypt.Net-Next package) over basic hashing algorithms because:

**Security Features:**

1. **Adaptive hashing** - Cost factor (work factor) can be increased over time as hardware improves
2. **Built-in salt** - Automatically generates random salt per password (prevents rainbow table attacks)
3. **Slow by design** - Computationally expensive to hash (prevents brute-force attacks)
4. **Industry standard** - Proven track record, used by major platforms

**Why NOT SHA256/MD5:**

- SHA256/MD5 are **too fast** - Attacker can try millions of passwords per second
- No built-in salt - Must implement salting manually (error-prone)
- Not designed for passwords - Designed for file integrity, not authentication

**Implementation:**

```csharp
public class PasswordHasher : IPasswordHasher {
    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

**Security benefits:**

- Even if database is compromised, passwords remain protected
- Each password has unique salt (same password = different hash)
- Cost factor = 10 (adjustable for future hardware improvements)"

---

## Q: "Explain your JWT token implementation."

**A:** "I implemented **JWT (JSON Web Tokens)** for stateless authentication:

**Token Structure:**

```
Header.Payload.Signature
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJ1c2VySWQiOiIxMjMiLCJlbWFpbCI6ImpvaG5AZXhhbXBsZS5jb20ifQ.
SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**Claims stored in token:**

```csharp
new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
new Claim(ClaimTypes.Email, email),
new Claim(ClaimTypes.Role, \"TenantAdmin\"),
new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
```

**Configuration (appsettings.json):**

```json
{
  \"Jwt\": {
    \"SecretKey\": \"MyDevelopmentSecretKeyForJWTTokenGeneration12345\",
    \"Issuer\": \"BookingSystemAPI\",
    \"Audience\": \"BookingSystemClient\",
    \"ExpirationMinutes\": 60
  }
}
```

**Token Validation:**

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = true,           // Check token was issued by our API
    ValidateAudience = true,         // Check token is for our client
    ValidateLifetime = true,         // Check token not expired
    ValidateIssuerSigningKey = true, // Verify signature
    ClockSkew = TimeSpan.Zero        // No tolerance for expired tokens
};
```

**Why JWT over sessions:**

- **Stateless** - No server-side session storage needed (scales horizontally)
- **Self-contained** - All user info in token (no database lookup per request)
- **Cross-domain** - Works across microservices without shared session store
- **Mobile-friendly** - Perfect for mobile apps and SPAs

**Security:**

- Signed with HMAC SHA256 (tamper-proof)
- 60-minute expiration (limits damage if stolen)
- Secret key from configuration (not hardcoded)
- In production, would use Azure Key Vault for secret"

---

## Q: "Why did you set ClockSkew to TimeSpan.Zero?"

**A:** "By default, ASP.NET Core JWT middleware has a **5-minute clock skew tolerance** to account for time differences between servers.

**Default behavior:**

- Token expires at 10:00:00
- Token still accepted until 10:05:00 (5 minutes grace period)

**Why I set ClockSkew = TimeSpan.Zero:**

1. **Stricter security** - Tokens expire exactly when they should
2. **Docker/Cloud environments** - Time sync is reliable (NTP), no need for tolerance
3. **Predictable behavior** - Expiration time means expiration time
4. **Better for testing** - No surprises with \"expired but still works\"

**Configuration:**

```csharp
ClockSkew = TimeSpan.Zero // Removes default 5-minute tolerance
```

**Trade-off:**

- Risk: If servers have time drift, valid tokens might be rejected
- Mitigation: Use NTP synchronization (standard in production)

For this project, strict expiration is worth it for security."

---

## Q: "Explain the difference between RegisterTenant and RegisterUser commands."

**A:** "These are two separate CQRS commands with different semantics:

**RegisterTenant Command:**

- **Purpose**: Create a new organization (tenant) + first admin user
- **Who**: Public endpoint (anyone can register)
- **Requires**: TenantName, Email, Password, FirstName, LastName, Plan
- **Tenant Context**: NOT required (creates new tenant)
- **Process**:
  1. Check if tenant email exists
  2. Create Tenant entity
  3. Hash password
  4. Create User with **direct SQL** (bypasses tenant context)
  5. Assign **TenantAdmin** role
  6. Generate JWT token
  7. Return AuthResult

**RegisterUser Command:**

- **Purpose**: Add user to existing tenant
- **Who**: Authenticated admin or public (depends on authorization)
- **Requires**: Email, Password, FirstName, LastName, Roles
- **Tenant Context**: REQUIRED (X-Tenant-Id header)
- **Process**:
  1. Validate tenant context is set
  2. Check if user email exists in tenant
  3. Hash password
  4. Create User via **UserRepository** (uses tenant context)
  5. Assign specified roles
  6. Generate JWT token
  7. Return AuthResult

**Key Differences:**

| Aspect               | RegisterTenant          | RegisterUser                  |
| -------------------- | ----------------------- | ----------------------------- |
| **Creates**          | Tenant + User           | User only                     |
| **X-Tenant-Id**      | Not required            | Required                      |
| **User Creation**    | Direct SQL (no context) | Via Repository (with context) |
| **Default Role**     | TenantAdmin             | User                          |
| **Email Uniqueness** | Global                  | Per-tenant                    |

**Why direct SQL for RegisterTenant?**

Because TenantContext isn't set yet (we're creating the tenant), so UserRepository's automatic tenant filtering would fail. Direct SQL bypasses this."

---

## Q: "How does the Login flow work?"

**A:** "The login flow uses email as the identifier for BOTH tenant and user:

**Step-by-step process:**

1. **Client sends:** `POST /api/v1/auth/login` with `{ email, password }`

2. **Find Tenant by Email:**

   ```csharp
   var tenant = await _tenantRepository.GetByEmailAsync(request.Email);
   ```

   - Assumption: Tenant was registered with same email as admin user
   - If tenant not found → \"Invalid email or password\" (generic error)

3. **Find User within Tenant:**

   ```csharp
   var user = await connection.QuerySingleOrDefaultAsync<dynamic>(
       \"SELECT * FROM Users WHERE Email = @Email AND TenantId = @TenantId\",
       new { Email = request.Email, TenantId = tenant.Id }
   );
   ```

   - If user not found → \"Invalid email or password\"

4. **Check if User is Active:**

   ```csharp
   if (!user.IsActive) {
       throw new UnauthorizedAccessException(\"User account is inactive\");
   }
   ```

5. **Verify Password:**

   ```csharp
   var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
   ```

   - BCrypt constant-time comparison (prevents timing attacks)

6. **Get User Roles:**

   ```csharp
   SELECT r.Name FROM UserRoles ur
   INNER JOIN Roles r ON ur.RoleId = r.Id
   WHERE ur.UserId = @UserId AND ur.TenantId = @TenantId
   ```

7. **Generate JWT Token:**

   ```csharp
   var token = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);
   ```

8. **Return AuthResult:**
   ```json
   {
     \"authResult\": {
       \"token\": \"eyJhbGci...\",
       \"userId\": \"123...\",
       \"email\": \"john@example.com\",
       \"roles\": [\"TenantAdmin\"],
       \"tenantId\": \"abc...\",
       \"tenantName\": \"Acme Corp\"
     },
     \"message\": \"Login successful\"
   }
   ```

**Security Features:**

- **Generic error messages** - \"Invalid email or password\" prevents user enumeration
- **Constant-time password verification** - Prevents timing attacks
- **Active check** - Supports account suspension
- **Tenant isolation** - User lookup within specific tenant only
- **No X-Tenant-Id required** - Email automatically resolves tenant"

---

## Q: "Explain your authorization policies."

**A:** "I implemented **role-based authorization** with a default FallbackPolicy:

**1. FallbackPolicy (Global Default):**

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
```

- **Applies to all endpoints by default**
- Requires valid JWT token
- Controllers/endpoints must explicitly opt-out with `[AllowAnonymous]`

**2. Named Role-Based Policies:**

```csharp
options.AddPolicy(\"AdminOnly\", policy =>
    policy.RequireRole(\"TenantAdmin\"));

options.AddPolicy(\"ManagerOrAdmin\", policy =>
    policy.RequireRole(\"TenantAdmin\", \"Manager\"));

options.AddPolicy(\"AllUsers\", policy =>
    policy.RequireRole(\"TenantAdmin\", \"Manager\", \"User\"));
```

**3. Controller-Level Protection:**

```csharp
[ApiController]
[Authorize]  // Requires authentication (from FallbackPolicy)
public class UsersController : ControllerBase { ... }
```

**4. Public Endpoints:**

```csharp
[ApiController]
[AllowAnonymous]  // Bypasses FallbackPolicy
public class AuthController : ControllerBase { ... }
```

**Usage Examples:**

```csharp
// Only TenantAdmin can delete users
[HttpDelete(\"{id}\")]
[Authorize(Policy = \"AdminOnly\")]
public async Task<IActionResult> DeleteUser(Guid id) { ... }

// Managers and Admins can approve bookings
[HttpPost(\"approve\")]
[Authorize(Policy = \"ManagerOrAdmin\")]
public async Task<IActionResult> ApproveBooking() { ... }

// All authenticated users can view their profile
[HttpGet(\"my-profile\")]
[Authorize(Policy = \"AllUsers\")]
public async Task<IActionResult> GetMyProfile() { ... }
```

**Why FallbackPolicy?**

- **Secure by default** - Forget to add [Authorize]? Still protected
- **Explicit opt-out** - Must consciously allow anonymous access
- **Prevents mistakes** - Can't accidentally expose sensitive endpoints

**Roles in JWT:**

```csharp
new Claim(ClaimTypes.Role, \"TenantAdmin\")
```

Middleware automatically populates `User.IsInRole(\"TenantAdmin\")` from token claims."

---

## Q: "How do you prevent security vulnerabilities in authentication?"

**A:** "I implemented multiple security layers:

**1. Password Security:**

- **BCrypt hashing** with built-in salt (prevents rainbow tables)
- **Password complexity validation**:
  - Minimum 8 characters
  - At least 1 uppercase
  - At least 1 lowercase
  - At least 1 number
- **No password in logs** - Only hashed values stored

**2. SQL Injection Prevention:**

- **Parameterized queries** everywhere:
  ```csharp
  \"SELECT * FROM Users WHERE Email = @Email\"  // Safe
  ```
- Dapper automatically escapes parameters

**3. Authentication Security:**

- **Generic error messages**: \"Invalid email or password\"
  - Prevents username enumeration attacks
  - Attacker can't tell if email exists
- **Constant-time password comparison** (BCrypt.Verify)
  - Prevents timing attacks
- **Account status check**: Reject inactive users

**4. JWT Security:**

- **HMAC SHA256 signature** - Prevents token tampering
- **Short expiration** - 60 minutes (limits damage if stolen)
- **Token validation** on every request:
  - ValidateIssuer, ValidateAudience, ValidateLifetime
  - ValidateIssuerSigningKey
- **No sensitive data in token** - Only userId, email, roles
- **JTI claim** - Unique token ID (supports revocation if needed)

**5. Authorization Security:**

- **FallbackPolicy** - All endpoints protected by default
- **Role-based access** - TenantAdmin, Manager, User roles
- **[AllowAnonymous]** - Explicit for public endpoints only

**6. Configuration Security:**

- **Secret key externalized** - appsettings, not hardcoded
- **Different keys** for Dev vs Production
- **Environment variables** in production
- **Production placeholder** - Forces explicit configuration

**7. Multi-Tenant Security:**

- **Tenant isolation** in user lookup
- **Email uniqueness per tenant** - Not global
- **TenantId in token** - For audit logging

**8. HTTPS Enforcement:**

- `app.UseHttpsRedirection()` - All traffic encrypted
- Tokens never sent over HTTP

**What I'd add for production:**

- **Rate limiting** - Prevent brute-force login attempts
- **Account lockout** - After N failed attempts
- **2FA/MFA** - Time-based OTP for sensitive operations
- **Refresh tokens** - Long-lived refresh, short-lived access
- **Token revocation** - Blacklist stolen tokens
- **Audit logging** - Track all login attempts
- **CORS** - Restrict API access to known domains"

---

## Q: "Why use CQRS for authentication commands?"

**A:** "I used **CQRS (Command Query Responsibility Segregation)** with MediatR for authentication to:

**Benefits:**

1. **Separation of Concerns:**

   ```
   Controller → Command → Handler → Services/Repositories
   ```

   - Controller: HTTP layer (minimal logic)
   - Command: Data container (what to do)
   - Handler: Business logic (how to do it)

2. **Reusability:**
   - Same RegisterTenantCommand can be triggered from:
     - API endpoint
     - Background job
     - Admin console
     - Migration script

3. **Validation Pipeline:**

   ```
   Request → MediatR → FluentValidation → Handler
   ```

   - Validators run automatically before handler
   - Fail fast if invalid (no database calls)
   - Centralized validation logic

4. **Testability:**
   - Test handlers in isolation
   - Mock repositories easily
   - No HTTP context required

5. **Middleware Support:**
   - MediatR pipeline behaviors:
     - Logging (before/after every command)
     - Performance monitoring
     - Transaction management
     - Caching

**Example Flow:**

```csharp
// 1. Controller (thin orchestrator)
[HttpPost(\"register-tenant\")]
public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request) {
    var command = new RegisterTenantCommand { ... };
    var response = await _mediator.Send(command);
    return CreatedAtAction(nameof(RegisterTenant), response);
}

// 2. Command (data container)
public class RegisterTenantCommand : IRequest<RegisterTenantResponse> {
    public string Email { get; set; }
    public string Password { get; set; }
}

// 3. Validator (runs automatically)
public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand> {
    public RegisterTenantCommandValidator() {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// 4. Handler (business logic)
public class RegisterTenantCommandHandler : IRequestHandler<...> {
    public async Task<RegisterTenantResponse> Handle(...) {
        // Create tenant, user, assign roles, generate token
    }
}
```

**Alternative (without CQRS):**

- All logic in controller → Fat controllers (anti-pattern)
- Hard to test (requires HTTP context)
- No reusability
- Validation scattered everywhere

**CQRS keeps controllers thin and handlers focused on one responsibility.**"

---

## Q: "How would you implement refresh tokens?"

**A:** "Currently using only **access tokens** (60-minute expiration). For production, I'd add **refresh tokens**:

**Current flow:**

1. Login → Get access token (60min)
2. Token expires → Must login again

**Refresh token flow:**

1. Login → Get access token (15min) + refresh token (7 days)
2. Access expires → Use refresh token to get new access token
3. Refresh expires → Must login again

**Implementation:**

**1. Database Table:**

```sql
CREATE TABLE RefreshTokens (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    Token VARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    RevokedAt TIMESTAMP NULL,
    ReplacedByToken VARCHAR(500) NULL
);
```

**2. Generate Refresh Token:**

```csharp
public class JwtTokenService : IJwtTokenService {
    public string GenerateRefreshToken() {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
```

**3. Store After Login:**

```csharp
var refreshToken = _jwtTokenService.GenerateRefreshToken();
await _refreshTokenRepository.AddAsync(new RefreshToken {
    UserId = user.Id,
    Token = refreshToken,
    ExpiresAt = DateTime.UtcNow.AddDays(7)
});
```

**4. New Endpoint:**

```csharp
[HttpPost(\"refresh\")]
[AllowAnonymous]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request) {
    // 1. Validate refresh token exists and not expired
    var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
    if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow) {
        return Unauthorized(\"Invalid refresh token\");
    }

    // 2. Generate new access token
    var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
    var newAccessToken = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);

    // 3. Optionally rotate refresh token
    var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
    refreshToken.RevokedAt = DateTime.UtcNow;
    refreshToken.ReplacedByToken = newRefreshToken;
    await _refreshTokenRepository.UpdateAsync(refreshToken);

    return Ok(new {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    });
}
```

**Security:**

- **Longer refresh expiration** (7 days vs 15 min access)
- **Rotation** - Replace refresh token on each use (prevents reuse)
- **Revocation** - Can invalidate all refresh tokens for user
- **Secure storage** - Refresh tokens in HttpOnly cookies (XSS protection)

**Benefits:**

- **Better UX** - Users stay logged in longer
- **Security** - Short access token limits damage if stolen
- **Revocable** - Can force logout by invalidating refresh tokens

**Current decision:** Kept it simple with 60-min access tokens for MVP. Would add refresh tokens for production."

---

## Q: "What's the difference between [Authorize] and [AllowAnonymous]?"

**A:** "`[Authorize]` and `[AllowAnonymous]` control access to endpoints:

**[Authorize]:**

- **Requires authentication** - Must send valid JWT token
- **Returns 401 Unauthorized** if no token or invalid token
- **Applied at**:
  - Controller level (all actions require auth)
  - Action level (specific endpoint requires auth)

**[AllowAnonymous]:**

- **Bypasses authentication** - No JWT token required
- **Overrides [Authorize]** at controller level
- **For public endpoints** like login, register

**Examples:**

```csharp
// All actions require authentication
[Authorize]
public class UsersController : ControllerBase {
    [HttpGet] // Requires auth
    public async Task<IActionResult> GetAll() { ... }

    [HttpGet(\"{id}\")] // Requires auth
    public async Task<IActionResult> GetById(Guid id) { ... }
}

// All actions public by default
[AllowAnonymous]
public class AuthController : ControllerBase {
    [HttpPost(\"login\")] // Public
    public async Task<IActionResult> Login() { ... }

    [HttpPost(\"register-tenant\")] // Public
    public async Task<IActionResult> RegisterTenant() { ... }
}

// Mixed access
[Authorize]
public class ProductsController : ControllerBase {
    [AllowAnonymous] // Public - anyone can view
    [HttpGet]
    public async Task<IActionResult> GetAll() { ... }

    [HttpPost] // Protected - only authenticated users can create
    public async Task<IActionResult> Create() { ... }
}
```

**FallbackPolicy interaction:**

```csharp
// With FallbackPolicy, ALL endpoints require auth by default
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

// Must explicitly allow anonymous access
[AllowAnonymous]
public class AuthController { ... }

// Without [AllowAnonymous], even endpoints without [Authorize] are protected
```

**My implementation:**

- **FallbackPolicy** = Require auth by default (secure by default)
- **AuthController** = `[AllowAnonymous]` (public registration/login)
- **UsersController, TenantController** = `[Authorize]` (protected resources)

This prevents accidentally exposing protected endpoints."

---

[← Previous: Day 2](./Day2-Infrastructure.md) | [Next: Day 4 →](./Day4-Resources.md) | [Back to Index](./README.md)
