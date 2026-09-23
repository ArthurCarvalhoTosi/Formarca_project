/** Rolling window used when a list also includes active records. */
export const RECENT_ACTIVE_DAYS = 7

export function recentActiveSince(days = RECENT_ACTIVE_DAYS): string {
  return new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString()
}

export function activeSinceFromRecentDays(value: string | undefined): string | undefined {
  const days = Number(value)
  if (!value || !Number.isFinite(days) || days <= 0) return undefined
  return recentActiveSince(days)
}
