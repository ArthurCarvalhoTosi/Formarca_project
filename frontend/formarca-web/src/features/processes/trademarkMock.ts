import { MOCK_ACCESS_TOKEN, MOCK_TENANTS } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { ApiError, paginate } from '@/shared/api/types'
import {
  TRADEMARK_PHASES,
  type ChangeTrademarkStatusPayload,
  type ProcessStatusChange,
  type ProcessTrademarkDetail,
  type TrademarkDashboardFilters,
  type TrademarkDashboardItem,
  type TrademarkDashboardKpis,
  type TrademarkDashboardResult,
  type TrademarkPhase,
  type TrademarkStatus,
  type TrademarkStatusPayload,
  type TrademarkTone,
} from './trademarkTypes'

type Seed = [string, string, string, TrademarkPhase, TrademarkTone]

const CATALOG: Seed[] = [
  ['deposito-efetuado', 'Depósito efetuado / Pedido protocolado', 'O pedido foi recebido e aguarda a triagem inicial.', 'Filing', 'Progress'],
  ['aguardando-exame-formal', 'Aguardando exame formal', 'O INPI está verificando a documentação, enquadramento e comprovantes de pagamento.', 'Filing', 'Progress'],
  ['exigencia-formal', 'Exigência formal', 'Foram identificadas pendências documentais ou de taxa. Há um prazo de 5 dias para cumprimento.', 'Filing', 'Attention'],
  ['pedido-inexistente', 'Pedido considerado inexistente', 'Ocorreu quando uma exigência formal não foi cumprida no prazo ou o pagamento não foi identificado.', 'Filing', 'Danger'],
  ['publicado-oposicao', 'Publicado pedido de registro de marca para oposição', 'O pedido superou o exame formal e foi publicado na RPI (Revista da Propriedade Industrial) para conhecimento público.', 'Opposition', 'Progress'],
  ['aguardando-manifestacao-oposicao', 'Aguardando manifestação sobre oposição', 'Uma empresa ou terceiro protocolou uma oposição ao seu registro. Abre-se o prazo de 60 dias para defesa (contrarrazões).', 'Opposition', 'Attention'],
  ['notificacao-oposicao', 'Notificação de oposição', 'O INPI notifica o titular sobre a existência de uma oposição interposta.', 'Opposition', 'Attention'],
  ['manifestacao-oposicao-apresentada', 'Manifestação à oposição apresentada', 'A defesa contra a oposição foi protocolada com sucesso.', 'Opposition', 'Progress'],
  ['aguardando-exame-merito', 'Aguardando exame de mérito', 'O pedido concluiu a fase de oposição e está na fila para análise técnica dos examinadores.', 'Merit', 'Progress'],
  ['exigencia-merito', 'Exigência de mérito', 'O examinador solicita documentos adicionais, adequação da especificação de produtos/serviços ou esclarecimentos. Prazo de 60 dias para resposta.', 'Merit', 'Attention'],
  ['sobrestamento', 'Sobrestando o exame de mérito (Sobrestamento)', 'A análise do pedido foi congelada/pausada aguardando a decisão final de outro processo (ex.: uma marca idêntica/similar anterior que está em fase de recurso ou caducidade).', 'Merit', 'Paused'],
  ['deferimento', 'Deferimento do pedido de registro', 'O pedido foi aprovado. Abre-se o prazo de 60 dias para pagamento das taxas federais do primeiro decênio e emissão do certificado.', 'Decision', 'Success'],
  ['indeferimento', 'Indeferimento do pedido de registro', 'O pedido foi negado pelo INPI. Cabe recurso no prazo de 60 dias.', 'Decision', 'Danger'],
  ['recurso-interposto', 'Recurso interposto / Publicado recurso', 'O recurso contra o indeferimento foi protocolado e aguarda análise da segunda instância do INPI.', 'Appeal', 'Progress'],
  ['notificacao-contrarrazoes', 'Notificação de contrarrazões ao recurso', 'Caso um terceiro tenha feito o recurso (ex.: após deferimento contestado), você é notificado para responder.', 'Appeal', 'Attention'],
  ['recurso-provido', 'Recurso provido', 'A decisão de indeferimento foi revertida; o pedido passa a ser deferido.', 'Appeal', 'Success'],
  ['recurso-nao-provido', 'Recurso não provido (Mantido o indeferimento)', 'A negação da marca foi mantida de forma definitiva na esfera administrativa.', 'Appeal', 'Danger'],
  ['concessao-registro', 'Concessão de registro', 'As taxas finais foram pagas e o INPI concedeu os direitos exclusivos sobre a marca (válidos por 10 anos).', 'Grant', 'Success'],
  ['certificado-expedido', 'Certificado de registro expedido', 'O documento oficial comprovando a propriedade da marca está disponível para download.', 'Grant', 'Success'],
  ['pedido-arquivado', 'Pedido arquivado definitivamente', 'Ocorreu a aprovação (deferimento), mas o titular não pagou as taxas relativas ao primeiro decênio no prazo legal.', 'Grant', 'Danger'],
  ['aguardando-prorrogacao', 'Aguardando pedido de prorrogação (Renovação)', 'Aproxima-se o fim dos 10 anos e aguarda-se a renovação do registro.', 'Renewal', 'Attention'],
  ['registro-prorrogado', 'Registro prorrogado', 'O titular pagou a taxa do novo decênio, renovando a proteção por mais 10 anos.', 'Renewal', 'Success'],
  ['registro-extinto', 'Registro extinto / Anotada a extinção', 'A marca perdeu a validade por falta de renovação (caducidade de vigência), renúncia do titular ou declaração de nulidade.', 'Extinction', 'Danger'],
]

let statuses: TrademarkStatus[] = CATALOG.map(([code, title, description, phase, tone], index) => ({
  id: `mock-status-${code}`,
  code,
  title,
  description,
  sortOrder: (index + 1) * 10,
  phase,
  tone,
  isActive: true,
  createdAtUtc: '2026-01-01T12:00:00.000Z',
}))

type MockProcess = TrademarkDashboardItem & { history: ProcessStatusChange[] }

function statusByCode(code: string): TrademarkStatus {
  const found = statuses.find((status) => status.code === code)
  if (!found) throw new Error(`Missing mock status ${code}`)
  return found
}

function change(
  id: string,
  toCode: string,
  occurredAtUtc: string,
  note?: string,
  fromCode?: string,
): ProcessStatusChange {
  const to = statusByCode(toCode)
  const from = fromCode ? statusByCode(fromCode) : null
  return {
    id,
    fromStatusId: from?.id ?? null,
    fromStatusTitle: from?.title ?? null,
    toStatusId: to.id,
    toStatusTitle: to.title,
    note: note ?? null,
    occurredAtUtc,
  }
}

let processes: MockProcess[] = [
  {
    processId: 'mock-process-demo',
    brandId: 'mock-brand-demo',
    brandName: 'Demo Marcas',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    contractId: 'mock-contract-demo',
    contractCode: 'FM-2026-001',
    processNumber: '924.567.890',
    createdAtUtc: '2026-02-02T12:00:00.000Z',
    status: statusByCode('exigencia-formal'),
    statusChangedAtUtc: '2026-03-12T15:00:00.000Z',
    latestNote: 'Falta o comprovante da GRU.',
    history: [
      change('mock-change-demo-2', 'exigencia-formal', '2026-03-12T15:00:00.000Z', 'Falta o comprovante da GRU.', 'aguardando-exame-formal'),
      change('mock-change-demo-1', 'aguardando-exame-formal', '2026-02-10T12:00:00.000Z', undefined, 'deposito-efetuado'),
      change('mock-change-demo-0', 'deposito-efetuado', '2026-02-02T12:00:00.000Z'),
    ],
  },
  {
    processId: 'mock-process-norte',
    brandId: 'mock-brand-norte',
    brandName: 'Norte Café',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    contractId: 'mock-contract-norte',
    contractCode: 'FM-2026-014',
    processNumber: '930.112.441',
    createdAtUtc: '2025-11-04T12:00:00.000Z',
    status: statusByCode('deferimento'),
    statusChangedAtUtc: '2026-08-20T12:00:00.000Z',
    latestNote: 'Prazo de 60 dias para a taxa do primeiro decênio.',
    history: [
      change('mock-change-norte-1', 'deferimento', '2026-08-20T12:00:00.000Z', 'Prazo de 60 dias para a taxa do primeiro decênio.', 'aguardando-exame-merito'),
      change('mock-change-norte-0', 'aguardando-exame-merito', '2026-04-01T12:00:00.000Z'),
    ],
  },
  {
    processId: 'mock-process-lume',
    brandId: 'mock-brand-lume',
    brandName: 'Lume',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    contractId: 'mock-contract-lume',
    contractCode: 'FM-2024-088',
    processNumber: '912.004.220',
    createdAtUtc: '2024-06-18T12:00:00.000Z',
    status: statusByCode('concessao-registro'),
    statusChangedAtUtc: '2026-01-15T12:00:00.000Z',
    latestNote: null,
    history: [change('mock-change-lume-0', 'concessao-registro', '2026-01-15T12:00:00.000Z')],
  },
  {
    processId: 'mock-process-vega',
    brandId: 'mock-brand-vega',
    brandName: 'Vega Studio',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    contractId: 'mock-contract-vega',
    contractCode: 'FM-2026-040',
    processNumber: null,
    createdAtUtc: '2026-09-01T12:00:00.000Z',
    status: null,
    statusChangedAtUtc: null,
    latestNote: null,
    history: [],
  },
  {
    processId: 'mock-process-acme',
    brandId: 'mock-brand-acme',
    brandName: 'Acme',
    tenantId: MOCK_TENANTS[1].id,
    tenantName: MOCK_TENANTS[1].name,
    contractId: 'mock-contract-acme',
    contractCode: 'FM-2026-007',
    processNumber: '928.300.112',
    createdAtUtc: '2026-01-20T12:00:00.000Z',
    status: statusByCode('aguardando-exame-merito'),
    statusChangedAtUtc: '2026-07-02T12:00:00.000Z',
    latestNote: null,
    history: [change('mock-change-acme-0', 'aguardando-exame-merito', '2026-07-02T12:00:00.000Z')],
  },
  {
    processId: 'mock-process-atlas',
    brandId: 'mock-brand-atlas',
    brandName: 'Atlas',
    tenantId: MOCK_TENANTS[1].id,
    tenantName: MOCK_TENANTS[1].name,
    contractId: 'mock-contract-atlas',
    contractCode: 'FM-2025-221',
    processNumber: '918.774.009',
    createdAtUtc: '2025-05-09T12:00:00.000Z',
    status: statusByCode('sobrestamento'),
    statusChangedAtUtc: '2026-06-11T12:00:00.000Z',
    latestNote: 'Aguarda o desfecho de marca anterior semelhante.',
    history: [
      change('mock-change-atlas-0', 'sobrestamento', '2026-06-11T12:00:00.000Z', 'Aguarda o desfecho de marca anterior semelhante.'),
    ],
  },
]

function isMockAdmin(): boolean {
  return tokenStorage.getAccessToken() === MOCK_ACCESS_TOKEN
}

function visibleProcesses(): MockProcess[] {
  if (isMockAdmin()) return processes
  const tenantId = tokenStorage.getActiveTenantId()
  if (!tenantId) return []
  return processes.filter((process) => process.tenantId === tenantId)
}

function cloneStatus(status: TrademarkStatus): TrademarkStatus {
  return { ...status }
}

function toItem(process: MockProcess): TrademarkDashboardItem {
  return {
    processId: process.processId,
    brandId: process.brandId,
    brandName: process.brandName,
    tenantId: process.tenantId,
    tenantName: process.tenantName,
    contractId: process.contractId,
    contractCode: process.contractCode,
    processNumber: process.processNumber,
    createdAtUtc: process.createdAtUtc,
    status: process.status ? cloneStatus(process.status) : null,
    statusChangedAtUtc: process.statusChangedAtUtc,
    latestNote: process.latestNote,
  }
}

function buildKpis(items: MockProcess[]): TrademarkDashboardKpis {
  const phases = TRADEMARK_PHASES.map((phase) => ({
    phase,
    count: items.filter((item) => item.status?.phase === phase).length,
  }))
  return {
    total: items.length,
    withoutStatus: items.filter((item) => !item.status).length,
    actionRequired: items.filter((item) => item.status?.tone === 'Attention').length,
    paused: items.filter((item) => item.status?.tone === 'Paused').length,
    activeRights: items.filter(
      (item) =>
        item.status?.tone === 'Success' &&
        (item.status.phase === 'Grant' || item.status.phase === 'Renewal'),
    ).length,
    closed: items.filter((item) => item.status?.tone === 'Danger').length,
    phases,
  }
}

function slug(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

export const trademarkMock = {
  listStatuses(includeInactive = false): TrademarkStatus[] {
    return statuses
      .filter((status) => includeInactive || status.isActive)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title))
      .map(cloneStatus)
  },

  dashboard(filters: TrademarkDashboardFilters = {}): TrademarkDashboardResult {
    const scoped = visibleProcesses().filter((process) => {
      if (filters.tenantId && process.tenantId !== filters.tenantId) return false
      if (filters.brandId && process.brandId !== filters.brandId) return false
      return true
    })
    const kpis = buildKpis(scoped)
    const filtered = scoped.filter((process) => {
      if (filters.statusId && process.status?.id !== filters.statusId) return false
      if (filters.phase && process.status?.phase !== filters.phase) return false
      return true
    })
    filtered.sort((a, b) => {
      const left = a.statusChangedAtUtc ?? a.createdAtUtc
      const right = b.statusChangedAtUtc ?? b.createdAtUtc
      return right.localeCompare(left)
    })
    const page = paginate(filtered.map(toItem), filters.page, filters.pageSize ?? 12)
    return { ...page, kpis }
  },

  detail(processId: string): ProcessTrademarkDetail {
    const process = visibleProcesses().find((item) => item.processId === processId)
    if (!process) throw new Error('Process not found.')
    return {
      ...toItem(process),
      history: process.history.map((entry) => ({ ...entry })),
    }
  },

  changeStatus(processId: string, payload: ChangeTrademarkStatusPayload): ProcessTrademarkDetail {
    const process = processes.find((item) => item.processId === processId)
    if (!process || !visibleProcesses().some((item) => item.processId === processId)) {
      throw new ApiError(404, 'Process not found.')
    }
    const next = statuses.find((status) => status.id === payload.trademarkStatusId && status.isActive)
    if (!next) throw new ApiError(404, 'Trademark status not found.')
    const same = process.status?.id === next.id
    const nextNumber =
      payload.processNumber === undefined ? process.processNumber : payload.processNumber.trim() || null
    const numberChanged = payload.processNumber !== undefined && nextNumber !== process.processNumber
    if (same && !numberChanged) {
      throw new ApiError(409, 'Process already has this status.')
    }
    if (!same) {
      const entry = change(
        `mock-change-${crypto.randomUUID()}`,
        next.code,
        new Date().toISOString(),
        payload.note,
        process.status?.code,
      )
      process.history = [entry, ...process.history]
      process.status = cloneStatus(next)
      process.statusChangedAtUtc = entry.occurredAtUtc
      process.latestNote = payload.note?.trim() || null
    }
    if (payload.processNumber !== undefined) {
      process.processNumber = payload.processNumber.trim() || null
    }
    return this.detail(processId)
  },

  create(payload: TrademarkStatusPayload): TrademarkStatus {
    const base = slug(payload.code || payload.title) || 'status'
    let code = base
    let suffix = 2
    while (statuses.some((status) => status.code === code)) {
      code = `${base}-${suffix}`
      suffix += 1
    }
    const max = statuses.reduce((current, status) => Math.max(current, status.sortOrder), 0)
    const created: TrademarkStatus = {
      id: `mock-status-${crypto.randomUUID()}`,
      code,
      title: payload.title.trim(),
      description: payload.description.trim(),
      sortOrder: payload.sortOrder ?? max + 10,
      phase: payload.phase,
      tone: payload.tone,
      isActive: payload.isActive,
      createdAtUtc: new Date().toISOString(),
    }
    statuses = [...statuses, created]
    return cloneStatus(created)
  },

  update(id: string, payload: TrademarkStatusPayload): TrademarkStatus {
    const current = statuses.find((status) => status.id === id)
    if (!current) throw new Error('Trademark status not found.')
    current.title = payload.title.trim()
    current.description = payload.description.trim()
    current.phase = payload.phase
    current.tone = payload.tone
    current.isActive = payload.isActive
    if (payload.sortOrder != null) current.sortOrder = payload.sortOrder
    current.updatedAtUtc = new Date().toISOString()
    for (const process of processes) {
      if (process.status?.id === id) process.status = cloneStatus(current)
    }
    return cloneStatus(current)
  },

  remove(id: string): void {
    const used = processes.some(
      (process) =>
        process.status?.id === id ||
        process.history.some((entry) => entry.toStatusId === id || entry.fromStatusId === id),
    )
    if (used) {
      throw new ApiError(409, 'Status is in use and cannot be deleted. Deactivate it instead.')
    }
    statuses = statuses.filter((status) => status.id !== id)
  },

  reorder(orderedIds: string[]): TrademarkStatus[] {
    const byId = new Map(statuses.map((status) => [status.id, status]))
    if (orderedIds.length !== statuses.length || new Set(orderedIds).size !== orderedIds.length) {
      throw new Error('Reorder must include every status once.')
    }
    orderedIds.forEach((id, index) => {
      const status = byId.get(id)
      if (!status) throw new Error('Trademark status not found.')
      status.sortOrder = (index + 1) * 10
    })
    return this.listStatuses(true)
  },
}
