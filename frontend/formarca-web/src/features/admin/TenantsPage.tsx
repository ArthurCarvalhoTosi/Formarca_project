import { Form, Input, Select, Table, Tag } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { Tenant } from './types'
import { useI18n } from '@/shared/i18n'
import type { Dictionary } from '@/shared/i18n'
import {
  csvChips,
  joinCsv,
  ListEmptyState,
  ListSearchLayout,
  splitCsv,
  toTablePagination,
  useListSearch,
} from '@/shared/listSearch'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { TenantApprovalControls } from './TenantApprovalControls'

const DEFAULT_FILTERS = {
  q: '',
  status: '',
}

function statusColor(status?: string) {
  switch (status) {
    case 'Pending':
      return 'warning'
    case 'Onboarding':
      return 'processing'
    case 'Active':
      return 'success'
    case 'Inactive':
      return 'default'
    case 'Suspended':
      return 'error'
    default:
      return 'default'
  }
}

function statusLabel(status: string | undefined, dict: Dictionary): string {
  if (!status) return '—'
  if (status in dict.status) {
    return dict.status[status as keyof Dictionary['status']]
  }
  return status
}

function matches(row: Tenant, query: string, statusCsv: string): boolean {
  const statuses = splitCsv(statusCsv)
  if (statuses.length > 0 && !statuses.includes(row.status ?? '')) return false
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  const digits = needle.replace(/\D/g, '')
  return [row.name, row.slug, row.document, row.email].some((value) => {
    if (!value) return false
    if (value.toLowerCase().includes(needle)) return true
    return Boolean(digits) && value.replace(/\D/g, '').includes(digits)
  })
}

export function TenantsPage() {
  const { t, dict } = useI18n()
  const search = useListSearch(DEFAULT_FILTERS)

  const query = useQuery({
    queryKey: ['admin', 'tenants', search.page, search.pageSize],
    queryFn: () => adminApi.listTenants({ page: search.page, pageSize: search.pageSize }),
    enabled: search.hasSearched,
  })

  const columns = useMemo<TableViewColumn<Tenant>[]>(
    () => [
      { key: 'name', dataIndex: 'name', title: t('admin.colName') },
      { key: 'slug', dataIndex: 'slug', title: t('admin.colSlug') },
      { key: 'document', dataIndex: 'document', title: t('admin.colDocument') },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('admin.colStatus'),
        render: (status: string | undefined) => (
          <Tag color={statusColor(status)}>{statusLabel(status, dict)}</Tag>
        ),
      },
      {
        key: 'actions',
        title: t('common.actions'),
        hideable: false,
        render: (_: unknown, tenant: Tenant) => (
          <TenantApprovalControls tenant={tenant} />
        ),
      },
    ],
    [dict, t],
  )

  const tableViews = useTableViews({ tableKey: 'admin.tenants', columns })
  const items = useMemo(
    () => (query.data?.items ?? []).filter((row) => matches(row, search.applied.q, search.applied.status)),
    [query.data?.items, search.applied.q, search.applied.status],
  )

  return (
    <ListSearchLayout
      title={t('admin.tenantsTitle')}
      description={t('admin.tenantsDescription')}
      search={search}
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('admin.tenantsUnavailable')}
      errorFallback={`${t('admin.tenantsLoadError')} ${t('admin.tenantsLoadHint')}`}
      chips={[
        search.applied.q ? { key: 'q', label: `${t('common.search')}: ${search.applied.q}` } : null,
        ...csvChips(
          search.applied.status,
          'status',
          t('admin.filterStatus'),
          (value) => statusLabel(value, dict),
          (value) => search.removeCsvValue('status', value),
        ),
      ].filter(Boolean) as { key: string; label: string }[]}
      filters={
        <>
          <Form.Item label={t('common.search')}>
            <Input
              allowClear
              placeholder={t('admin.searchPlaceholder')}
              value={search.draft.q}
              onChange={(event) => search.setDraftValue('q', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('admin.filterStatus')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('admin.filterStatus')}
              value={splitCsv(search.draft.status)}
              onChange={(value) => search.setDraftValue('status', joinCsv(value))}
              options={['Pending', 'Onboarding', 'Active', 'Inactive', 'Suspended'].map((value) => ({
                value,
                label: statusLabel(value, dict),
              }))}
            />
          </Form.Item>
        </>
      }
    >
      <div className="list-surface">
        <Table<Tenant>
          rowKey="id"
          dataSource={items}
          locale={{ emptyText: <ListEmptyState searched /> }}
          columns={tableViews.visibleColumns}
          pagination={toTablePagination(
            {
              total: query.data?.total ?? 0,
              page: query.data?.page ?? search.page,
              pageSize: query.data?.pageSize ?? search.pageSize,
            },
            search.setPage,
          )}
          sticky
        />
      </div>
    </ListSearchLayout>
  )
}
