export type PlatformRole = 'PlatformAdmin' | 'None' | string

export type TenantSummary = {
  id: string
  name: string
  slug?: string
  isDefault?: boolean
}

export type AuthUser = {
  id: string
  email: string
  displayName?: string
  platformRole?: PlatformRole | null
  permissions?: string[]
}

export type AuthTokens = {
  accessToken: string
  refreshToken: string
  expiresIn?: number
}

export type LoginRequest = {
  email: string
  password: string
  tenantId?: string
}

export type LoginResponse = AuthTokens & {
  user: AuthUser
  tenants: TenantSummary[]
  activeTenantId?: string | null
}

export type MeResponse = {
  user: AuthUser
  tenants: TenantSummary[]
  activeTenantId?: string | null
}

export type SwitchTenantRequest = {
  tenantId: string
}

export type SwitchTenantResponse = AuthTokens & {
  user: AuthUser
  tenants: TenantSummary[]
  activeTenantId: string
}

export type RefreshRequest = {
  refreshToken: string
}

export type RefreshResponse = AuthTokens
