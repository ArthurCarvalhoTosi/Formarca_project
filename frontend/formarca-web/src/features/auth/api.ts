import { apiRequest } from '@/shared/api/client'
import type {
  AuthUser,
  LoginRequest,
  LoginResponse,
  MeResponse,
  RefreshRequest,
  RefreshResponse,
  SwitchTenantRequest,
  SwitchTenantResponse,
  TenantSummary,
} from '@/shared/auth/types'

type BackendTokens = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc?: string
  tenantId?: string | null
}

type BackendAuthResult = {
  tokens: BackendTokens
  userId: string
  email: string
  displayName?: string | null
  isPlatformAdmin: boolean
  tenants: TenantSummary[]
  permissions: string[]
}

type BackendMe = {
  userId: string
  email: string
  displayName?: string | null
  isPlatformAdmin: boolean
  activeTenantId?: string | null
  tenants: TenantSummary[]
  permissions: string[]
}

function mapUser(data: {
  userId: string
  email: string
  displayName?: string | null
  isPlatformAdmin: boolean
  permissions?: string[]
}): AuthUser {
  return {
    id: data.userId,
    email: data.email,
    displayName: data.displayName ?? undefined,
    platformRole: data.isPlatformAdmin ? 'PlatformAdmin' : 'None',
    permissions: data.permissions ?? [],
  }
}

function mapAuthResult(data: BackendAuthResult): LoginResponse {
  return {
    accessToken: data.tokens.accessToken,
    refreshToken: data.tokens.refreshToken,
    user: mapUser(data),
    tenants: data.tenants ?? [],
    activeTenantId: data.tokens.tenantId ?? null,
  }
}

export const authApi = {
  async login(payload: LoginRequest) {
    const data = await apiRequest<BackendAuthResult>('/api/auth/login', {
      method: 'POST',
      body: payload,
      skipAuth: true,
      skipRefresh: true,
    })
    return mapAuthResult(data)
  },

  async refresh(payload: RefreshRequest) {
    const data = await apiRequest<BackendAuthResult>('/api/auth/refresh', {
      method: 'POST',
      body: payload,
      skipAuth: true,
      skipRefresh: true,
    })
    const mapped = mapAuthResult(data)
    return {
      accessToken: mapped.accessToken,
      refreshToken: mapped.refreshToken,
    } satisfies RefreshResponse
  },

  logout(refreshToken?: string | null) {
    return apiRequest<void>('/api/auth/logout', {
      method: 'POST',
      body: refreshToken ? { refreshToken } : {},
      skipRefresh: true,
    })
  },

  async me(): Promise<MeResponse> {
    const data = await apiRequest<BackendMe>('/api/auth/me')
    return {
      user: mapUser(data),
      tenants: data.tenants ?? [],
      activeTenantId: data.activeTenantId,
    }
  },

  async switchTenant(payload: SwitchTenantRequest): Promise<SwitchTenantResponse> {
    const data = await apiRequest<BackendAuthResult>('/api/auth/switch-tenant', {
      method: 'POST',
      body: payload,
    })
    const mapped = mapAuthResult(data)
    return {
      ...mapped,
      activeTenantId: mapped.activeTenantId ?? payload.tenantId,
    }
  },
}
