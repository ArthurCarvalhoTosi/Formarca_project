import { mockFunnel } from '@/features/funnel/mockStore'
import { apiRequest } from '@/shared/api/client'
import { asPaged, paginate, toQuery, type PagedResult } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import type { Brand, BrandListFilters, BrandPayload } from './types'

export const brandsApi = {
  async list(filters: BrandListFilters = {}) {
    if (env.mockAuth) {
      return paginate<Brand>(
        mockFunnel.listBrands(filters),
        filters.page,
        filters.pageSize,
      )
    }
    const data = await apiRequest<Brand[] | PagedResult<Brand>>(
      `/api/brands${toQuery({
        name: filters.name,
        inpiClass: filters.inpiClass,
        page: filters.page,
        pageSize: filters.pageSize,
        tenantId: filters.tenantId,
        createdFrom: filters.createdFrom,
      })}`,
    )
    return asPaged<Brand>(data, filters.page, filters.pageSize)
  },

  async get(id: string): Promise<Brand> {
    if (env.mockAuth) return mockFunnel.getBrand(id)
    return apiRequest<Brand>(`/api/brands/${id}`)
  },

  async create(payload: BrandPayload): Promise<Brand> {
    if (env.mockAuth) return mockFunnel.createBrand(payload)
    return apiRequest<Brand>('/api/brands', { method: 'POST', body: payload })
  },

  async update(id: string, payload: BrandPayload): Promise<Brand> {
    if (env.mockAuth) return mockFunnel.updateBrand(id, payload)
    return apiRequest<Brand>(`/api/brands/${id}`, { method: 'PUT', body: payload })
  },

  async remove(id: string): Promise<void> {
    if (env.mockAuth) {
      mockFunnel.deleteBrand(id)
      return
    }
    await apiRequest<void>(`/api/brands/${id}`, { method: 'DELETE' })
  },
}
