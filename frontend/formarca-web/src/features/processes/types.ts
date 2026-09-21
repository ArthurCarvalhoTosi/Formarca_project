export type ProcessRecord = {
  id: string
  tenantId: string
  brandId: string
  brandName?: string | null
  contractId: string
  contractCode?: string | null
  status: string
  processNumber?: string | null
  createdAt?: string
  trademarkStatusTitle?: string | null
  trademarkStatusTone?: string | null
}

export type ProcessListFilters = {
  tenantId?: string
  brandId?: string
  page?: number
  pageSize?: number
}

export type FunnelStage = 'Brand' | 'Contract' | 'Invoice' | 'Process'

export type ProcessDashboardFilters = {
  tenantId?: string
  brandId?: string
  funnelStage?: FunnelStage | string
  page?: number
  pageSize?: number
}

export type ProcessDashboardItem = {
  brandId: string
  brandName: string
  tenantId: string
  tenantName: string | null
  contract: { id: string; code: string; status: string } | null
  invoice: { id: string; status: string; expectedAmount: number } | null
  process: { id: string; status: string; processNumber: string | null } | null
  funnelStage: FunnelStage
}

export type ProcessDashboardKpis = {
  brands: number
  contractsInProgress: number
  invoicesPending: number
  processesGenerated: number
}

export type ProcessDashboardResult = {
  items: ProcessDashboardItem[]
  kpis: ProcessDashboardKpis
  total: number
  page: number
  pageSize: number
}

export const EMPTY_DASHBOARD_KPIS: ProcessDashboardKpis = {
  brands: 0,
  contractsInProgress: 0,
  invoicesPending: 0,
  processesGenerated: 0,
}

export const FUNNEL_STAGES: FunnelStage[] = ['Brand', 'Contract', 'Invoice', 'Process']
