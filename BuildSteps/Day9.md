# Day 9: Authentication Flow & Tenant Setup

---

## Auth Redux Slice - Step 1

1. Created TypeScript auth types in `src/types/auth.types.ts`
2. Created Redux store configuration in `src/store/store.ts`:
   - Configured `configureStore` with auth reducer
   - Added middleware for serializable check (ignored persist actions)
   - Exported `RootState` type: `ReturnType<typeof store.getState>`
   - Exported `AppDispatch` type: `typeof store.dispatch`

3. Created typed Redux hooks in `src/store/hooks.ts`
4. Created auth Redux slice in `src/features/auth/authSlice.ts`

## Token Storage Utilities - Step 2

5. Created token storage utilities in `src/lib/tokenStorage.ts`

## Axios Configuration with Interceptors - Step 3

6. Created axios instance with interceptors in `src/lib/apiClient.ts`:
   - **Base configuration**: baseURL: '/api/v1' (uses Vite proxy to backend)
   - **Request interceptor**: Automatically adds headers to every request:
     - `Authorization: Bearer <accessToken>` (from localStorage)
     - `X-Tenant-Id: <tenantId>` (required by backend TenantResolutionMiddleware)
   - **Response interceptor**: Handles 401 Unauthorized errors:
     - Detects expired access token
     - Calls `/api/v1/auth/refresh` with refresh token
     - Updates tokens in localStorage
     - Retries original request with new access token
     - On refresh failure: clears auth data, redirects to `/login`
   - **Token refresh queue**: Prevents duplicate refresh requests
     - `isRefreshing` flag ensures only one refresh call at a time
     - `failedQueue` holds concurrent 401 requests
     - After refresh success, all queued requests retry automatically
   - Purpose: Seamless token management, no manual header injection needed

## Protected Route Component - Step 4

7. Created ProtectedRoute component in `src/components/ProtectedRoute.tsx`:
   - Wraps protected pages/routes that require authentication
   - Uses `useAppSelector` to check Redux auth state (`isAuthenticated`, `isLoading`)
   - **Three states**:
     - Loading: Shows "Loading..." message while checking auth
     - Not authenticated: Redirects to `/login` with `<Navigate replace />`
     - Authenticated: Renders `{children}` (protected content)
   - `replace` prop: Prevents adding redirect to browser history
   - Purpose: Centralized route protection, prevents unauthorized access
   - Usage: `<ProtectedRoute><DashboardPage /></ProtectedRoute>`

## Form Handling and Validation Libraries - Step 5

8. Installed React Hook Form and Zod validation:
   - Command: `npm install react-hook-form zod @hookform/resolvers`
   - **react-hook-form** (v7.x): Performant form library with minimal re-renders
   - **zod** (v3.x): TypeScript-first schema validation library
   - **@hookform/resolvers**: Connects Zod schemas to React Hook Form

## Tenant Registration Form - Step 6

9. Created RegisterTenantForm component in `src/features/auth/components`

## Login Form - Step 7

10. Created LoginForm component in `src/features/auth/components`

## User Registration Form - Step 8

11. Created RegisterUserForm component in `src/features/auth/components`

## Navigation with User Menu - Step 9

12. Created Navigation component in `src/components`

## Test Complete Auth Flow - Step 10

13. Created page components in `src/pages/`:
    - **Dashboard.tsx**: Protected dashboard page showing user and tenant info
    - **LoginPage.tsx**: Wrapper for LoginForm component
    - **RegisterTenantPage.tsx**: Wrapper for RegisterTenantForm component
    - **RegisterUserPage.tsx**: Wrapper for RegisterUserForm component

14. Updated `src/App.tsx` with React Router setup:
    - Added `BrowserRouter` wrapper
    - Configured routes
    - Protected routes use `<ProtectedRoute>` wrapper component

15. Updated `src/main.tsx` with Redux Provider:
    - Wrapped `<App />` with Redux `<Provider store={store}>`
    - Makes Redux state available throughout the application

16. Auth flow is now ready to test:
    - **Register Tenant**: Create new tenant + admin user → Auto-login → Redirect to dashboard
    - **Login**: Existing user login → Redirect to dashboard
    - **Register User**: Add user to existing tenant (requires login) → Redirect to dashboard
    - **Logout**: Click sign out in navigation → Clear state → Redirect to login
    - **Protected Routes**: Unauthenticated access to `/dashboard` → Redirect to `/login`
    - **Token Refresh**: Axios interceptor automatically refreshes expired tokens
