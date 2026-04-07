import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import type {
  AuthState,
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
  RegisterTenantRequest,
  RegisterTenantResponse,
  RegisterUserRequest,
  RegisterUserResponse,
} from "../../types/auth.types";

// Initial state
const initialState: AuthState = {
  user: null,
  tenant: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

// Async thunk for login
export const login = createAsyncThunk<
  LoginResponse,
  LoginRequest,
  { rejectValue: string }
>("auth/login", async (credentials, { rejectWithValue }) => {
  try {
    const response = await fetch("/api/v1/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      const error = await response.json();
      return rejectWithValue(error.message || "Login failed");
    }

    return await response.json();
  } catch (error) {
    return rejectWithValue("Network error. Please try again.");
  }
});

// Async thunk for tenant registration
export const registerTenant = createAsyncThunk<
  RegisterTenantResponse,
  RegisterTenantRequest,
  { rejectValue: string }
>("auth/registerTenant", async (data, { rejectWithValue }) => {
  try {
    const response = await fetch("/api/v1/auth/register-tenant", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      const error = await response.json();
      return rejectWithValue(error.message || "Registration failed");
    }

    return await response.json();
  } catch (error) {
    return rejectWithValue("Network error. Please try again.");
  }
});

// Async thunk for user registration (within tenant)
export const registerUser = createAsyncThunk<
  RegisterUserResponse,
  RegisterUserRequest,
  { rejectValue: string }
>("auth/registerUser", async (data, { rejectWithValue }) => {
  try {
    const response = await fetch("/api/v1/auth/register-user", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        // Note: This requires authentication (existing user registering new user)
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      const error = await response.json();
      return rejectWithValue(error.message || "User registration failed");
    }

    return await response.json();
  } catch (error) {
    return rejectWithValue("Network error. Please try again.");
  }
});

// Async thunk for refreshing access token
export const refreshAccessToken = createAsyncThunk<
  RefreshTokenResponse,
  string,
  { rejectValue: string }
>("auth/refreshToken", async (refreshToken, { rejectWithValue }) => {
  try {
    const response = await fetch("/api/v1/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      return rejectWithValue("Token refresh failed");
    }

    return await response.json();
  } catch (error) {
    return rejectWithValue("Network error. Please try again.");
  }
});

// Auth slice
const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    // Logout action
    logout(state) {
      state.user = null;
      state.tenant = null;
      state.accessToken = null;
      state.refreshToken = null;
      state.isAuthenticated = false;
      state.error = null;

      // Clear tokens from localStorage
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("tenantId");
    },

    // Clear error action
    clearError(state) {
      state.error = null;
    },

    // Restore auth from localStorage (on app init)
    restoreAuth(
      state,
      action: PayloadAction<{
        user: any;
        tenant: any;
        accessToken: string;
        refreshToken: string;
      }>,
    ) {
      state.user = action.payload.user;
      state.tenant = action.payload.tenant;
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      state.isAuthenticated = true;
    },
  },
  extraReducers: (builder) => {
    // Login
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        state.tenant = action.payload.tenant;
        state.accessToken = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;
        state.isAuthenticated = true;
        state.error = null;

        // Store tokens in localStorage
        localStorage.setItem("accessToken", action.payload.accessToken);
        localStorage.setItem("refreshToken", action.payload.refreshToken);
        localStorage.setItem("tenantId", action.payload.tenant.id);
        localStorage.setItem("user", JSON.stringify(action.payload.user));
        localStorage.setItem("tenant", JSON.stringify(action.payload.tenant));
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload || "Login failed";
      });

    // Register Tenant
    builder
      .addCase(registerTenant.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerTenant.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        state.tenant = action.payload.tenant;
        state.accessToken = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;
        state.isAuthenticated = true;
        state.error = null;

        // Store tokens in localStorage
        localStorage.setItem("accessToken", action.payload.accessToken);
        localStorage.setItem("refreshToken", action.payload.refreshToken);
        localStorage.setItem("tenantId", action.payload.tenant.id);
        localStorage.setItem("user", JSON.stringify(action.payload.user));
        localStorage.setItem("tenant", JSON.stringify(action.payload.tenant));
      })
      .addCase(registerTenant.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload || "Registration failed";
      });

    // Register User (within tenant)
    builder
      .addCase(registerUser.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerUser.fulfilled, (state, action) => {
        state.isLoading = false;
        // Update current user with newly registered user's info
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;
        state.error = null;

        // Update tokens in localStorage
        localStorage.setItem("accessToken", action.payload.accessToken);
        localStorage.setItem("refreshToken", action.payload.refreshToken);
        localStorage.setItem("user", JSON.stringify(action.payload.user));
      })
      .addCase(registerUser.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload || "User registration failed";
      });

    // Refresh Token
    builder
      .addCase(refreshAccessToken.pending, (state) => {
        state.error = null;
      })
      .addCase(refreshAccessToken.fulfilled, (state, action) => {
        state.accessToken = action.payload.accessToken;
        state.refreshToken = action.payload.refreshToken;

        // Update tokens in localStorage
        localStorage.setItem("accessToken", action.payload.accessToken);
        localStorage.setItem("refreshToken", action.payload.refreshToken);
      })
      .addCase(refreshAccessToken.rejected, (state) => {
        // Token refresh failed, log out user
        state.user = null;
        state.tenant = null;
        state.accessToken = null;
        state.refreshToken = null;
        state.isAuthenticated = false;

        // Clear localStorage
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("tenantId");
        localStorage.removeItem("user");
        localStorage.removeItem("tenant");
      });
  },
});

export const { logout, clearError, restoreAuth } = authSlice.actions;
export default authSlice.reducer;
