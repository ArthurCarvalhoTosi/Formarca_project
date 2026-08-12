const ACCESS_TOKEN_KEY = 'formarca.accessToken'
const REFRESH_TOKEN_KEY = 'formarca.refreshToken'
const ACTIVE_TENANT_KEY = 'formarca.activeTenantId'

export const tokenStorage = {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY)
  },

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },

  getActiveTenantId(): string | null {
    return localStorage.getItem(ACTIVE_TENANT_KEY)
  },

  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  },

  setActiveTenantId(tenantId: string | null): void {
    if (tenantId) {
      localStorage.setItem(ACTIVE_TENANT_KEY, tenantId)
    } else {
      localStorage.removeItem(ACTIVE_TENANT_KEY)
    }
  },

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    localStorage.removeItem(ACTIVE_TENANT_KEY)
  },
}
