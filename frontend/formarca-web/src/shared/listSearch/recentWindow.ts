/** Rolling window used when a list also includes active records. */
export const RECENT_ACTIVE_DAYS = 7

/** Keep the cutoff stable so list query keys do not change on every render. */
const ACTIVE_SINCE_BUCKET_MS = 60 * 60 * 1000

export function recentActiveSince(days = RECENT_ACTIVE_DAYS): string {
  const since = Date.now() - days * 24 * 60 * 60 * 1000
  const bucket = Math.floor(since / ACTIVE_SINCE_BUCKET_MS) * ACTIVE_SINCE_BUCKET_MS
  return new Date(bucket).toISOString()
}

export function activeSinceFromRecentDays(value: string | undefined): string | undefined {
  const days = Number(value)
  if (!value || !Number.isFinite(days) || days <= 0) return undefined
  return recentActiveSince(days)
}
