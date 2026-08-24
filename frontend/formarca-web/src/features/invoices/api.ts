import { mockFunnel } from '@/features/funnel/mockStore'
import { apiRequest } from '@/shared/api/client'
import { ApiError, asList, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import { fromSettingsDto } from './interestRates'
import type {
  ApproveInvoiceRequest,
  Invoice,
  InvoiceSettings,
  InvoiceSettingsDto,
  SubmitPaymentRequest,
} from './types'

export const invoicesApi = {
  async list(): Promise<Invoice[]> {
    if (env.mockAuth) return mockFunnel.listInvoices()
    const data = await apiRequest<Invoice[] | { items: Invoice[] }>(
      `/api/invoices${toQuery({ pageSize: 100 })}`,
    )
    return asList(data)
  },

  async getByContract(contractId: string): Promise<Invoice | null> {
    if (env.mockAuth) return mockFunnel.getInvoiceByContract(contractId)
    try {
      return await apiRequest<Invoice>(`/api/invoices/by-contract/${contractId}`)
    } catch (err) {
      if (err instanceof ApiError && (err.status === 404 || err.status === 400)) {
        return null
      }
      throw err
    }
  },

  async submitPayment(id: string, payload: SubmitPaymentRequest): Promise<Invoice> {
    if (env.mockAuth) return mockFunnel.submitPayment(id, payload)
    return apiRequest<Invoice>(`/api/invoices/${id}/submit-payment`, {
      method: 'POST',
      body: payload,
    })
  },

  async approve(id: string, payload: ApproveInvoiceRequest = {}): Promise<Invoice> {
    if (env.mockAuth) return mockFunnel.approveInvoice(id, payload)
    return apiRequest<Invoice>(`/api/invoices/${id}/approve`, {
      method: 'POST',
      body: payload,
    })
  },

  async reject(id: string): Promise<Invoice> {
    if (env.mockAuth) return mockFunnel.rejectInvoice(id)
    return apiRequest<Invoice>(`/api/invoices/${id}/reject`, { method: 'POST' })
  },

  async getSettings(): Promise<InvoiceSettings> {
    if (env.mockAuth) return mockFunnel.getInvoiceSettings()
    const dto = await apiRequest<InvoiceSettingsDto>('/api/admin/invoice-settings')
    return fromSettingsDto(dto)
  },

  async updateSettings(payload: InvoiceSettings): Promise<InvoiceSettings> {
    if (env.mockAuth) return mockFunnel.updateInvoiceSettings(payload)
    const dto = await apiRequest<InvoiceSettingsDto>('/api/admin/invoice-settings', {
      method: 'PUT',
      body: {
        interestRates: payload.interestRates,
        interestRatesJson: JSON.stringify(payload.interestRates),
        pixDiscount: payload.pixDiscount,
        generalDiscount: payload.generalDiscount,
      },
    })
    return fromSettingsDto(dto)
  },
}
