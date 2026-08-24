const GENERIC_API_TITLES = new Set([
  'conflict',
  'not found',
  'forbidden',
  'unauthorized',
  'validation failed',
  'server error',
])

export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(status: number, message: string, body?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' ? (value as Record<string, unknown>) : null
}

function pickString(record: Record<string, unknown>, keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key]
    if (typeof value === 'string' && value.trim()) return value.trim()
  }
  return undefined
}

function flattenValidationErrors(errors: unknown): string | undefined {
  if (!errors || typeof errors !== 'object') return undefined
  const messages: string[] = []
  for (const value of Object.values(errors as Record<string, unknown>)) {
    if (typeof value === 'string' && value.trim()) {
      messages.push(value.trim())
    } else if (Array.isArray(value)) {
      for (const item of value) {
        if (typeof item === 'string' && item.trim()) messages.push(item.trim())
      }
    }
  }
  return messages.length > 0 ? messages.join(' ') : undefined
}

export function parseApiErrorBody(body: unknown, fallback: string): string {
  if (typeof body === 'string' && body.trim()) return body.trim()
  const record = asRecord(body)
  if (!record) return fallback

  const validation = flattenValidationErrors(record.errors ?? record.Errors)
  if (validation) return validation

  const detail = pickString(record, ['detail', 'Detail', 'message', 'Message', 'error', 'Error'])
  if (detail) return detail

  const title = pickString(record, ['title', 'Title'])
  if (title && !GENERIC_API_TITLES.has(title.toLowerCase())) return title

  return fallback
}

export function getApiErrorMessage(
  error: unknown,
  fallback: string,
  aliases?: Record<string, string>,
): string {
  const message =
    error instanceof ApiError && error.message.trim()
      ? error.message
      : error instanceof Error && error.message.trim()
        ? error.message
        : fallback
  return aliases?.[message] ?? message
}

export type ApiRequestOptions = {
  method?: string
  body?: unknown
  headers?: Record<string, string>
  /** Skip Authorization header (login/refresh). */
  skipAuth?: boolean
  /** Do not attempt token refresh on 401. */
  skipRefresh?: boolean
  signal?: AbortSignal
}

export type PagedResult<T> = {
  items: T[]
  totalCount?: number
  page?: number
  pageSize?: number
}

export function asList<T>(data: T[] | PagedResult<T> | null | undefined): T[] {
  if (!data) return []
  if (Array.isArray(data)) return data
  return data.items ?? []
}

export function toQuery(
  params: Record<string, string | number | undefined | null>,
): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }
  const qs = search.toString()
  return qs ? `?${qs}` : ''
}
