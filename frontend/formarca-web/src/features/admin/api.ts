import { apiRequest } from '@/shared/api/client'
import { env } from '@/shared/config/env'
import {
  mockApproveTenant,
  mockListTenants,
  mockRejectTenant,
} from '@/features/registration/mockStore'
import type { Group, PagedResult, Tenant, UserListItem } from './types'

type BackendUser = {
  id: string
  email: string
  displayName?: string | null
  isPlatformAdmin?: boolean
  isActive?: boolean
  tenants?: { id: string }[]
}

function asList<T>(data: T[] | PagedResult<T> | null | undefined): T[] {
  if (!data) return []
  if (Array.isArray(data)) return data
  return data.items ?? []
}

function mapUser(user: BackendUser): UserListItem {
  return {
    id: user.id,
    email: user.email,
    displayName: user.displayName,
    platformRole: user.isPlatformAdmin ? 'PlatformAdmin' : 'None',
    isActive: user.isActive,
    tenantIds: user.tenants?.map((t) => t.id) ?? [],
  }
}

export const adminApi = {
  async listTenants(): Promise<Tenant[]> {
    if (env.mockAuth) {
      return mockListTenants()
    }
    const data = await apiRequest<Tenant[] | PagedResult<Tenant>>(
      `/api/admin/tenants?page=1&pageSize=100`,
    )
    return asList(data)
  },

  async approveTenant(id: string, password: string): Promise<void> {
    if (env.mockAuth) {
      mockApproveTenant(id, password)
      return
    }
    await apiRequest<void>(`/api/admin/tenants/${id}/approve`, {
      method: 'POST',
      body: { password },
    })
  },

  async rejectTenant(id: string): Promise<void> {
    if (env.mockAuth) {
      mockRejectTenant(id)
      return
    }
    await apiRequest<void>(`/api/admin/tenants/${id}/reject`, {
      method: 'POST',
    })
  },

  async listUsers(): Promise<UserListItem[]> {
    const data = await apiRequest<BackendUser[] | PagedResult<BackendUser>>(
      `/api/admin/users?page=1&pageSize=100`,
    )
    return asList(data).map(mapUser)
  },

  async listGroups(): Promise<Group[]> {
    const data = await apiRequest<Group[] | PagedResult<Group>>(
      '/api/admin/groups?page=1&pageSize=100',
    )
    return asList(data)
  },
}
