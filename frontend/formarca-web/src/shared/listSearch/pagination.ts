import type { TablePaginationConfig } from 'antd'
import type { NormalizedPaged } from '@/shared/api/types'

export const DEFAULT_PAGE = 1
export const DEFAULT_PAGE_SIZE = 20
export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const

export function toTablePagination(
  result: Pick<NormalizedPaged<unknown>, 'total' | 'page' | 'pageSize'>,
  onChange: (page: number, pageSize: number) => void,
): TablePaginationConfig {
  return {
    current: result.page,
    pageSize: result.pageSize,
    total: result.total,
    showSizeChanger: true,
    pageSizeOptions: PAGE_SIZE_OPTIONS.map(String),
    onChange,
  }
}
