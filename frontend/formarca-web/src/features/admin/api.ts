import { apiRequest } from '@/shared/api/client'
import { asPaged, paginate, toQuery, type PagedResult } from '@/shared/api/types'
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
  q?: string
  status?: string
  activeSince?: string
}

function matchesTenantWindow(row: Tenant, filters: ListPaging): boolean {
  const statuses = (filters.status ?? '')
    .split(',')
    .map((value) => value.trim())
    .filter(Boolean)
  if (statuses.length > 0 && !statuses.includes(row.status ?? '')) return false
  if (filters.activeSince && row.status === 'Active') {
    const created = Date.parse(row.createdAt ?? '')
    if (Number.isNaN(created) || created < Date.parse(filters.activeSince)) return false
  }
  const needle = filters.q?.trim().toLowerCase()
  if (!needle) return true
  const digits = needle.replace(/\D/g, '')
  return [row.name, row.slug, row.document, row.email].some((value) => {
    if (!value) return false
    if (value.toLowerCase().includes(needle)) return true
    return Boolean(digits) && value.replace(/\D/g, '').includes(digits)
  })
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

const TENANT_PAGE_SIZE = 100

export const adminApi = {
  async listTenants(paging: ListPaging = {}) {
    if (env.mockAuth) {
      return paginate<Tenant>(
        mockListTenants().filter((row) => matchesTenantWindow(row, paging)),
        paging.page,
        paging.pageSize,
      )
    }
    const data = await apiRequest<Tenant[] | PagedResult<Tenant>>(
      `/api/admin/tenants${toQuery({
        page: paging.page ?? 1,
        pageSize: paging.pageSize ?? 20,
        q: paging.q,
        status: paging.status,
        activeSince: paging.activeSince,
      })}`,
    )
    return asPaged<Tenant>(data, paging.page, paging.pageSize)
  },

  async listAllTenants(): Promise<Tenant[]> {
    if (env.mockAuth) return mockListTenants()
    const first = await this.listTenants({ page: 1, pageSize: TENANT_PAGE_SIZE })
    const pageCount = Math.min(10, Math.max(1, Math.ceil(first.total / TENANT_PAGE_SIZE)))
    if (pageCount === 1) return first.items
    const rest = await Promise.all(
      Array.from({ length: pageCount - 1 }, (_, index) =>
        this.listTenants({ page: index + 2, pageSize: TENANT_PAGE_SIZE }),
      ),
    )
    return [...first.items, ...rest.flatMap((page) => page.items)]
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
