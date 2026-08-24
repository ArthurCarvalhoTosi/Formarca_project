export type ProcessRecord = {
  id: string
  tenantId: string
  brandId: string
  brandName?: string | null
  contractId: string
  contractCode?: string | null
  status: string
  processNumber?: string | null
  createdAt?: string
}
