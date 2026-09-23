export const TRADEMARK_PHASES = [
  'Filing',
  'Opposition',
  'Merit',
  'Decision',
  'Appeal',
  'Grant',
  'Renewal',
  'Extinction',
] as const

export const TRADEMARK_TONES = [
  'Neutral',
  'Progress',
  'Attention',
  'Success',
  'Danger',
  'Paused',
] as const

export type TrademarkPhase = (typeof TRADEMARK_PHASES)[number]
export type TrademarkTone = (typeof TRADEMARK_TONES)[number]

export type TrademarkStatus = {
  id: string
  code: string
  title: string
  description: string
  sortOrder: number
  phase: TrademarkPhase
  tone: TrademarkTone
  isActive: boolean
  createdAtUtc?: string
  updatedAtUtc?: string | null
}

export type TrademarkStatusPayload = {
  title: string
  description: string
  phase: TrademarkPhase
  tone: TrademarkTone
  isActive: boolean
  sortOrder?: number | null
  code?: string | null
}

export type ProcessStatusChange = {
  id: string
  fromStatusId?: string | null
  fromStatusTitle?: string | null
  toStatusId: string
  toStatusTitle: string
  note?: string | null
  changedByUserId?: string | null
  occurredAtUtc: string
}

export type TrademarkDashboardItem = {
  processId: string
  brandId: string
  brandName: string
  tenantId: string
  tenantName?: string | null
  contractId: string
  contractCode?: string | null
  processNumber?: string | null
  createdAtUtc: string
  status?: TrademarkStatus | null
  statusChangedAtUtc?: string | null
  latestNote?: string | null
}

export type TrademarkPhaseCount = {
  phase: TrademarkPhase
  count: number
}

export type TrademarkDashboardKpis = {
  total: number
  withoutStatus: number
  actionRequired: number
  paused: number
  activeRights: number
  closed: number
  phases: TrademarkPhaseCount[]
}

export type TrademarkDashboardResult = {
  items: TrademarkDashboardItem[]
  kpis: TrademarkDashboardKpis
  total: number
  page: number
  pageSize: number
}

export type TrademarkDashboardFilters = {
  tenantId?: string
  brandId?: string
  statusId?: string
  phase?: TrademarkPhase | ''
  tone?: string
  activeTone?: string
  includeWithoutStatus?: boolean
  activeSince?: string
  page?: number
  pageSize?: number
}

export type ProcessTrademarkDetail = {
  processId: string
  brandId: string
  brandName: string
  tenantId: string
  tenantName?: string | null
  contractId: string
  contractCode?: string | null
  processNumber?: string | null
  status?: TrademarkStatus | null
  statusChangedAtUtc?: string | null
  history: ProcessStatusChange[]
}

export type ChangeTrademarkStatusPayload = {
  trademarkStatusId: string
  note?: string
  processNumber?: string
}

export const EMPTY_TRADEMARK_KPIS: TrademarkDashboardKpis = {
  total: 0,
  withoutStatus: 0,
  actionRequired: 0,
  paused: 0,
  activeRights: 0,
  closed: 0,
  phases: TRADEMARK_PHASES.map((phase) => ({ phase, count: 0 })),
}
