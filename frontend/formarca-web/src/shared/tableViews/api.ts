import { apiRequest } from '@/shared/api/client'
import { asList, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import { mockTableViews } from './mockStore'
import type {
  CreateTableViewRequest,
  TableViewDto,
  UpdateTableViewRequest,
} from './types'

export const tableViewsApi = {
  async list(tableKey: string): Promise<TableViewDto[]> {
    if (env.mockAuth) {
      return mockTableViews.list(tableKey)
    }
    const data = await apiRequest<TableViewDto[] | { items: TableViewDto[] }>(
      `/api/me/table-views${toQuery({ tableKey })}`,
    )
    return asList(data)
  },

  async create(body: CreateTableViewRequest): Promise<TableViewDto> {
    if (env.mockAuth) {
      return mockTableViews.create(body)
    }
    return apiRequest<TableViewDto>('/api/me/table-views', {
      method: 'POST',
      body,
    })
  },

  async update(id: string, body: UpdateTableViewRequest): Promise<TableViewDto> {
    if (env.mockAuth) {
      return mockTableViews.update(id, body)
    }
    return apiRequest<TableViewDto>(`/api/me/table-views/${id}`, {
      method: 'PUT',
      body,
    })
  },

  async remove(id: string): Promise<void> {
    if (env.mockAuth) {
      mockTableViews.remove(id)
      return
    }
    await apiRequest<void>(`/api/me/table-views/${id}`, { method: 'DELETE' })
  },

  async activate(id: string): Promise<TableViewDto> {
    if (env.mockAuth) {
      return mockTableViews.activate(id)
    }
    return apiRequest<TableViewDto>(`/api/me/table-views/${id}/activate`, {
      method: 'POST',
    })
  },
}
