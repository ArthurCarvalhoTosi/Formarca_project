import { apiRequest } from '@/shared/api/client'
import { ApiError } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import {
  mockGetCurrentRegistration,
  mockSubmitRegistration,
  mockUpdateRegistration,
} from './mockStore'
import { toRegistrationPayload } from './payload'
import type { RegistrationDto, RegistrationPayload } from './types'

export function mapRegistrationError(
  err: unknown,
  fallback: string,
  taken: { email: string; document: string },
): string {
  if (err instanceof ApiError) {
    const message = err.message.toLowerCase()
    if (message.includes('email')) return taken.email
    if (
      message.includes('document') ||
      message.includes('cpf') ||
      message.includes('cnpj') ||
      message.includes('tax')
    ) {
      return taken.document
    }
    return err.message || fallback
  }
  return fallback
}

export const registrationApi = {
  async submit(payload: RegistrationPayload): Promise<void> {
    const body = toRegistrationPayload(payload)
    if (env.mockAuth) {
      mockSubmitRegistration(body)
      return
    }
    await apiRequest<void>('/api/registrations', {
      method: 'POST',
      body,
      skipAuth: true,
    })
  },

  async getCurrent(): Promise<RegistrationDto | null> {
    if (env.mockAuth) {
      return mockGetCurrentRegistration()
    }
    try {
      return await apiRequest<RegistrationDto>('/api/registration')
    } catch (err) {
      if (err instanceof ApiError && (err.status === 404 || err.status === 400)) {
        return null
      }
      throw err
    }
  },

  async update(payload: RegistrationPayload): Promise<RegistrationDto> {
    const body = toRegistrationPayload(payload)
    if (env.mockAuth) {
      return mockUpdateRegistration(body)
    }
    return apiRequest<RegistrationDto>('/api/registration', {
      method: 'PUT',
      body,
    })
  },
}
