import { mockFunnel } from '@/features/funnel/mockStore'
import { apiRequest } from '@/shared/api/client'
import { asPaged, paginate, toQuery, type PagedResult } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import type {
  ProcessDashboardFilters,
  ProcessDashboardKpis,
  ProcessDashboardResult,
  ProcessListFilters,
  ProcessRecord,
} from './types'
import { EMPTY_DASHBOARD_KPIS } from './types'

function readKpis(data: unknown): ProcessDashboardKpis {
  if (!data || typeof data !== 'object') return EMPTY_DASHBOARD_KPIS
  const kpis = (data as { kpis?: ProcessDashboardKpis }).kpis
  if (!kpis) return EMPTY_DASHBOARD_KPIS
  return {
    brands: kpis.brands ?? 0,
    contractsInProgress: kpis.contractsInProgress ?? 0,
    invoicesPending: kpis.invoicesPending ?? 0,
    processesGenerated: kpis.processesGenerated ?? 0,
  }
}

export const processesApi = {
  async list(filters: ProcessListFilters = {}) {
    if (env.mockAuth) {
      return paginate<ProcessRecord>(
        mockFunnel.listProcesses(filters),
        filters.page,
        filters.pageSize,
      )
    }
    const data = await apiRequest<ProcessRecord[] | PagedResult<ProcessRecord>>(
      `/api/processes${toQuery({
        tenantId: filters.tenantId,
        brandId: filters.brandId,
        page: filters.page,
        pageSize: filters.pageSize,
      })}`,
    )
    return asPaged<ProcessRecord>(data, filters.page, filters.pageSize)
  },

  async dashboard(filters: ProcessDashboardFilters = {}): Promise<ProcessDashboardResult> {
    if (env.mockAuth) return mockFunnel.listDashboard(filters)
    const data = await apiRequest<
      PagedResult<ProcessDashboardResult['items'][number]> & { kpis?: ProcessDashboardKpis }
    >(
      `/api/processes/dashboard${toQuery({
        tenantId: filters.tenantId,
        brandId: filters.brandId,
        funnelStage: filters.funnelStage,
        page: filters.page,
        pageSize: filters.pageSize,
      })}`,
    )
    const paged = asPaged<ProcessDashboardResult['items'][number]>(data, filters.page, filters.pageSize)
    return {
      items: paged.items,
      kpis: readKpis(data),
      total: paged.total,
      page: paged.page,
      pageSize: paged.pageSize,
    }
  },
}
