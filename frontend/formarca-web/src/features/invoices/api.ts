import { mockFunnel } from '@/features/funnel/mockStore'
import { apiRequest } from '@/shared/api/client'
import { ApiError, asPaged, paginate, toQuery, type PagedResult } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import { fromSettingsDto } from './interestRates'
import type {
  ApproveInvoiceRequest,
  Invoice,
  InvoiceListFilters,
  InvoiceSettings,
  InvoiceSettingsDto,
  SubmitPaymentRequest,
} from './types'

export const invoicesApi = {
  async list(filters: InvoiceListFilters = {}) {
    if (env.mockAuth) {
      return paginate<Invoice>(
        mockFunnel.listInvoices(filters),
        filters.page,
        filters.pageSize,
      )
    }
    const data = await apiRequest<Invoice[] | PagedResult<Invoice>>(
      `/api/invoices${toQuery({
        status: filters.status,
        tenantId: filters.tenantId,
        contractId: filters.contractId,
        activeSince: filters.activeSince,
        page: filters.page,
        pageSize: filters.pageSize,
      })}`,
    )
    return asPaged<Invoice>(data, filters.page, filters.pageSize)
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
