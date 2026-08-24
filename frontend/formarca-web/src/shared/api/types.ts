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
