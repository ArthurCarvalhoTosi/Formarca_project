export type Tenant = {
  id: string
  name: string
  slug?: string
  document?: string | null
  status?: string
  createdAt?: string
}

export type UserListItem = {
  id: string
  email: string
  displayName?: string | null
  platformRole?: string | null
  isActive?: boolean
  tenantIds?: string[]
}

export type Group = {
  id: string
  name: string
  tenantId?: string | null
  description?: string | null
}

export type PagedResult<T> = {
  items: T[]
  totalCount?: number
  page?: number
  pageSize?: number
}
