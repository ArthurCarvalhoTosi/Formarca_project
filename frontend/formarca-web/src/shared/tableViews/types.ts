import type { TableColumnsType } from 'antd'

export const TABLE_KEYS = [
  'customers',
  'brands',
  'contracts',
  'invoices',
  'processes',
  'admin.tenants',
  'admin.users',
  'admin.groups',
] as const

export type TableKey = (typeof TABLE_KEYS)[number]

export const DEFAULT_VIEW_VALUE = '__default__'

export type TableViewDto = {
  id: string
  tableKey: string
  name: string
  columnKeys: string[]
  isActive: boolean
}

export type CreateTableViewRequest = {
  tableKey: string
  name: string
  columnKeys: string[]
  activate?: boolean
}

export type UpdateTableViewRequest = {
  name?: string
  columnKeys?: string[]
}

export type TableViewColumn<RecordType> = NonNullable<
  TableColumnsType<RecordType>
>[number] & {
  key: string
  hideable?: boolean
  adminOnly?: boolean
}

export function isTableKey(value: string): value is TableKey {
  return (TABLE_KEYS as readonly string[]).includes(value)
}
