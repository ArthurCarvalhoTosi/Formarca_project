import { env } from '@/shared/config/env'
import { isMockAccessToken } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { ApiError, parseApiErrorBody, type ApiRequestOptions } from './types'

type UnauthorizedHandler = () => void

let onUnauthorized: UnauthorizedHandler | null = null
let refreshPromise: Promise<string | null> | null = null

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  onUnauthorized = handler
}

async function parseBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) return null
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}

async function refreshAccessToken(): Promise<string | null> {
  if (env.mockAuth && isMockAccessToken(tokenStorage.getAccessToken())) {
    return tokenStorage.getAccessToken()
  }

  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) return null

  const response = await fetch(`${env.apiUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })

  if (!response.ok) {
    tokenStorage.clear()
    return null
  }

  const data = (await response.json()) as {
    accessToken?: string
    refreshToken?: string
    tokens?: { accessToken?: string; refreshToken?: string }
  }

  const accessToken = data.accessToken ?? data.tokens?.accessToken
  const nextRefreshToken = data.refreshToken ?? data.tokens?.refreshToken

  if (!accessToken || !nextRefreshToken) {
    tokenStorage.clear()
    return null
  }

  tokenStorage.setTokens(accessToken, nextRefreshToken)
  return accessToken
}

function getOrRefreshAccessToken(): Promise<string | null> {
  if (!refreshPromise) {
    refreshPromise = refreshAccessToken().finally(() => {
      refreshPromise = null
    })
  }
  return refreshPromise
}

function isActiveMockSession(): boolean {
  return env.mockAuth && isMockAccessToken(tokenStorage.getAccessToken())
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const {
    method = 'GET',
    body,
    headers = {},
    skipAuth = false,
    skipRefresh = false,
    signal,
  } = options

  const requestHeaders: Record<string, string> = {
    Accept: 'application/json',
    ...headers,
  }

  if (body !== undefined) {
    requestHeaders['Content-Type'] = 'application/json'
  }

  if (!skipAuth) {
    const accessToken = tokenStorage.getAccessToken()
    if (accessToken) {
      requestHeaders.Authorization = `Bearer ${accessToken}`
    }

    const tenantId = tokenStorage.getActiveTenantId()
    if (tenantId) {
      requestHeaders['X-Tenant-Id'] = tenantId
    }
  }

  const url = path.startsWith('http') ? path : `${env.apiUrl}${path}`

  let response = await fetch(url, {
    method,
    headers: requestHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  })

  if (
    response.status === 401 &&
    !skipAuth &&
    !skipRefresh &&
    !isActiveMockSession()
  ) {
    const newToken = await getOrRefreshAccessToken()
    if (newToken) {
      requestHeaders.Authorization = `Bearer ${newToken}`
      response = await fetch(url, {
        method,
        headers: requestHeaders,
        body: body === undefined ? undefined : JSON.stringify(body),
        signal,
      })
    } else {
      onUnauthorized?.()
      const errBody = await parseBody(response)
      throw new ApiError(401, parseApiErrorBody(errBody, 'Sessão expirada'), errBody)
    }
  }

  if (response.status === 204) {
    return undefined as T
  }

  const parsed = await parseBody(response)

  if (!response.ok) {
    if (response.status === 401 && !isActiveMockSession()) {
      onUnauthorized?.()
    }
    throw new ApiError(
      response.status,
      parseApiErrorBody(parsed, `Erro HTTP ${response.status}`),
      parsed,
    )
  }

  return parsed as T
}

export function triggerBlobDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export async function apiDownload(path: string, filename: string): Promise<void> {
  const requestHeaders: Record<string, string> = { Accept: '*/*' }
  const accessToken = tokenStorage.getAccessToken()
  if (accessToken) {
    requestHeaders.Authorization = `Bearer ${accessToken}`
  }
  const tenantId = tokenStorage.getActiveTenantId()
  if (tenantId) {
    requestHeaders['X-Tenant-Id'] = tenantId
  }

  const url = path.startsWith('http') ? path : `${env.apiUrl}${path}`
  const response = await fetch(url, { headers: requestHeaders })
  const parsed = await parseBody(response.clone())

  if (!response.ok) {
    throw new ApiError(
      response.status,
      parseApiErrorBody(parsed, `Erro HTTP ${response.status}`),
      parsed,
    )
  }

  const headerName = response.headers.get('Content-Disposition')
  const matched = headerName?.match(/filename\*?=(?:UTF-8''|"?)([^";]+)/i)
  const downloadName = matched ? decodeURIComponent(matched[1].replaceAll('"', '')) : filename
  triggerBlobDownload(await response.blob(), downloadName)
}
