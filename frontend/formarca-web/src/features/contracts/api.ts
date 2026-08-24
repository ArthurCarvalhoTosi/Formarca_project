import { mockFunnel } from '@/features/funnel/mockStore'
import { apiDownload, apiRequest, triggerBlobDownload } from '@/shared/api/client'
import { asList, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import type {
  Contract,
  ContractListFilters,
  PreIssueContractRequest,
  UpdateContractRequest,
} from './types'

function toClassesSnapshot(classes: number[] | string | undefined | null): string | undefined {
  if (classes == null) return undefined
  if (Array.isArray(classes)) return classes.join(', ')
  return classes
}

export const contractsApi = {
  async list(filters: ContractListFilters = {}): Promise<Contract[]> {
    if (env.mockAuth) return mockFunnel.listContracts(filters)
    const data = await apiRequest<Contract[] | { items: Contract[] }>(
      `/api/contracts${toQuery({
        clientName: filters.clientName,
        code: filters.code,
        brandName: filters.brandName,
        document: filters.document,
        status: filters.status,
        from: filters.from,
        to: filters.to,
        pageSize: 100,
      })}`,
    )
    return asList(data)
  },

  async get(id: string): Promise<Contract> {
    if (env.mockAuth) return mockFunnel.getContract(id)
    return apiRequest<Contract>(`/api/contracts/${id}`)
  },

  async preIssue(payload: PreIssueContractRequest): Promise<Contract> {
    if (env.mockAuth) return mockFunnel.preIssue(payload)
    return apiRequest<Contract>('/api/contracts/pre-issue', {
      method: 'POST',
      body: {
        ...payload,
        classes: toClassesSnapshot(payload.classes),
      },
    })
  },

  async update(id: string, payload: UpdateContractRequest): Promise<Contract> {
    if (env.mockAuth) return mockFunnel.updateContract(id, payload)
    return apiRequest<Contract>(`/api/contracts/${id}`, {
      method: 'PUT',
      body: {
        ...payload,
        classes: toClassesSnapshot(payload.classes),
      },
    })
  },

  async issue(id: string): Promise<Contract> {
    if (env.mockAuth) return mockFunnel.issueContract(id)
    return apiRequest<Contract>(`/api/contracts/${id}/issue`, { method: 'POST' })
  },

  async sign(id: string): Promise<Contract> {
    if (env.mockAuth) return mockFunnel.signContract(id)
    return apiRequest<Contract>(`/api/contracts/${id}/sign`, { method: 'POST' })
  },

  async download(id: string, format: 'pdf' | 'docx'): Promise<void> {
    if (env.mockAuth) {
      const file = mockFunnel.downloadContract(id, format)
      triggerBlobDownload(file.blob, file.filename)
      return
    }
    await apiDownload(
      `/api/contracts/${id}/download${toQuery({ format })}`,
      `contract.${format}`,
    )
  },
}
