export const INSTALLMENT_NUMBERS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12] as const

export type InstallmentNumber = (typeof INSTALLMENT_NUMBERS)[number]

export type InterestRates = Record<`${InstallmentNumber}`, number> & {
  cardCash: number
}

export type InvoiceStatus =
  | 'PendingPayment'
  | 'AwaitingApproval'
  | 'Paid'
  | 'Rejected'
  | string

export type InvoicePaymentMethod = 'Pix' | 'Card'

export type Invoice = {
  id: string
  contractId: string
  contractCode?: string | null
  tenantId: string
  tenantName?: string | null
  paymentMethod?: InvoicePaymentMethod | null
  installments?: number | null
  contractAmount: number
  discount: number
  expectedAmount: number
  receivedAmount: number
  status: InvoiceStatus
  createdAt?: string
}

export type InvoiceListFilters = {
  status?: string
  tenantId?: string
  contractId?: string
  page?: number
  pageSize?: number
}

export type InvoiceSettings = {
  id?: string
  interestRates: InterestRates
  pixDiscount: number
  generalDiscount: number
}

export type InvoiceSettingsDto = {
  id?: string
  interestRates?: InterestRates | string | null
  interestRatesJson?: string | null
  installmentInterestRates?: Record<string, number> | null
  cardCashInterest?: number
  pixDiscount?: number
  generalDiscount?: number
}

export type SubmitPaymentRequest = {
  paymentMethod: InvoicePaymentMethod
  installments: number
  discount?: number
}

export type ApproveInvoiceRequest = {
  receivedAmount?: number
}
