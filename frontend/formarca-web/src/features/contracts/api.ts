import { mockFunnel } from '@/features/funnel/mockStore'
import { apiDownload, apiFetchBlob, apiRequest, triggerBlobDownload } from '@/shared/api/client'
import { ApiError, asList, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import {
  parseContractSignLink,
  type Contract,
  type ContractListFilters,
  type ContractSignLink,
  type PreIssueContractRequest,
  type UpdateContractRequest,
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

  async getSignLink(id: string): Promise<ContractSignLink> {
    if (env.mockAuth) return mockFunnel.getContractSignLink(id)
    const data = await apiRequest<unknown>(`/api/contracts/${id}/sign-link`)
    const parsed = parseContractSignLink(data)
    if (!parsed) {
      throw new ApiError(502, 'Signature link response did not include signUrl', data)
    }
    return parsed
  },

  async simulateAutentiqueSign(id: string): Promise<Contract> {
    if (!env.mockAuth) {
      throw new ApiError(400, 'Autentique simulation is only available in mock mode')
    }
    return mockFunnel.simulateAutentiqueSign(id)
  },

  async preview(id: string): Promise<Blob> {
    if (env.mockAuth) return mockFunnel.previewContract(id)
    const file = await apiFetchBlob(`/api/contracts/${id}/preview`, 'application/pdf')
    return file.blob
  },

  async download(id: string): Promise<void> {
    if (env.mockAuth) {
      const file = mockFunnel.downloadContract(id)
      triggerBlobDownload(file.blob, file.filename)
      return
    }
    await apiDownload(`/api/contracts/${id}/download?format=pdf`, 'contract.pdf')
  },
}
