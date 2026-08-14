import { adminApi } from '@/features/admin/api'
import type { Tenant } from '@/features/admin/types'
import { registrationApi } from '@/features/registration/api'
import {
  mockGetRegistrationById,
  mockListCustomerTenants,
  mockUpdateRegistrationById,
} from '@/features/registration/mockStore'
import { toRegistrationPayload } from '@/features/registration/payload'
import type {
  RegistrationDto,
  RegistrationPayload,
} from '@/features/registration/types'
import { apiRequest } from '@/shared/api/client'
import { ApiError } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import type { CustomerListItem } from './types'

function fromTenant(tenant: Tenant): CustomerListItem {
  return {
    id: tenant.id,
    name: tenant.name,
    document: tenant.document,
    email: tenant.email,
    status: tenant.status,
    slug: tenant.slug,
  }
}

function fromRegistration(dto: RegistrationDto): CustomerListItem {
  return {
    id: dto.id,
    name: dto.name,
    document: dto.document,
    email: dto.email,
    status: dto.status,
    slug: dto.slug,
  }
}

export const customersApi = {
  async list(isPlatformAdmin: boolean): Promise<CustomerListItem[]> {
    if (isPlatformAdmin) {
      if (env.mockAuth) {
        return mockListCustomerTenants()
      }
      const tenants = await adminApi.listTenants()
      return tenants.map(fromTenant)
    }

    const current = await registrationApi.getCurrent()
    return current ? [fromRegistration(current)] : []
  },

  async get(
    id: string,
    activeTenantId: string | null,
  ): Promise<RegistrationDto | null> {
    if (id === activeTenantId) {
      return registrationApi.getCurrent()
    }
    if (env.mockAuth) {
      return mockGetRegistrationById(id)
    }
    try {
      return await apiRequest<RegistrationDto>(`/api/registration/${id}`)
    } catch (err) {
      if (err instanceof ApiError && (err.status === 404 || err.status === 400)) {
        return null
      }
      throw err
    }
  },

  async create(payload: RegistrationPayload): Promise<void> {
    await registrationApi.submit(payload)
  },

  async update(
    id: string,
    payload: RegistrationPayload,
    activeTenantId: string | null,
  ): Promise<RegistrationDto> {
    if (id === activeTenantId) {
      return registrationApi.update(payload)
    }
    const body = toRegistrationPayload(payload)
    if (env.mockAuth) {
      return mockUpdateRegistrationById(id, body)
    }
    return apiRequest<RegistrationDto>(`/api/registration/${id}`, {
      method: 'PUT',
      body,
    })
  },
}
