import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '@/features/auth/api'
import { setUnauthorizedHandler } from '@/shared/api/client'
import { env } from '@/shared/config/env'
import { getMockSessionByToken, mockLogin } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { hasPermission as userHasPermission } from '@/shared/auth/permissions'
import type {
  AuthUser,
  LoginRequest,
  TenantSummary,
} from '@/shared/auth/types'

type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

type AuthContextValue = {
  status: AuthStatus
  user: AuthUser | null
  tenants: TenantSummary[]
  activeTenantId: string | null
  activeTenant: TenantSummary | null
  isPlatformAdmin: boolean
  isOnboardingLimited: boolean
  hasPermission: (code: string, options?: { bypassAdmin?: boolean }) => boolean
  login: (payload: LoginRequest) => Promise<void>
  logout: () => Promise<void>
  switchTenant: (tenantId: string) => Promise<void>
  refreshSession: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function pickDefaultTenantId(
  tenants: TenantSummary[],
  preferred?: string | null,
): string | null {
  if (preferred && tenants.some((t) => t.id === preferred)) return preferred
  const stored = tokenStorage.getActiveTenantId()
  if (stored && tenants.some((t) => t.id === stored)) return stored
  const defaultTenant = tenants.find((t) => t.isDefault)
  if (defaultTenant) return defaultTenant.id
  return tenants[0]?.id ?? null
}

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<AuthUser | null>(null)
  const [tenants, setTenants] = useState<TenantSummary[]>([])
  const [activeTenantId, setActiveTenantId] = useState<string | null>(null)

  const clearSession = useCallback(() => {
    tokenStorage.clear()
    setUser(null)
    setTenants([])
    setActiveTenantId(null)
    setStatus('anonymous')
  }, [])

  const applySession = useCallback(
    (next: {
      user: AuthUser
      tenants: TenantSummary[]
      activeTenantId?: string | null
      accessToken?: string
      refreshToken?: string
    }) => {
      if (next.accessToken && next.refreshToken) {
        tokenStorage.setTokens(next.accessToken, next.refreshToken)
      }

      const tenantId = pickDefaultTenantId(next.tenants, next.activeTenantId)
      tokenStorage.setActiveTenantId(tenantId)
      setUser(next.user)
      setTenants(next.tenants)
      setActiveTenantId(tenantId)
      setStatus('authenticated')
    },
    [],
  )

  const refreshSession = useCallback(async () => {
    const accessToken = tokenStorage.getAccessToken()
    if (!accessToken) {
      clearSession()
      return
    }

    if (env.mockAuth) {
      const session = getMockSessionByToken(accessToken)
      if (session) {
        applySession({
          user: session.user,
          tenants: session.tenants,
          activeTenantId: tokenStorage.getActiveTenantId(),
        })
        return
      }
      clearSession()
      return
    }

    try {
      const me = await authApi.me()
      applySession({
        user: me.user,
        tenants: me.tenants ?? [],
        activeTenantId: me.activeTenantId,
      })
    } catch {
      clearSession()
    }
  }, [applySession, clearSession])

  useEffect(() => {
    void refreshSession()
  }, [refreshSession])

  useEffect(() => {
    setUnauthorizedHandler(() => {
      clearSession()
    })
    return () => setUnauthorizedHandler(null)
  }, [clearSession])

  const login = useCallback(
    async (payload: LoginRequest) => {
      const result = env.mockAuth
        ? mockLogin(payload)
        : await authApi.login(payload)
      applySession({
        user: result.user,
        tenants: result.tenants ?? [],
        activeTenantId: result.activeTenantId ?? payload.tenantId,
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
      })
    },
    [applySession],
  )

  const logout = useCallback(async () => {
    if (!env.mockAuth) {
      const refreshToken = tokenStorage.getRefreshToken()
      try {
        await authApi.logout(refreshToken)
      } catch {
        // Local logout still proceeds if API is unreachable.
      }
    }
    clearSession()
  }, [clearSession])

  const switchTenant = useCallback(
    async (tenantId: string) => {
      if (env.mockAuth) {
        const session = getMockSessionByToken(tokenStorage.getAccessToken())
        const nextUser = session?.user ?? user
        if (!nextUser) return
        applySession({
          user: nextUser,
          tenants: session?.tenants ?? tenants,
          activeTenantId: tenantId,
        })
        return
      }

      const result = await authApi.switchTenant({ tenantId })
      applySession({
        user: result.user,
        tenants: result.tenants ?? tenants,
        activeTenantId: result.activeTenantId ?? tenantId,
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
      })
    },
    [applySession, tenants, user],
  )

  const isPlatformAdmin =
    user?.platformRole === 'PlatformAdmin' ||
    user?.email?.trim().toLowerCase() === 'admin@formarca.com'
  const activeTenant = tenants.find((tenant) => tenant.id === activeTenantId) ?? null
  const isOnboardingLimited =
    activeTenant?.status === 'Onboarding' && !isPlatformAdmin
  const hasPermission = useCallback(
    (code: string, options?: { bypassAdmin?: boolean }) =>
      userHasPermission(user, code, { isPlatformAdmin, bypassAdmin: options?.bypassAdmin }),
    [isPlatformAdmin, user],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      tenants,
      activeTenantId,
      activeTenant,
      isPlatformAdmin,
      isOnboardingLimited,
      hasPermission,
      login,
      logout,
      switchTenant,
      refreshSession,
    }),
    [
      status,
      user,
      tenants,
      activeTenantId,
      activeTenant,
      isPlatformAdmin,
      isOnboardingLimited,
      hasPermission,
      login,
      logout,
      switchTenant,
      refreshSession,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}
