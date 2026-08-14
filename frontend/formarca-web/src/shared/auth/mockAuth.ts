import { ApiError } from '@/shared/api/types'
import type { AuthUser, LoginRequest, LoginResponse, TenantSummary } from './types'

export const MOCK_ACCESS_TOKEN = 'mock-access-token'
export const MOCK_REFRESH_TOKEN = 'mock-refresh-token'

export const MOCK_CREDENTIALS = {
  email: 'admin@formarca.com',
  password: 'admin',
} as const

export const MOCK_USER: AuthUser = {
  id: 'mock-user-id',
  email: MOCK_CREDENTIALS.email,
  displayName: 'Admin Formarca',
  platformRole: 'PlatformAdmin',
  permissions: [],
}

export const MOCK_TENANTS: TenantSummary[] = [
  { id: 'mock-tenant-demo', name: 'Demo', slug: 'demo', isDefault: true },
  { id: 'mock-tenant-acme', name: 'Acme Marcas', slug: 'acme' },
]

export function isMockAccessToken(token: string | null): boolean {
  return token === MOCK_ACCESS_TOKEN
}

export function mockLogin(payload: LoginRequest): LoginResponse {
  const email = payload.email.trim().toLowerCase()
  const expected = MOCK_CREDENTIALS.email.toLowerCase()

  if (email !== expected || payload.password !== MOCK_CREDENTIALS.password) {
    throw new ApiError(401, 'Credenciais inválidas (modo mock).')
  }

  const preferred =
    payload.tenantId && MOCK_TENANTS.some((t) => t.id === payload.tenantId)
      ? payload.tenantId
      : MOCK_TENANTS[0].id

  return {
    accessToken: MOCK_ACCESS_TOKEN,
    refreshToken: MOCK_REFRESH_TOKEN,
    user: MOCK_USER,
    tenants: MOCK_TENANTS,
    activeTenantId: preferred,
  }
}
