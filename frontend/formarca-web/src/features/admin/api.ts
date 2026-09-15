import { apiRequest } from '@/shared/api/client'
import { asPaged, paginate, type PagedResult } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import {
  mockApproveTenant,
  mockListTenants,
  mockRejectTenant,
} from '@/features/registration/mockStore'
import type { Group, Tenant, UserListItem } from './types'

type ListPaging = {
  page?: number
  pageSize?: number
}

type BackendUser = {
  id: string
  email: string
  displayName?: string | null
  isPlatformAdmin?: boolean
  isActive?: boolean
  tenants?: { id: string }[]
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
  async listTenants(paging: ListPaging = {}) {
    if (env.mockAuth) {
      return paginate<Tenant>(mockListTenants(), paging.page, paging.pageSize)
    }
    const data = await apiRequest<Tenant[] | PagedResult<Tenant>>(
      `/api/admin/tenants?page=${paging.page ?? 1}&pageSize=${paging.pageSize ?? 20}`,
    )
    return asPaged<Tenant>(data, paging.page, paging.pageSize)
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

  async listUsers(paging: ListPaging = {}) {
    const data = await apiRequest<BackendUser[] | PagedResult<BackendUser>>(
      `/api/admin/users?page=${paging.page ?? 1}&pageSize=${paging.pageSize ?? 20}`,
    )
    const paged = asPaged<BackendUser>(data, paging.page, paging.pageSize)
    return {
      ...paged,
      items: paged.items.map(mapUser),
    }
  },

  async listGroups(paging: ListPaging = {}) {
    const data = await apiRequest<Group[] | PagedResult<Group>>(
      `/api/admin/groups?page=${paging.page ?? 1}&pageSize=${paging.pageSize ?? 20}`,
    )
    return asPaged<Group>(data, paging.page, paging.pageSize)
  },
}
