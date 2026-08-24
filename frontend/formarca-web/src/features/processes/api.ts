import { mockFunnel } from '@/features/funnel/mockStore'
import { apiRequest } from '@/shared/api/client'
import { asList, toQuery } from '@/shared/api/types'
import { env } from '@/shared/config/env'
import type { ProcessRecord } from './types'

export const processesApi = {
  async list(): Promise<ProcessRecord[]> {
    if (env.mockAuth) return mockFunnel.listProcesses()
    const data = await apiRequest<ProcessRecord[] | { items: ProcessRecord[] }>(
      `/api/processes${toQuery({ pageSize: 100 })}`,
    )
    return asList(data)
  },
}
