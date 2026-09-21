import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { Alert, Button, Form, Input, Select, Table, Tag } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { TenantApprovalControls } from '@/features/admin/TenantApprovalControls'
import { env } from '@/shared/config/env'
import { useAuth } from '@/shared/auth/AuthContext'
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
import { customersApi } from './api'
import type { CustomerListItem } from './types'

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

function matchesSearch(row: CustomerListItem, query: string, statusCsv: string): boolean {
  const statuses = splitCsv(statusCsv)
  if (statuses.length > 0 && !statuses.includes(row.status ?? '')) return false
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  const digits = needle.replace(/\D/g, '')
  return [row.name, row.document, row.email, row.slug].some((value) => {
    if (!value) return false
    if (value.toLowerCase().includes(needle)) return true
    return Boolean(digits) && value.replace(/\D/g, '').includes(digits)
  })
}

export function CustomersPage() {
  const { t, dict } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin, hasPermission } = useAuth()
  const search = useListSearch(DEFAULT_FILTERS)
  const canCreate = hasPermission('registrations.manage')

  const query = useQuery({
    queryKey: ['customers', isPlatformAdmin, search.page, search.pageSize, search.applied],
    queryFn: () =>
      customersApi.list(isPlatformAdmin, {
        page: search.page,
        pageSize: search.pageSize,
      }),
    enabled: search.hasSearched,
  })

  const columns = useMemo<TableViewColumn<CustomerListItem>[]>(
    () => [
      { key: 'name', dataIndex: 'name', title: t('customers.colName') },
      {
        key: 'document',
        dataIndex: 'document',
        title: t('customers.colDocument'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'email',
        dataIndex: 'email',
        title: t('customers.colEmail'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('customers.colStatus'),
        render: (status: string | undefined) => (
          <Tag color={statusColor(status)}>{statusLabel(status, dict)}</Tag>
        ),
      },
      {
        key: 'actions',
        title: t('common.actions'),
        hideable: false,
        adminOnly: true,
        render: (_: unknown, row: CustomerListItem) => (
          <TenantApprovalControls tenant={row} stopRowClick />
        ),
      },
    ],
    [dict, t],
  )

  const tableViews = useTableViews({ tableKey: 'customers', columns })
  const items = useMemo(() => {
    const rows = query.data?.items ?? []
    return rows.filter((row) => matchesSearch(row, search.applied.q, search.applied.status))
  }, [query.data?.items, search.applied.q, search.applied.status])

  const chips = [
    search.applied.q
      ? { key: 'q', label: `${t('common.search')}: ${search.applied.q}` }
      : null,
    ...csvChips(
      search.applied.status,
      'status',
      t('customers.filterStatus'),
      (value) => statusLabel(value, dict),
      (value) => search.removeCsvValue('status', value),
    ),
  ].filter(Boolean) as { key: string; label: string }[]

  return (
    <ListSearchLayout
      title={t('customers.title')}
      description={t('customers.description')}
      search={search}
      chips={chips}
      extra={
        canCreate ? (
          <Button type="primary" onClick={() => navigate('/clientes/novo')}>
            {t('customers.new')}
          </Button>
        ) : null
      }
      views={<TableViewToolbar controller={tableViews} />}
      banner={
        env.mockAuth ? (
          <Alert
            type="info"
            showIcon
            message={t('customers.mockHint')}
            style={{ marginBottom: 16 }}
          />
        ) : null
      }
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('customers.unavailable')}
      errorFallback={t('customers.loadError')}
      filters={
        <>
          <Form.Item label={t('common.search')}>
            <Input
              allowClear
              placeholder={t('customers.searchPlaceholder')}
              value={search.draft.q}
              onChange={(event) => search.setDraftValue('q', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('customers.filterStatus')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('customers.filterStatus')}
              value={splitCsv(search.draft.status)}
              onChange={(value) => search.setDraftValue('status', joinCsv(value))}
              options={['Pending', 'Onboarding', 'Active', 'Inactive', 'Suspended'].map(
                (value) => ({ value, label: statusLabel(value, dict) }),
              )}
            />
          </Form.Item>
        </>
      }
    >
      <div className="list-surface">
        <Table<CustomerListItem>
          rowKey="id"
          dataSource={items}
          locale={{ emptyText: <ListEmptyState searched /> }}
          onRow={(record) => ({
            onClick: () => navigate(`/clientes/${record.id}`),
            style: { cursor: 'pointer' },
          })}
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
