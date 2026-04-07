# Day 9: Authentication Flow & Tenant Setup (Frontend)

## Q1: "Walk me through your frontend authentication architecture. How does it work end-to-end?"

### Answer

**Architecture Overview**:

```
User Action → Redux Thunk → API Call → Response → Redux State → localStorage → UI Update
```

**Components**:

1. **Redux Slice** (`authSlice.ts`): Centralized auth state management
2. **Token Storage** (`tokenStorage.ts`): localStorage abstraction layer
3. **Axios Interceptors** (`apiClient.ts`): Automatic token injection & refresh
4. **Protected Routes** (`ProtectedRoute.tsx`): Route-level access control
5. **Form Components**: Login, RegisterTenant, RegisterUser (with validation)

**Complete Flow Example (Login)**:

```typescript
// 1. User submits login form
const onSubmit = async (data) => {
  const result = await dispatch(login(data)); // Redux thunk
  if (login.fulfilled.match(result)) {
    navigate("/dashboard"); // Redirect on success
  }
};

// 2. Redux thunk makes API call
export const login = createAsyncThunk<LoginResponse, LoginRequest>(
  "auth/login",
  async (credentials) => {
    const response = await fetch("/api/v1/auth/login", {
      method: "POST",
      body: JSON.stringify(credentials),
    });
    return await response.json();
  },
);

// 3. Redux updates state on success
extraReducers: (builder) => {
  builder.addCase(login.fulfilled, (state, action) => {
    state.user = action.payload.user;
    state.tenant = action.payload.tenant;
    state.accessToken = action.payload.accessToken;
    state.refreshToken = action.payload.refreshToken;
    state.isAuthenticated = true;

    // Also persist to localStorage
    tokenStorage.setAccessToken(action.payload.accessToken);
    tokenStorage.setRefreshToken(action.payload.refreshToken);
    tokenStorage.setUser(action.payload.user);
    tokenStorage.setTenant(action.payload.tenant);
  });
};

// 4. All future API calls automatically include token
// (handled by axios request interceptor)
```

### Key Technical Points

**Why Redux Toolkit + createAsyncThunk?**

- **Less boilerplate**: No need to write action types, action creators, reducers separately
- **Built-in loading states**: `pending`, `fulfilled`, `rejected` handled automatically
- **TypeScript support**: Full type inference for payloads
- **Error handling**: Automatic error serialization

**Multi-tenant Context**:

- Every API request includes `X-Tenant-Id` header (extracted from Redux state)
- Backend uses this header to filter all database queries
- Frontend cannot access other tenant's data (enforced server-side)

---

## Q2: "How does your automatic token refresh mechanism work? Walk me through the code."

### Answer

**Problem**: Access tokens expire (typically 15-60 minutes). Don't want to force user to re-login.

**Solution**: Axios response interceptor detects 401 errors, automatically refreshes token, retries request.

**Implementation** (`apiClient.ts`):

```typescript
let isRefreshing = false;
let failedQueue = [];

apiClient.interceptors.response.use(
  (response) => response, // Pass through successful responses

  async (error) => {
    const originalRequest = error.config;

    // Detect 401 (expired token) and prevent infinite loop
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Queue concurrent requests (prevent duplicate refreshes)
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(() => apiClient(originalRequest));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Call refresh endpoint
        const refreshToken = tokenStorage.getRefreshToken();
        const response = await axios.post("/api/v1/auth/refresh", {
          refreshToken,
        });

        // Update tokens in localStorage
        const { accessToken, refreshToken: newRefreshToken } = response.data;
        tokenStorage.setAccessToken(accessToken);
        tokenStorage.setRefreshToken(newRefreshToken);

        // Update request header with new token
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;

        // Retry all queued requests
        processQueue(null);

        // Retry original request
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed → Logout user
        processQueue(refreshError); // Reject queued requests
        tokenStorage.clearAll();
        window.location.href = "/login";
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);
```

**Token Refresh Queue**:

```typescript
const processQueue = (error) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error); // Refresh failed
    } else {
      promise.resolve(); // Refresh succeeded, retry request
    }
  });
  failedQueue = [];
};
```

**Why Queue Concurrent Requests?**

Without queue:

- User makes 5 API calls simultaneously
- All 5 get 401 (token expired)
- All 5 call `/auth/refresh` → 5 duplicate refresh calls!

With queue:

- First 401 triggers refresh, sets `isRefreshing = true`
- Next 4 requests get queued (added to `failedQueue`)
- After refresh succeeds, all 5 requests retry with new token

### Key Technical Points

**Token Rotation**:

- Refresh endpoint returns NEW access token + NEW refresh token
- Old refresh token is revoked in database (prevents reuse)
- If someone steals refresh token and uses it, real user's next refresh fails → Both logout

**Security Benefits**:

- Short-lived access tokens (15 min) limit damage if stolen
- Refresh tokens stored in database, can be revoked remotely
- Token rotation prevents replay attacks

---

## Q3: "Explain your form validation strategy. Why Zod + React Hook Form?"

### Answer

**Technology Stack**:

- **Zod**: TypeScript-first schema validation library
- **React Hook Form**: Performant form library with minimal re-renders
- **@hookform/resolvers**: Connects Zod schemas to React Hook Form

**Why This Combination?**

**Zod Benefits**:

- Schema defines validation rules AND TypeScript types (single source of truth)
- Composable validators (`.email()`, `.min()`, `.max()`, custom refinements)
- Great error messages out-of-box
- Works client-side and server-side (shared validation logic)

**React Hook Form Benefits**:

- Minimal re-renders (uncontrolled components, ref-based)
- Built-in error handling (`formState.errors`)
- Great TypeScript support
- Small bundle size (~9KB)

**Example** (LoginForm):

```typescript
// 1. Define Zod schema
const loginSchema = z.object({
  email: z.string().email("Invalid email address"),
  password: z.string().min(1, "Password is required"),
});

// 2. Infer TypeScript type from schema
type LoginFormData = z.infer<typeof loginSchema>;

// 3. Connect to React Hook Form
const { register, handleSubmit, formState: { errors } } = useForm<LoginFormData>({
  resolver: zodResolver(loginSchema),
});

// 4. Use in JSX
<input {...register("email")} type="email" />
{errors.email && <p className="text-red-600">{errors.email.message}</p>}
```

**Complex Example** (RegisterTenantForm with 6 fields):

```typescript
const registerTenantSchema = z.object({
  tenantName: z.string().min(2, "Tenant name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  plan: z.enum(["Free", "Pro", "Enterprise"], {
    message: "Please select a valid plan",
  }),
});

type RegisterTenantFormData = z.infer<typeof registerTenantSchema>;
// TypeScript type is automatically:
// {
//   tenantName: string;
//   email: string;
//   password: string;
//   firstName: string;
//   lastName: string;
//   plan: "Free" | "Pro" | "Enterprise";
// }
```

### Key Technical Points

**Validation Timing**:

- Client-side validation (instant feedback, better UX)
- Server-side validation (security, handle edge cases)
- Both use same validation rules (shared DTO types)

**Performance**:

- React Hook Form only re-renders when errors change
- Form inputs are uncontrolled (no React state updates on every keystroke)
- Validation runs on blur or submit (configurable)

**Alternatives (Not Used)**:

- **Formik**: Slower, more re-renders, larger bundle
- **Manual validation**: Error-prone, hard to maintain
- **Yup**: Good but less TypeScript-native than Zod

---

## Q4: "How do you handle multi-tenant context in the frontend? Where does X-Tenant-Id come from?"

### Answer

**Multi-tenant Flow**:

1. User logs in → Backend returns `user` + `tenant` + `accessToken`
2. Frontend stores `tenant` in Redux state + localStorage
3. Axios request interceptor automatically adds `X-Tenant-Id` header to all requests
4. Backend uses this header to filter database queries

**Implementation** (`apiClient.ts`):

```typescript
// Request interceptor - runs before EVERY request
apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken();
  const tenantId = tokenStorage.getTenantId();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (tenantId) {
    config.headers["X-Tenant-Id"] = tenantId; // Required by backend
  }

  return config;
});
```

**Token Storage** (`tokenStorage.ts`):

```typescript
export const tokenStorage = {
  getTenantId(): string | null {
    return localStorage.getItem("tenantId");
  },

  setTenantId(tenantId: string): void {
    localStorage.setItem("tenantId", tenantId);
  },

  getTenant(): Tenant | null {
    const tenant = localStorage.getItem("tenant");
    return tenant ? JSON.parse(tenant) : null;
  },

  setTenant(tenant: Tenant): void {
    localStorage.setItem("tenant", JSON.stringify(tenant));
    // Also store tenantId separately for easy access
    localStorage.setItem("tenantId", tenant.id);
  },
};
```

**Why This Approach?**

- **Automatic**: Developers don't manually add header to every request
- **Consistent**: All API calls guaranteed to have tenant context
- **Secure**: Header validated server-side (can't fake access to other tenant)
- **Centralized**: Single place to modify tenant logic

### Key Technical Points

**Backend Validation** (middleware):

```csharp
// Backend checks header is valid GUID
if (!Guid.TryParse(tenantIdHeader, out var tenantId))
{
    return Results.BadRequest("Invalid X-Tenant-Id header format");
}

// All queries auto-filtered by tenantId
SELECT * FROM resources WHERE tenant_id = @TenantId;
```

**Frontend Cannot Bypass**:

- Even if user modifies localStorage, backend validates tenant access
- JWT token contains user's tenant_id claim
- Backend rejects requests where header doesn't match token claim

---

## Q5: "Explain your Protected Route pattern. How does it prevent unauthorized access?"

### Answer

**Implementation** (`ProtectedRoute.tsx`):

```typescript
interface ProtectedRouteProps {
  children: ReactNode;
}

export const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { isAuthenticated, isLoading } = useAppSelector((state) => state.auth);

  // Still checking auth status (initial page load)
  if (isLoading) {
    return <div className="flex items-center justify-center min-h-screen">
      <div className="text-lg">Loading...</div>
    </div>;
  }

  // Not authenticated → Redirect to login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Authenticated → Render protected content
  return <>{children}</>;
};
```

**Usage** (App.tsx):

```typescript
<Routes>
  {/* Public routes */}
  <Route path="/login" element={<LoginPage />} />
  <Route path="/register-tenant" element={<RegisterTenantPage />} />

  {/* Protected routes */}
  <Route
    path="/dashboard"
    element={
      <ProtectedRoute>
        <Dashboard />
      </ProtectedRoute>
    }
  />

  <Route
    path="/resources"
    element={
      <ProtectedRoute>
        <ResourcesPage />
      </ProtectedRoute>
    }
  />
</Routes>
```

**How It Works**:

1. User tries to access `/dashboard` without login
2. `ProtectedRoute` checks Redux state: `isAuthenticated = false`
3. Component renders `<Navigate to="/login" replace />`
4. React Router redirects to `/login` page
5. `replace` prop prevents adding `/dashboard` to browser history (can't go back)

**Initial Page Load**:

```typescript
// On app startup, check localStorage for existing session
useEffect(() => {
  const authData = tokenStorage.getAllAuthData();
  if (authData.accessToken && authData.user && authData.tenant) {
    dispatch(restoreAuth(authData)); // Restore Redux state from localStorage
  }
}, [dispatch]);
```

**Logout Flow**:

```typescript
// In authSlice
logout(state) {
  state.user = null;
  state.tenant = null;
  state.accessToken = null;
  state.refreshToken = null;
  state.isAuthenticated = false;
  state.error = null;

  tokenStorage.clearAll(); // Clear localStorage
}

// In Navigation component
const handleLogout = () => {
  dispatch(logout()); // Clear state
  navigate('/login'); // Redirect
};
```

### Key Technical Points

**Why `replace` Prop?**

Without `replace`:

- User visits `/dashboard` → Redirects to `/login` → History: ["/dashboard", "/login"]
- User logs in → Redirects to `/dashboard` → History: ["/dashboard", "/login", "/dashboard"]
- User clicks back button → Goes to `/login` (confusing!)

With `replace`:

- User visits `/dashboard` → Redirects to `/login` → History: ["/login"] (replaces)
- User logs in → Redirects to `/dashboard` → History: ["/login", "/dashboard"]
- User clicks back button → Goes to previous page before login (expected)

**Security Note**:

- This is client-side protection only (UX, prevents route rendering)
- **Real security is server-side**: Backend validates JWT on every request
- Even if user bypasses frontend routing, API returns 401 Unauthorized

---

## Q6: "How does your logout functionality work? What gets cleared and why?"

### Answer

**Logout Implementation** (Navigation.tsx):

```typescript
const handleLogout = () => {
  dispatch(logout()); // Clear Redux state
  navigate("/login"); // Redirect to login page
};
```

**Redux Logout Action** (authSlice.ts):

```typescript
logout(state) {
  // Clear all Redux state
  state.user = null;
  state.tenant = null;
  state.accessToken = null;
  state.refreshToken = null;
  state.isAuthenticated = false;
  state.isLoading = false;
  state.error = null;

  // Clear all localStorage
  tokenStorage.clearAll();
}
```

**localStorage Cleanup** (tokenStorage.ts):

```typescript
clearAll(): void {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('tenantId');
  localStorage.removeItem('user');
  localStorage.removeItem('tenant');
}
```

**What Happens After Logout**:

1. Redux state cleared → `isAuthenticated = false`
2. `ProtectedRoute` sees `isAuthenticated = false` → Redirects to `/login`
3. All API calls fail (no token in headers)
4. User must login again to access protected routes

**Security Considerations**:

**What We Clear**:

- ✅ Access token (JWT)
- ✅ Refresh token
- ✅ User data
- ✅ Tenant data
- ✅ Redux state

**Backend Actions** (optional enhancement):

- Could call `/api/v1/auth/revoke` endpoint
- Backend marks refresh token as revoked in database
- Prevents token reuse if stolen

**Why localStorage for Tokens?**

**Pros**:

- Persists across page reloads
- Simple API
- Survives browser restart

**Cons**:

- Vulnerable to XSS attacks (malicious scripts can read tokens)

**Alternatives**:

- **HttpOnly cookies**: More secure (JavaScript can't access), but harder with CORS
- **SessionStorage**: Cleared when tab closes (less convenient)
- **Memory only**: Lost on page reload (bad UX)

**Mitigation**:

- Short-lived access tokens (15 min)
- Token rotation (refresh returns new token)
- Content Security Policy (CSP) headers
- Sanitize user input (prevent XSS)

### Key Technical Points

**Navigation Component** (where logout button lives):

```typescript
export const Navigation = () => {
  const { user, tenant, isAuthenticated } = useAppSelector((state) => state.auth);
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  // Only show navigation when authenticated
  if (!isAuthenticated) {
    return null;
  }

  return (
    <nav className="bg-white shadow-sm">
      {/* Logo, Tenant name */}
      {/* User dropdown menu */}
      <button onClick={handleLogout}>Sign out</button>
    </nav>
  );
};
```

---

## Q7: "Why did you create separate Page components (LoginPage, Dashboard) instead of using forms directly in routes?"

### Answer

**Structure**:

```
features/auth/components/   # Pure forms (no routing logic)
  ├── LoginForm.tsx
  ├── RegisterTenantForm.tsx
  └── RegisterUserForm.tsx

pages/                      # Route-level components
  ├── LoginPage.tsx         # Wraps LoginForm
  ├── RegisterTenantPage.tsx
  └── Dashboard.tsx
```

**Benefits**:

1. **Separation of Concerns**:
   - Forms handle validation, submission, UI
   - Pages handle routing, layout, page-level logic

2. **Reusability**:

   ```typescript
   // Could use LoginForm in multiple places
   <LoginPage />              // Full-page login
   <Modal><LoginForm /></Modal>  // Login modal
   ```

3. **Testability**:

   ```typescript
   // Test form without routing
   render(<LoginForm />);

   // Test page with routing
   render(<LoginPage />, { wrapper: BrowserRouter });
   ```

4. **Future Flexibility**:
   ```typescript
   // Page can compose multiple components
   export const Dashboard = () => (
     <>
       <Navigation />
       <Sidebar />
       <DashboardContent />
       <Footer />
     </>
   );
   ```

**Example** (LoginPage.tsx):

```typescript
import { LoginForm } from "../features/auth/components/LoginForm";

export const LoginPage = () => {
  // Could add page-level logic here:
  // - Analytics tracking
  // - A/B testing
  // - Feature flags
  // - Layout wrappers

  return <LoginForm />;
};
```

**Alternative (Not Used)**:

```typescript
// Mixing routing and form logic (harder to maintain)
<Route path="/login" element={<LoginForm />} />
```

### Key Technical Points

**Feature-based Organization**:

- `features/auth/` contains all auth-related logic (forms, slice, types)
- `pages/` contains route-level wrappers
- `components/` contains shared UI components (Navigation, Button, etc.)

**Why This Matters for Interviews**:

- Shows understanding of component composition
- Demonstrates thinking about long-term maintainability
- Separation of concerns (routing vs. business logic)

---

## 🎯 Common Follow-up Questions

### "How would you handle role-based access control in the frontend?"

**Answer**:

Create a higher-order component or hook:

```typescript
// Hook approach
export const useRequireRole = (allowedRoles: string[]) => {
  const { user } = useAppSelector((state) => state.auth);
  const navigate = useNavigate();

  useEffect(() => {
    if (!user?.roles.some(role => allowedRoles.includes(role))) {
      navigate('/unauthorized');
    }
  }, [user, allowedRoles, navigate]);
};

// Usage
const AdminPanel = () => {
  useRequireRole(['TenantAdmin']); // Only admins can access
  return <div>Admin stuff</div>;
};

// HOC approach
export const RequireRole = ({ children, roles }: { children: ReactNode, roles: string[] }) => {
  const { user } = useAppSelector((state) => state.auth);

  if (!user?.roles.some(role => roles.includes(role))) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
};

// Usage in routes
<Route path="/admin" element={
  <ProtectedRoute>
    <RequireRole roles={['TenantAdmin']}>
      <AdminPanel />
    </RequireRole>
  </ProtectedRoute>
} />
```

**Note**: Frontend role checks are UX only. Real security is backend authorization policies.

---

### "How would you handle 'remember me' functionality?"

**Answer**:

Add checkbox to login form:

```typescript
const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
  rememberMe: z.boolean().optional(),
});

const onSubmit = async (data) => {
  const result = await dispatch(login(data));

  if (login.fulfilled.match(result)) {
    if (data.rememberMe) {
      // Keep refresh token in localStorage (survives browser restart)
      tokenStorage.setRememberMe(true);
    } else {
      // Move refresh token to sessionStorage (cleared on tab close)
      sessionStorage.setItem("refreshToken", result.payload.refreshToken);
      localStorage.removeItem("refreshToken");
    }
  }
};
```

**Backend Consideration**: Refresh token expiry should be longer if "remember me" is checked (7 days vs. 1 day).

---

### "How do you prevent security vulnerabilities in your frontend auth?"

**Answer**:

**XSS Prevention**:

- React auto-escapes all dynamic content
- Never use `dangerouslySetInnerHTML` with user input
- Content Security Policy (CSP) headers

**CSRF Prevention**:

- Using JWT in Authorization header (not cookies)
- Backend validates tenant context matches token claim

**Token Security**:

- Short-lived access tokens (15 min)
- Refresh token rotation (old token revoked)
- HTTPS only in production
- Clear tokens on logout

**Input Validation**:

- Zod validates all form inputs
- Backend validates again (never trust client)
- SQL injection prevented (Dapper parameterized queries)

**Dependency Security**:

- Regular `npm audit` checks
- Keep dependencies updated
- Review security advisories
