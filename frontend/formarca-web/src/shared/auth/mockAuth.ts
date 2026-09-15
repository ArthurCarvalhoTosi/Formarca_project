import { ApiError } from '@/shared/api/types'
import type { AuthUser, LoginRequest, LoginResponse, TenantSummary } from './types'

export const MOCK_ACCESS_TOKEN = 'mock-access-token'
export const MOCK_REFRESH_TOKEN = 'mock-refresh-token'
export const MOCK_CLIENT_ACCESS_TOKEN = 'mock-client-access-token'
export const MOCK_CLIENT_REFRESH_TOKEN = 'mock-client-refresh-token'

export const MOCK_CREDENTIALS = {
  email: 'admin@formarca.com',
  password: 'admin',
} as const

export const MOCK_CLIENT_CREDENTIALS = {
  email: 'cliente@acmemarcas.com',
  password: 'cliente123',
} as const

export const MOCK_USER: AuthUser = {
  id: 'mock-user-id',
  email: MOCK_CREDENTIALS.email,
  displayName: 'Admin Formarca',
  platformRole: 'PlatformAdmin',
  permissions: [],
}

export const MOCK_CLIENT_USER: AuthUser = {
  id: 'mock-client-user-id',
  email: MOCK_CLIENT_CREDENTIALS.email,
  displayName: 'Cliente Acme',
  platformRole: 'None',
  permissions: [
    'registrations.read',
    'registrations.manage',
    'contracts.read',
    'contracts.sign',
    'invoices.read',
    'invoices.pay',
    'brands.read',
    'processes.read',
  ],
}

export const MOCK_TENANTS: TenantSummary[] = [
  {
    id: 'mock-tenant-demo',
    name: 'Demo',
    slug: 'demo',
    isDefault: true,
    status: 'Active',
  },
  {
    id: 'mock-tenant-acme',
    name: 'Acme Marcas',
    slug: 'acme',
    status: 'Onboarding',
  },
]

export function isMockAccessToken(token: string | null): boolean {
  return token === MOCK_ACCESS_TOKEN || token === MOCK_CLIENT_ACCESS_TOKEN
}

export function snapshotMockTenants(ids?: string[]): TenantSummary[] {
  const source = ids
    ? MOCK_TENANTS.filter((tenant) => ids.includes(tenant.id))
    : MOCK_TENANTS
  return source.map((tenant) => ({ ...tenant }))
}

export function syncMockTenantStatus(id: string, status: string): void {
  const tenant = MOCK_TENANTS.find((item) => item.id === id)
  if (tenant) tenant.status = status
}

export function getMockSessionByToken(token: string | null): {
  user: AuthUser
  tenants: TenantSummary[]
} | null {
  if (token === MOCK_ACCESS_TOKEN) {
    return { user: MOCK_USER, tenants: snapshotMockTenants() }
  }
  if (token === MOCK_CLIENT_ACCESS_TOKEN) {
    return {
      user: MOCK_CLIENT_USER,
      tenants: snapshotMockTenants([MOCK_TENANTS[1].id]),
    }
  }
  return null
}

export function mockLogin(payload: LoginRequest): LoginResponse {
  const email = payload.email.trim().toLowerCase()
  const adminEmail = MOCK_CREDENTIALS.email.toLowerCase()
  const clientEmail = MOCK_CLIENT_CREDENTIALS.email.toLowerCase()

  if (email === adminEmail && payload.password === MOCK_CREDENTIALS.password) {
    const tenants = snapshotMockTenants()
    const preferred =
      payload.tenantId && tenants.some((t) => t.id === payload.tenantId)
        ? payload.tenantId
        : tenants[0].id
    return {
      accessToken: MOCK_ACCESS_TOKEN,
      refreshToken: MOCK_REFRESH_TOKEN,
      user: MOCK_USER,
      tenants,
      activeTenantId: preferred,
    }
  }

  if (email === clientEmail && payload.password === MOCK_CLIENT_CREDENTIALS.password) {
    const tenants = snapshotMockTenants([MOCK_TENANTS[1].id])
    return {
      accessToken: MOCK_CLIENT_ACCESS_TOKEN,
      refreshToken: MOCK_CLIENT_REFRESH_TOKEN,
      user: MOCK_CLIENT_USER,
      tenants,
      activeTenantId: tenants[0]?.id ?? null,
    }
  }

  throw new ApiError(401, 'Invalid credentials')
}
