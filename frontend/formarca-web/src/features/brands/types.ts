export const NICE_CLASSES = Array.from({ length: 45 }, (_, index) => index + 1)

export type Brand = {
  id: string
  tenantId: string
  tenantName?: string | null
  name: string
  servicesDescription: string
  inpiClasses: number[]
  createdAt?: string
}

export type BrandPayload = {
  tenantId?: string
  name: string
  servicesDescription: string
  inpiClasses: number[]
}

export type BrandListFilters = {
  name?: string
  inpiClass?: string | number
  tenantId?: string
  page?: number
  pageSize?: number
}
