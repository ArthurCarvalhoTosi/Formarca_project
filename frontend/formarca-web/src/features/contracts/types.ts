export type ContractStatus = 'PreIssued' | 'Issued' | 'Signed' | 'Cancelled' | string

export type PaymentMethod = 'Pix' | 'Card'

export type Contract = {
  id: string
  code: string
  tenantId: string
  tenantName?: string | null
  brandId: string
  companyDocument: string
  representativeDocument: string
  brandName: string
  classes: number[] | string
  amount: number
  status: ContractStatus
  formarcaSignedAt?: string | null
  clientSignedAt?: string | null
  generatedAtUtc?: string | null
  signedAtUtc?: string | null
  paidAmount?: number | null
  paymentMethod?: PaymentMethod | null
  invoiceId?: string | null
  templateKey?: string | null
  bodyMarkdown?: string | null
  companyName?: string | null
  street?: string | null
  number?: string | null
  city?: string | null
  state?: string | null
  zipCode?: string | null
  minimumTerm?: string | null
  autentiqueDocumentId?: string | null
  autentiqueSignUrl?: string | null
  /** pending | finished | rejected — matches AutentiqueDocumentStatus */
  autentiqueStatus?: string | null
  /** GET payloads may omit the PNG; do not rely on this field in the UI. */
  clientSignaturePng?: string | null
}

export type ContractListFilters = {
  clientName?: string
  code?: string
  brandName?: string
  document?: string
  status?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export type PreIssueContractRequest = {
  brandId: string
  amount?: number
  companyDocument?: string
  representativeDocument?: string
  brandName?: string
  classes?: number[]
}

export type UpdateContractRequest = {
  companyDocument: string
  representativeDocument: string
  brandName: string
  classes: number[] | string
  amount: number
  bodyMarkdown?: string | null
  companyName?: string | null
  street?: string | null
  number?: string | null
  city?: string | null
  state?: string | null
  zipCode?: string | null
  minimumTerm?: string | null
}

export type ContractSignLink = {
  signUrl: string
}

/** Matches Formarca.Domain.Contracts.AutentiqueDocumentStatus */
export const AUTENTIQUE_STATUS = {
  pending: 'pending',
  finished: 'finished',
  rejected: 'rejected',
} as const

export type AutentiqueStatus = (typeof AUTENTIQUE_STATUS)[keyof typeof AUTENTIQUE_STATUS]

export function isAutentiqueRejected(status?: string | null): boolean {
  return status?.trim().toLowerCase() === AUTENTIQUE_STATUS.rejected
}

export function hasAutentiqueDocument(contract: Pick<Contract, 'autentiqueDocumentId'>): boolean {
  return Boolean(contract.autentiqueDocumentId?.trim())
}

export function parseContractSignLink(data: unknown): ContractSignLink | null {
  const record = data && typeof data === 'object' ? (data as Record<string, unknown>) : null
  const raw =
    (typeof record?.signUrl === 'string' && record.signUrl) ||
    (typeof record?.SignUrl === 'string' && record.SignUrl) ||
    ''
  const signUrl = raw.trim()
  return signUrl ? { signUrl } : null
}

export const DEFAULT_MINIMUM_TERM = '12 (doze) meses'
export const DEFAULT_TEMPLATE_KEY = 'prestacao-servicos'
