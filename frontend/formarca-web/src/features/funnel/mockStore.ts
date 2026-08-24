import { MOCK_ACCESS_TOKEN, MOCK_TENANTS } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { ApiError } from '@/shared/api/types'
import type { Brand, BrandListFilters, BrandPayload } from '@/features/brands/types'
import {
  PRE_ISSUE_COMPANY_DOCUMENT_REQUIRED,
  PRE_ISSUE_REPRESENTATIVE_DOCUMENT_REQUIRED,
} from '@/features/brands/preIssueError'
import type {
  Contract,
  ContractListFilters,
  PreIssueContractRequest,
  UpdateContractRequest,
} from '@/features/contracts/types'
import type {
  ApproveInvoiceRequest,
  Invoice,
  InvoiceSettings,
  SubmitPaymentRequest,
} from '@/features/invoices/types'
import { calculateExpectedAmount } from '@/features/invoices/calc'
import { defaultInterestRates } from '@/features/invoices/interestRates'
import type { ProcessRecord } from '@/features/processes/types'
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
  return {
    ...contract,
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

export const mockFunnel = {
  listBrands(filters: BrandListFilters = {}): Brand[] {
    const tenantId = filters.tenantId || scopedTenantId()
    return brands
      .filter((brand) => (tenantId ? brand.tenantId === tenantId : true))
      .filter((brand) =>
        filters.name
          ? brand.name.toLowerCase().includes(filters.name.trim().toLowerCase())
          : true,
      )
      .filter((brand) =>
        filters.inpiClass
          ? brand.inpiClasses.includes(Number(filters.inpiClass))
          : true,
      )
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
      .filter((contract) => (filters.status ? contract.status === filters.status : true))
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
    const contract: Contract = {
      id: crypto.randomUUID(),
      code: generateContractCode(),
      tenantId: brand.tenantId,
      tenantName: tenant.name,
      brandId: brand.id,
      companyDocument,
      representativeDocument,
      brandName: payload.brandName || brand.name,
      classes: payload.classes ?? [...brand.inpiClasses],
      amount: payload.amount ?? tenant.quotedAmount ?? 0,
      status: 'PreIssued',
      formarcaSignedAt: now,
      clientSignedAt: null,
      generatedAtUtc: now,
      signedAtUtc: null,
      invoiceId: null,
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
    contract.classes = [...payload.classes]
    contract.amount = payload.amount
    return enrichContract(contract)
  },

  issueContract(id: string): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'PreIssued') {
      throw new ApiError(409, 'Only pre-issued contracts can be issued')
    }
    contract.status = 'Issued'
    return enrichContract(contract)
  },

  signContract(id: string): Contract {
    const contract = contracts.find((item) => item.id === id)
    if (!contract) throw new ApiError(404, 'Contract not found')
    assertContractScope(contract)
    if (contract.status !== 'Issued') {
      throw new ApiError(409, 'Only issued contracts can be signed')
    }
    const now = new Date().toISOString()
    contract.status = 'Signed'
    contract.clientSignedAt = now
    contract.signedAtUtc = now
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

  downloadContract(id: string, format: 'pdf' | 'docx'): { blob: Blob; filename: string } {
    const contract = this.getContract(id)
    const classes = parseClasses(contract.classes).join(', ')
    const body = [
      `Formarca contract ${contract.code}`,
      `Brand: ${contract.brandName}`,
      `CNPJ: ${contract.companyDocument}`,
      `CPF: ${contract.representativeDocument}`,
      `Classes: ${classes}`,
      `Amount: ${contract.amount}`,
      `Status: ${contract.status}`,
    ].join('\n')
    const mime =
      format === 'pdf'
        ? 'application/pdf'
        : 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    return {
      blob: new Blob([body], { type: mime }),
      filename: `${contract.code}.${format === 'pdf' ? 'pdf' : 'docx'}`,
    }
  },

  listInvoices(): Invoice[] {
    const tenantId = scopedTenantId()
    return invoices
      .filter((invoice) => (tenantId ? invoice.tenantId === tenantId : true))
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

  listProcesses(): ProcessRecord[] {
    const tenantId = scopedTenantId()
    return processes
      .filter((item) => (tenantId ? item.tenantId === tenantId : true))
      .map(clone)
  },
}
