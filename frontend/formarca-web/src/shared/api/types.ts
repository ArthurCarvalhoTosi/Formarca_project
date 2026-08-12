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
