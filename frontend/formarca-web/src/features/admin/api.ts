import { apiRequest } from '@/shared/api/client'
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
    const data = await apiRequest<Tenant[] | PagedResult<Tenant>>(
      '/api/admin/tenants',
    )
    return asList(data)
  },

  async listUsers(): Promise<UserListItem[]> {
    const data = await apiRequest<BackendUser[] | PagedResult<BackendUser>>(
      '/api/admin/users',
    )
    return asList(data).map(mapUser)
  },

  async listGroups(): Promise<Group[]> {
    const data = await apiRequest<Group[] | PagedResult<Group>>(
      '/api/admin/groups',
    )
    return asList(data)
  },
}
