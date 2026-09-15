import type { AuthUser } from './types'

export function hasPermission(
  user: AuthUser | null | undefined,
  code: string,
  options: { isPlatformAdmin?: boolean; bypassAdmin?: boolean } = {},
): boolean {
  if (!code) return false
  const isPlatformAdmin =
    options.isPlatformAdmin ?? user?.platformRole === 'PlatformAdmin'
  if (isPlatformAdmin && options.bypassAdmin !== false) return true
  return (user?.permissions ?? []).includes(code)
}
