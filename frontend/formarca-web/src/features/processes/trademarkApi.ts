import { apiRequest } from '@/shared/api/client'
import { asPaged, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import { trademarkMock } from './trademarkMock'
import {
  EMPTY_TRADEMARK_KPIS,
  type ChangeTrademarkStatusPayload,
  type ProcessTrademarkDetail,
  type TrademarkDashboardFilters,
  type TrademarkDashboardKpis,
  type TrademarkDashboardResult,
  type TrademarkStatus,
  type TrademarkStatusPayload,
} from './trademarkTypes'

function readKpis(data: unknown): TrademarkDashboardKpis {
  if (!data || typeof data !== 'object') return EMPTY_TRADEMARK_KPIS
  const kpis = (data as { kpis?: TrademarkDashboardKpis }).kpis
  if (!kpis) return EMPTY_TRADEMARK_KPIS
  return {
    total: kpis.total ?? 0,
    withoutStatus: kpis.withoutStatus ?? 0,
    actionRequired: kpis.actionRequired ?? 0,
    paused: kpis.paused ?? 0,
    activeRights: kpis.activeRights ?? 0,
    closed: kpis.closed ?? 0,
    phases: kpis.phases ?? EMPTY_TRADEMARK_KPIS.phases,
  }
}

export const trademarkApi = {
  async listStatuses(includeInactive = false) {
    if (env.mockAuth) return trademarkMock.listStatuses(includeInactive)
    return apiRequest<TrademarkStatus[]>(
      `/api/trademark-statuses${toQuery({ includeInactive: includeInactive ? 'true' : undefined })}`,
    )
  },

  async dashboard(filters: TrademarkDashboardFilters = {}): Promise<TrademarkDashboardResult> {
    if (env.mockAuth) return trademarkMock.dashboard(filters)
    const data = await apiRequest<TrademarkDashboardResult>(
      `/api/processes/trademark-dashboard${toQuery({
        tenantId: filters.tenantId,
        brandId: filters.brandId,
        statusId: filters.statusId,
        phase: filters.phase || undefined,
        page: filters.page,
        pageSize: filters.pageSize,
      })}`,
    )
    const paged = asPaged(data.items ? data : { items: [] }, filters.page, filters.pageSize)
    return {
      items: paged.items,
      kpis: readKpis(data),
      total: paged.total,
      page: paged.page,
      pageSize: paged.pageSize,
    }
  },

  detail(processId: string) {
    if (env.mockAuth) return Promise.resolve(trademarkMock.detail(processId))
    return apiRequest<ProcessTrademarkDetail>(`/api/processes/${processId}/trademark-status`)
  },

  changeStatus(processId: string, payload: ChangeTrademarkStatusPayload) {
    if (env.mockAuth) return Promise.resolve(trademarkMock.changeStatus(processId, payload))
    return apiRequest<ProcessTrademarkDetail>(`/api/processes/${processId}/trademark-status`, {
      method: 'PUT',
      body: payload,
    })
  },

  create(payload: TrademarkStatusPayload) {
    if (env.mockAuth) return Promise.resolve(trademarkMock.create(payload))
    return apiRequest<TrademarkStatus>('/api/trademark-statuses', { method: 'POST', body: payload })
  },

  update(id: string, payload: TrademarkStatusPayload) {
    if (env.mockAuth) return Promise.resolve(trademarkMock.update(id, payload))
    return apiRequest<TrademarkStatus>(`/api/trademark-statuses/${id}`, { method: 'PUT', body: payload })
  },

  remove(id: string) {
    if (env.mockAuth) {
      trademarkMock.remove(id)
      return Promise.resolve()
    }
    return apiRequest<void>(`/api/trademark-statuses/${id}`, { method: 'DELETE' })
  },

  reorder(orderedIds: string[]) {
    if (env.mockAuth) return Promise.resolve(trademarkMock.reorder(orderedIds))
    return apiRequest<TrademarkStatus[]>('/api/trademark-statuses/reorder', {
      method: 'PUT',
      body: { orderedIds },
    })
  },
}
