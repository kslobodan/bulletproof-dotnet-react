// Token Storage Utilities for localStorage management

const TOKEN_KEY = "accessToken";
const REFRESH_TOKEN_KEY = "refreshToken";
const TENANT_ID_KEY = "tenantId";
const USER_KEY = "user";
const TENANT_KEY = "tenant";

export const tokenStorage = {
  // Get access token
  getAccessToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  // Set access token
  setAccessToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  },

  // Get refresh token
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  // Set refresh token
  setRefreshToken(token: string): void {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  },

  // Get tenant ID
  getTenantId(): string | null {
    return localStorage.getItem(TENANT_ID_KEY);
  },

  // Set tenant ID
  setTenantId(tenantId: string): void {
    localStorage.setItem(TENANT_ID_KEY, tenantId);
  },

  // Get user from localStorage
  getUser(): any | null {
    const userStr = localStorage.getItem(USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  },

  // Set user in localStorage
  setUser(user: any): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  // Get tenant from localStorage
  getTenant(): any | null {
    const tenantStr = localStorage.getItem(TENANT_KEY);
    return tenantStr ? JSON.parse(tenantStr) : null;
  },

  // Set tenant in localStorage
  setTenant(tenant: any): void {
    localStorage.setItem(TENANT_KEY, JSON.stringify(tenant));
  },

  // Clear all auth data
  clearAll(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(TENANT_ID_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(TENANT_KEY);
  },

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  },

  // Get all auth data at once for restoring state
  getAllAuthData() {
    return {
      accessToken: this.getAccessToken(),
      refreshToken: this.getRefreshToken(),
      tenantId: this.getTenantId(),
      user: this.getUser(),
      tenant: this.getTenant(),
    };
  },
};
