import { MOCK_ACCESS_TOKEN, MOCK_TENANTS } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { ApiError, paginate } from '@/shared/api/types'
import { splitCsv } from '@/shared/listSearch'
import type { Brand, BrandListFilters, BrandPayload } from '@/features/brands/types'
import {
  PRE_ISSUE_COMPANY_DOCUMENT_REQUIRED,
  PRE_ISSUE_REPRESENTATIVE_DOCUMENT_REQUIRED,
} from '@/features/brands/preIssueError'
import { buildContractPdf } from '@/features/contracts/mockPdf'
import type {
  Contract,
  ContractListFilters,
  ContractSignLink,
  PreIssueContractRequest,
  UpdateContractRequest,
} from '@/features/contracts/types'
import { DEFAULT_MINIMUM_TERM, DEFAULT_TEMPLATE_KEY } from '@/features/contracts/types'
import type {
  ApproveInvoiceRequest,
  Invoice,
  InvoiceListFilters,
  InvoiceSettings,
  SubmitPaymentRequest,
} from '@/features/invoices/types'
import { calculateExpectedAmount } from '@/features/invoices/calc'
import { defaultInterestRates } from '@/features/invoices/interestRates'
import type {
  FunnelStage,
  ProcessDashboardFilters,
  ProcessDashboardItem,
  ProcessDashboardResult,
  ProcessListFilters,
  ProcessRecord,
} from '@/features/processes/types'
import {
  mockActivateTenant,
  mockGetTenantRecord,
} from '@/features/registration/mockStore'
import { parseClasses } from '@/shared/money'

const brands: Brand[] = [
  {
    id: 'mock-brand-demo',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    name: 'Demo Marcas',
    servicesDescription: 'Marca de demonstração da fundação ForMarca.',
    inpiClasses: [35, 41],
    createdAt: '2026-01-05T00:00:00.000Z',
  },
  {
    id: 'mock-brand-acme',
    tenantId: MOCK_TENANTS[1].id,
    tenantName: MOCK_TENANTS[1].name,
    name: 'Acme',
    servicesDescription: 'Produtos e serviços de marca da Acme.',
    inpiClasses: [25, 35],
    createdAt: '2026-01-06T00:00:00.000Z',
  },
]

function formatBrl(amount: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount)
}

function mergedBodyMarkdown(input: {
  companyName: string
  companyDocument: string
  street: string
  number: string
  city: string
  state: string
  zipCode: string
  brandName: string
  classes: number[] | string
  amount: number
  minimumTerm: string
}): string {
  const classes = parseClasses(input.classes)
  return `CONTRATO DE PRESTAÇÃO DE SERVIÇOS

Pelo presente instrumento particular, de um lado FOR MARCA LTDA, inscrita no CNPJ 62.500.545/0001-14, com sede na Av. Wilma Ritter Beiser, 4870, Cachoeirinha, RS, CEP 94931-130, doravante denominada CONTRATADA, e de outro lado ${input.companyName}, inscrita no CNPJ ${input.companyDocument}, com endereço na ${input.street}, nº ${input.number}, ${input.city}/${input.state}, CEP ${input.zipCode}, doravante denominada CONTRATANTE, têm entre si justo e contratado o seguinte:

1. OBJETO
A CONTRATADA prestará serviços de registro de marca referentes a "${input.brandName}", abrangendo ${classes.length} classe(s) (${classes.join(', ')}), pelo valor de ${formatBrl(input.amount)}.

2. PRAZO
O prazo mínimo de vigência deste contrato é de ${input.minimumTerm}.

3. PREÇO E PAGAMENTO
O valor total dos serviços é de ${formatBrl(input.amount)}, a ser pago nas condições da fatura emitida após a assinatura deste instrumento.

4. OBRIGAÇÕES DA CONTRATADA
Executar os serviços com diligência, manter a CONTRATANTE informada e observar a legislação aplicável ao registro de marcas.

5. OBRIGAÇÕES DA CONTRATANTE
Fornecer documentos, informações e procurações necessárias, bem como efetuar os pagamentos nas datas acordadas.

6. RESCISÃO
O presente contrato poderá ser rescindido nas hipóteses legais, persistindo as obrigações já vencidas.

7. FORO
Fica eleito o foro da comarca de Cachoeirinha/RS para dirimir quaisquer controvérsias oriundas deste contrato.

8. DISPOSIÇÕES GERAIS
Este instrumento substitui entendimentos anteriores sobre o mesmo objeto.
`
}

function documentSnapshot(tenantId: string, extras: {
  companyDocument: string
  brandName: string
  classes: number[] | string
  amount: number
}): Pick<
  Contract,
  | 'templateKey'
  | 'bodyMarkdown'
  | 'companyName'
  | 'street'
  | 'number'
  | 'city'
  | 'state'
  | 'zipCode'
  | 'minimumTerm'
> {
  const tenant = mockGetTenantRecord(tenantId)
  const companyName = tenant?.name ?? ''
  const street = tenant?.address.street ?? ''
  const number = tenant?.address.number ?? ''
  const city = tenant?.address.city ?? ''
  const state = tenant?.address.state ?? ''
  const zipCode = tenant?.address.zipCode ?? ''
  const minimumTerm = DEFAULT_MINIMUM_TERM
  return {
    templateKey: DEFAULT_TEMPLATE_KEY,
    companyName,
    street,
    number,
    city,
    state,
    zipCode,
    minimumTerm,
    bodyMarkdown: mergedBodyMarkdown({
      companyName,
      companyDocument: extras.companyDocument,
      street,
      number,
      city,
      state,
      zipCode,
      brandName: extras.brandName,
      classes: extras.classes,
      amount: extras.amount,
      minimumTerm,
    }),
  }
}

function mockAutentiqueSignUrl(contractId: string): string {
  return `https://assina.ae/mock/${contractId}`
}

function ensureAutentiqueSignLink(contract: Contract): string {
  if (!contract.autentiqueSignUrl) {
    contract.autentiqueDocumentId ??= `mock-autentique-${contract.id}`
    contract.autentiqueSignUrl = mockAutentiqueSignUrl(contract.id)
    contract.autentiqueStatus ??= 'pending'
  }
  return contract.autentiqueSignUrl
}

function hasCompleteSnapshot(contract: Contract): boolean {
  return Boolean(
    contract.companyName?.trim() &&
      contract.street?.trim() &&
      contract.number?.trim() &&
      contract.city?.trim() &&
      contract.state?.trim() &&
      contract.zipCode?.trim() &&
      contract.minimumTerm?.trim() &&
      contract.bodyMarkdown?.trim(),
  )
}

const contracts: Contract[] = [
  {
    id: 'mock-contract-demo',
    code: 'FM-2026-000001',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    brandId: 'mock-brand-demo',
    companyDocument: '39053344705',
    representativeDocument: '39053344705',
    brandName: 'Demo Marcas',
    classes: [35, 41],
    amount: 15000,
    status: 'Signed',
    formarcaSignedAt: '2026-01-10T12:00:00.000Z',
    clientSignedAt: '2026-01-12T15:00:00.000Z',
    generatedAtUtc: '2026-01-10T12:00:00.000Z',
    signedAtUtc: '2026-01-12T15:00:00.000Z',
    invoiceId: 'mock-invoice-demo',
    autentiqueDocumentId: 'mock-autentique-demo',
    autentiqueSignUrl: 'https://assina.ae/mock/mock-contract-demo',
    autentiqueStatus: 'finished',
    ...documentSnapshot(MOCK_TENANTS[0].id, {
      companyDocument: '39053344705',
      brandName: 'Demo Marcas',
      classes: [35, 41],
      amount: 15000,
    }),
  },
  {
    id: 'mock-contract-acme',
    code: 'FM-2026-000002',
    tenantId: MOCK_TENANTS[1].id,
    tenantName: MOCK_TENANTS[1].name,
    brandId: 'mock-brand-acme',
    companyDocument: '12345678000195',
    representativeDocument: '39053344705',
    brandName: 'Acme',
    classes: [25, 35],
    amount: 8500,
    status: 'Issued',
    formarcaSignedAt: '2026-02-01T12:00:00.000Z',
    clientSignedAt: null,
    generatedAtUtc: '2026-02-01T12:00:00.000Z',
    signedAtUtc: null,
    invoiceId: null,
    autentiqueDocumentId: 'mock-autentique-acme',
    autentiqueSignUrl: 'https://assina.ae/mock/mock-contract-acme',
    autentiqueStatus: 'pending',
    ...documentSnapshot(MOCK_TENANTS[1].id, {
      companyDocument: '12345678000195',
      brandName: 'Acme',
      classes: [25, 35],
      amount: 8500,
    }),
  },
  {
    id: 'mock-contract-draft',
    code: 'FM-2026-000003',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    brandId: 'mock-brand-demo',
    companyDocument: '39053344705',
    representativeDocument: '39053344705',
    brandName: 'Demo Marcas',
    classes: [35, 41],
    amount: 15000,
    status: 'PreIssued',
    formarcaSignedAt: '2026-03-01T12:00:00.000Z',
    clientSignedAt: null,
    generatedAtUtc: '2026-03-01T12:00:00.000Z',
    signedAtUtc: null,
    invoiceId: null,
    ...documentSnapshot(MOCK_TENANTS[0].id, {
      companyDocument: '39053344705',
      brandName: 'Demo Marcas',
      classes: [35, 41],
      amount: 15000,
    }),
  },
]

const invoices: Invoice[] = [
  {
    id: 'mock-invoice-demo',
    contractId: 'mock-contract-demo',
    contractCode: 'FM-2026-000001',
    tenantId: MOCK_TENANTS[0].id,
    tenantName: MOCK_TENANTS[0].name,
    paymentMethod: 'Pix',
    installments: 1,
    contractAmount: 15000,
    discount: 0,
    expectedAmount: 14250,
    receivedAmount: 14250,
    status: 'Paid',
    createdAt: '2026-01-12T15:00:00.000Z',
  },
]

const processes: ProcessRecord[] = [
  {
    id: 'mock-process-demo',
    tenantId: MOCK_TENANTS[0].id,
    brandId: 'mock-brand-demo',
    brandName: 'Demo Marcas',
    contractId: 'mock-contract-demo',
    contractCode: 'FM-2026-000001',
    status: 'Generated',
    processNumber: null,
    createdAt: '2026-01-13T10:00:00.000Z',
  },
]

let invoiceSettings: InvoiceSettings = {
  id: 'mock-invoice-settings',
  interestRates: {
    ...defaultInterestRates(0),
    '1': 0,
    '2': 2.99,
    '3': 4.99,
    '4': 6.49,
    '5': 7.99,
    '6': 9.49,
    '7': 10.49,
    '8': 11.49,
    '9': 12.49,
    '10': 13.29,
    '11': 14.09,
    '12': 14.99,
    cardCash: 0,
  },
  pixDiscount: 5,
  generalDiscount: 0,
}

function isMockAdmin(): boolean {
  return tokenStorage.getAccessToken() === MOCK_ACCESS_TOKEN
}

function scopedTenantId(): string | null {
  if (isMockAdmin()) return null
  return tokenStorage.getActiveTenantId()
}

function clone<T>(value: T): T {
  return structuredClone(value)
}

function enrichContract(contract: Contract): Contract {
  const invoice = invoices.find((item) => item.contractId === contract.id)
  const copy: Contract = { ...contract }
  delete copy.clientSignaturePng
  return {
    ...copy,
    classes: Array.isArray(contract.classes)
      ? [...contract.classes]
      : contract.classes,
    paidAmount: invoice?.status === 'Paid' ? invoice.receivedAmount : null,
    paymentMethod: invoice?.paymentMethod ?? null,
    invoiceId: invoice?.id ?? contract.invoiceId ?? null,
  }
}

function generateContractCode(): string {
  const year = new Date().getUTCFullYear()
  for (let attempt = 0; attempt < 20; attempt += 1) {
    const suffix = String(Math.floor(Math.random() * 1_000_000)).padStart(6, '0')
    const code = `FM-${year}-${suffix}`
    if (!contracts.some((item) => item.code === code)) return code
  }
  return `FM-${year}-${crypto.randomUUID().slice(0, 6).toUpperCase()}`
}

function assertBrandScope(brand: Brand): void {
  const tenantId = scopedTenantId()
  if (tenantId && brand.tenantId !== tenantId) {
    throw new ApiError(404, 'Brand not found')
  }
}

function assertContractScope(contract: Contract): void {
  const tenantId = scopedTenantId()
  if (tenantId && contract.tenantId !== tenantId) {
    throw new ApiError(404, 'Contract not found')
  }
}

function contractRank(contract: Contract): number {
  if (processes.some((item) => item.contractId === contract.id)) return 5
  const invoice = invoices.find((item) => item.contractId === contract.id)
  if (invoice?.status === 'Paid') return 4
  if (invoice) return 3
  if (contract.status === 'Signed') return 2
  if (contract.status === 'Issued') return 1
  return 0
}

function pickBrandContract(brandId: string): Contract | null {
  const related = contracts.filter((contract) => contract.brandId === brandId)
  if (related.length === 0) return null
  return [...related].sort((left, right) => {
    const rank = contractRank(right) - contractRank(left)
    if (rank !== 0) return rank
    return (right.generatedAtUtc ?? '').localeCompare(left.generatedAtUtc ?? '')
  })[0]
}

function funnelStageFor(item: {
  contract: Contract | null
  invoice: Invoice | null
  process: ProcessRecord | null
}): FunnelStage {
  if (item.process) return 'Process'
  if (item.invoice && item.invoice.status !== 'Paid') return 'Invoice'
  if (item.contract) return 'Contract'
  return 'Brand'
}

function dashboardKpis(items: ProcessDashboardItem[]) {
  return {
    brands: items.length,
    contractsInProgress: items.filter(
      (item) =>
        Boolean(item.contract) &&
        !item.process &&
        item.invoice?.status !== 'Paid',
    ).length,
    invoicesPending: items.filter(
      (item) =>
        item.invoice?.status === 'PendingPayment' ||
        item.invoice?.status === 'AwaitingApproval',
    ).length,
    processesGenerated: items.filter((item) => Boolean(item.process)).length,
  }
}

function toDashboardItem(brand: Brand): ProcessDashboardItem {
  const contract = pickBrandContract(brand.id)
  const invoice = contract
    ? invoices.find((item) => item.contractId === contract.id) ?? null
    : null
  const process =
    processes.find((item) =>
      contract ? item.contractId === contract.id : item.brandId === brand.id,
    ) ?? null
  return {
    brandId: brand.id,
    brandName: brand.name,
    tenantId: brand.tenantId,
    tenantName: brand.tenantName ?? null,
    contract: contract
      ? { id: contract.id, code: contract.code, status: contract.status }
      : null,
    invoice: invoice
      ? {
          id: invoice.id,
          status: invoice.status,
          expectedAmount: invoice.expectedAmount,
        }
      : null,
    process: process
      ? {
          id: process.id,
          status: process.status,
          processNumber: process.processNumber ?? null,
        }
      : null,
    funnelStage: funnelStageFor({ contract, invoice, process }),
  }
}

export const mockFunnel = {
  listBrands(filters: BrandListFilters = {}): Brand[] {
    const tenantIds = splitCsv(filters.tenantId)
    const scoped = scopedTenantId()
    const classes = splitCsv(String(filters.inpiClass ?? '')).map(Number).filter((n) => !Number.isNaN(n))
    return brands
      .filter((brand) =>
        tenantIds.length > 0
          ? tenantIds.includes(brand.tenantId)
          : scoped
            ? brand.tenantId === scoped
            : true,
      )
      .filter((brand) =>
        filters.name
          ? brand.name.toLowerCase().includes(filters.name.trim().toLowerCase())
          : true,
      )
      .filter((brand) =>
        classes.length > 0 ? classes.some((n) => brand.inpiClasses.includes(n)) : true,
      )
      .filter((brand) => {
        if (!filters.createdFrom) return true
        const created = Date.parse(brand.createdAt ?? '')
        return !Number.isNaN(created) && created >= Date.parse(filters.createdFrom)
      })
      .map(clone)
  },

  getBrand(id: string): Brand {
    const brand = brands.find((item) => item.id === id)
    if (!brand) throw new ApiError(404, 'Brand not found')
    assertBrandScope(brand)
    return clone(brand)
  },

  createBrand(payload: BrandPayload): Brand {
    const tenantId = payload.tenantId || tokenStorage.getActiveTenantId()
    if (!tenantId) throw new ApiError(400, 'tenantId is required')
    const tenant = mockGetTenantRecord(tenantId)
    const brand: Brand = {
      id: crypto.randomUUID(),
      tenantId,
      tenantName: tenant?.name ?? null,
      name: payload.name.trim(),
      servicesDescription: payload.servicesDescription.trim(),
      inpiClasses: [...payload.inpiClasses],
      createdAt: new Date().toISOString(),
    }
    brands.push(brand)
    return clone(brand)
  },

  updateBrand(id: string, payload: BrandPayload): Brand {
    const brand = brands.find((item) => item.id === id)
    if (!brand) throw new ApiError(404, 'Brand not found')
    assertBrandScope(brand)
    brand.name = payload.name.trim()
    brand.servicesDescription = payload.servicesDescription.trim()
    brand.inpiClasses = [...payload.inpiClasses]
    if (payload.tenantId && isMockAdmin()) {
      brand.tenantId = payload.tenantId
      brand.tenantName = mockGetTenantRecord(payload.tenantId)?.name ?? brand.tenantName
    }
    return clone(brand)
  },

  deleteBrand(id: string): void {
    const index = brands.findIndex((item) => item.id === id)
    if (index < 0) throw new ApiError(404, 'Brand not found')
    assertBrandScope(brands[index])
    if (contracts.some((contract) => contract.brandId === id)) {
      throw new ApiError(409, 'Brand has contracts')
    }
    brands.splice(index, 1)
  },

  listContracts(filters: ContractListFilters = {}): Contract[] {
    const tenantId = scopedTenantId()
    const from = filters.from ? new Date(filters.from) : null
    const to = filters.to ? new Date(filters.to) : null
    return contracts
      .filter((contract) => (tenantId ? contract.tenantId === tenantId : true))
      .filter((contract) =>
        filters.clientName
          ? (contract.tenantName ?? '')
              .toLowerCase()
              .includes(filters.clientName.trim().toLowerCase())
          : true,
      )
      .filter((contract) =>
        filters.code
          ? contract.code.toLowerCase().includes(filters.code.trim().toLowerCase())
          : true,
      )
      .filter((contract) =>
        filters.brandName
          ? contract.brandName.toLowerCase().includes(filters.brandName.trim().toLowerCase())
          : true,
      )
      .filter((contract) => {
        if (!filters.document) return true
        const digits = filters.document.replace(/\D/g, '')
        return (
          contract.companyDocument.includes(digits) ||
          contract.representativeDocument.includes(digits)
        )
      })
      .filter((contract) => {
        const statuses = splitCsv(filters.status)
        if (statuses.length === 0) return true
        if (!statuses.includes(contract.status)) return false
        if (filters.activeSince && contract.status === 'Signed') {
          const created = Date.parse(contract.generatedAtUtc ?? '')
          return !Number.isNaN(created) && created >= Date.parse(filters.activeSince)
        }
        return true
      })
      .filter((contract) => {
        if (!from && !to) return true
        if (!contract.generatedAtUtc) return false
        const generated = new Date(contract.generatedAtUtc)
        if (from && generated < from) return false
        if (to && generated > to) return false
        return true
      })
      .map((contract) => enrichContract(contract))
  },

  getContract(id: string): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    return enrichContract(contract)
  },

  preIssue(payload: PreIssueContractRequest): Contract {
    const brand = brands.find((item) => item.id === payload.brandId)
    if (!brand) throw new ApiError(404, 'Brand not found')
    const tenant = mockGetTenantRecord(brand.tenantId)
    if (!tenant) throw new ApiError(404, 'Customer not found')
    const companyDocument = payload.companyDocument || tenant.document
    const representativeDocument =
      payload.representativeDocument || tenant.legalRepresentativeDocument
    if (!companyDocument?.trim()) {
      throw new ApiError(409, PRE_ISSUE_COMPANY_DOCUMENT_REQUIRED)
    }
    if (!representativeDocument?.trim()) {
      throw new ApiError(409, PRE_ISSUE_REPRESENTATIVE_DOCUMENT_REQUIRED)
    }
    const now = new Date().toISOString()
    const brandName = payload.brandName || brand.name
    const classes = payload.classes ?? [...brand.inpiClasses]
    const amount = payload.amount ?? tenant.quotedAmount ?? 0
    const contract: Contract = {
      id: crypto.randomUUID(),
      code: generateContractCode(),
      tenantId: brand.tenantId,
      tenantName: tenant.name,
      brandId: brand.id,
      companyDocument,
      representativeDocument,
      brandName,
      classes,
      amount,
      status: 'PreIssued',
      formarcaSignedAt: now,
      clientSignedAt: null,
      generatedAtUtc: now,
      signedAtUtc: null,
      invoiceId: null,
      ...documentSnapshot(brand.tenantId, {
        companyDocument,
        brandName,
        classes,
        amount,
      }),
    }
    contracts.push(contract)
    return enrichContract(contract)
  },

  updateContract(id: string, payload: UpdateContractRequest): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'PreIssued') {
      throw new ApiError(409, 'Only pre-issued contracts can be edited')
    }
    contract.companyDocument = payload.companyDocument
    contract.representativeDocument = payload.representativeDocument
    contract.brandName = payload.brandName
    contract.classes = Array.isArray(payload.classes)
      ? [...payload.classes]
      : payload.classes
    contract.amount = payload.amount
    if (payload.bodyMarkdown != null) contract.bodyMarkdown = payload.bodyMarkdown
    if (payload.companyName != null) contract.companyName = payload.companyName
    if (payload.street != null) contract.street = payload.street
    if (payload.number != null) contract.number = payload.number
    if (payload.city != null) contract.city = payload.city
    if (payload.state != null) contract.state = payload.state
    if (payload.zipCode != null) contract.zipCode = payload.zipCode
    if (payload.minimumTerm != null) contract.minimumTerm = payload.minimumTerm
    return enrichContract(contract)
  },

  issueContract(id: string): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'PreIssued') {
      throw new ApiError(409, 'Only pre-issued contracts can be issued')
    }
    if (!hasCompleteSnapshot(contract)) {
      throw new ApiError(
        409,
        'Contract cannot be issued until company name, address, period, and body are complete.',
      )
    }
    contract.status = 'Issued'
    ensureAutentiqueSignLink(contract)
    return enrichContract(contract)
  },

  getContractSignLink(id: string): ContractSignLink {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'Issued') {
      throw new ApiError(409, 'Signature link is available only for issued contracts.')
    }
    if (!contract.autentiqueDocumentId && !contract.autentiqueSignUrl) {
      throw new ApiError(409, 'This contract has no Autentique signature link.')
    }
    return { signUrl: ensureAutentiqueSignLink(contract) }
  },

  simulateAutentiqueSign(id: string): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'Issued') {
      throw new ApiError(409, 'Only issued contracts can be signed')
    }
    if (contract.autentiqueStatus === 'rejected') {
      throw new ApiError(409, 'This contract was rejected in Autentique.')
    }
    const now = new Date().toISOString()
    contract.status = 'Signed'
    contract.clientSignedAt = now
    contract.signedAtUtc = now
    contract.autentiqueStatus = 'finished'
    ensureAutentiqueSignLink(contract)
    const invoice: Invoice = {
      id: crypto.randomUUID(),
      contractId: contract.id,
      contractCode: contract.code,
      tenantId: contract.tenantId,
      tenantName: contract.tenantName,
      paymentMethod: null,
      installments: null,
      contractAmount: contract.amount,
      discount: 0,
      expectedAmount: contract.amount,
      receivedAmount: 0,
      status: 'PendingPayment',
      createdAt: now,
    }
    invoices.push(invoice)
    contract.invoiceId = invoice.id
    return enrichContract(contract)
  },

  previewContract(id: string): Blob {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (
      contract.status !== 'PreIssued' &&
      contract.status !== 'Issued' &&
      contract.status !== 'Signed'
    ) {
      throw new ApiError(
        409,
        'Contract preview is available for pre-issued, issued, or signed contracts.',
      )
    }
    return buildContractPdf(contract)
  },

  downloadContract(id: string): { blob: Blob; filename: string } {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'Issued' && contract.status !== 'Signed') {
      throw new ApiError(409, 'Contract download is available after issuance.')
    }
    return {
      blob: buildContractPdf(contract),
      filename: `${contract.code}.pdf`,
    }
  },

  listInvoices(filters: InvoiceListFilters = {}): Invoice[] {
    const tenantIds = splitCsv(filters.tenantId)
    const scoped = scopedTenantId()
    const statuses = splitCsv(filters.status)
    return invoices
      .filter((invoice) =>
        tenantIds.length > 0
          ? tenantIds.includes(invoice.tenantId)
          : scoped
            ? invoice.tenantId === scoped
            : true,
      )
      .filter((invoice) => {
        if (statuses.length === 0) return true
        if (!statuses.includes(invoice.status)) return false
        if (filters.activeSince && invoice.status === 'Paid') {
          const created = Date.parse(invoice.createdAt ?? '')
          return !Number.isNaN(created) && created >= Date.parse(filters.activeSince)
        }
        return true
      })
      .filter((invoice) =>
        filters.contractId ? invoice.contractId === filters.contractId : true,
      )
      .map(clone)
  },

  getInvoiceByContract(contractId: string): Invoice | null {
    const contract = contracts.find((item) => item.id === contractId)
    if (contract) assertContractScope(contract)
    const invoice = invoices.find((item) => item.contractId === contractId)
    return invoice ? clone(invoice) : null
  },

  submitPayment(id: string, payload: SubmitPaymentRequest): Invoice {
    const invoice = invoices.find((item) => item.id === id)
    if (!invoice) throw new ApiError(404, 'Invoice not found')
    const tenantId = scopedTenantId()
    if (tenantId && invoice.tenantId !== tenantId) {
      throw new ApiError(404, 'Invoice not found')
    }
    if (invoice.status !== 'PendingPayment' && invoice.status !== 'Rejected') {
      throw new ApiError(409, 'Invoice is not awaiting payment')
    }
    const installments =
      payload.paymentMethod === 'Pix' ? 1 : Math.min(12, Math.max(1, payload.installments))
    invoice.paymentMethod = payload.paymentMethod
    invoice.installments = installments
    invoice.discount = payload.discount ?? invoice.discount
    invoice.expectedAmount = calculateExpectedAmount(
      invoice.contractAmount,
      invoice.discount,
      invoiceSettings,
      payload.paymentMethod,
      installments,
    )
    invoice.status = 'AwaitingApproval'
    return clone(invoice)
  },

  approveInvoice(id: string, payload: ApproveInvoiceRequest = {}): Invoice {
    const invoice = invoices.find((item) => item.id === id)
    if (!invoice) throw new ApiError(404, 'Invoice not found')
    if (invoice.status !== 'AwaitingApproval' && invoice.status !== 'PendingPayment') {
      throw new ApiError(409, 'Invoice cannot be approved')
    }
    invoice.status = 'Paid'
    invoice.receivedAmount = payload.receivedAmount ?? invoice.expectedAmount
    const alreadyProcessed = processes.some((item) => item.contractId === invoice.contractId)
    if (!alreadyProcessed) {
      const contract = contracts.find((item) => item.id === invoice.contractId)
      const brand = contract
        ? brands.find((item) => item.id === contract.brandId)
        : undefined
      processes.push({
        id: crypto.randomUUID(),
        tenantId: invoice.tenantId,
        brandId: contract?.brandId ?? '',
        brandName: brand?.name ?? contract?.brandName,
        contractId: invoice.contractId,
        contractCode: invoice.contractCode,
        status: 'Generated',
        processNumber: null,
        createdAt: new Date().toISOString(),
      })
    }
    mockActivateTenant(invoice.tenantId)
    return clone(invoice)
  },

  rejectInvoice(id: string): Invoice {
    const invoice = invoices.find((item) => item.id === id)
    if (!invoice) throw new ApiError(404, 'Invoice not found')
    if (invoice.status !== 'AwaitingApproval') {
      throw new ApiError(409, 'Invoice cannot be rejected')
    }
    invoice.status = 'Rejected'
    return clone(invoice)
  },

  getInvoiceSettings(): InvoiceSettings {
    return clone(invoiceSettings)
  },

  updateInvoiceSettings(next: InvoiceSettings): InvoiceSettings {
    invoiceSettings = {
      id: invoiceSettings.id,
      interestRates: { ...next.interestRates },
      pixDiscount: next.pixDiscount,
      generalDiscount: next.generalDiscount,
    }
    return clone(invoiceSettings)
  },

  listProcesses(filters: ProcessListFilters = {}): ProcessRecord[] {
    const tenantIds = splitCsv(filters.tenantId)
    const brandIds = splitCsv(filters.brandId)
    const scoped = scopedTenantId()
    return processes
      .filter((item) =>
        tenantIds.length > 0
          ? tenantIds.includes(item.tenantId)
          : scoped
            ? item.tenantId === scoped
            : true,
      )
      .filter((item) => (brandIds.length > 0 ? brandIds.includes(item.brandId) : true))
      .map(clone)
  },

  listDashboard(filters: ProcessDashboardFilters = {}): ProcessDashboardResult {
    const tenantIds = splitCsv(filters.tenantId)
    const brandIds = splitCsv(filters.brandId)
    const stages = splitCsv(filters.funnelStage)
    const scoped = scopedTenantId()
    const cards = brands
      .filter((brand) =>
        tenantIds.length > 0
          ? tenantIds.includes(brand.tenantId)
          : scoped
            ? brand.tenantId === scoped
            : true,
      )
      .filter((brand) => (brandIds.length > 0 ? brandIds.includes(brand.id) : true))
      .map(toDashboardItem)
    const since = filters.activeSince ? Date.parse(filters.activeSince) : NaN
    const items = cards.filter((item) => {
      if (stages.length > 0 && !stages.includes(item.funnelStage)) return false
      if (!Number.isNaN(since) && item.funnelStage === 'Process') {
        const brand = brands.find((row) => row.id === item.brandId)
        const created = Date.parse(brand?.createdAt ?? '')
        return !Number.isNaN(created) && created >= since
      }
      return true
    })
    const paged = paginate(items, filters.page, filters.pageSize)
    return { ...paged, kpis: dashboardKpis(cards) }
  },
}
