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
  activeSinceFromRecentDays,
  csvChips,
  joinCsv,
  ListEmptyState,
  ListSearchLayout,
  RECENT_ACTIVE_DAYS,
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
  recentDays: '',
}

const OPEN_FILTERS = {
  status: 'Pending,Onboarding,Suspended,Active',
  recentDays: String(RECENT_ACTIVE_DAYS),
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

export function CustomersPage() {
  const { t, dict } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin, hasPermission } = useAuth()
  const search = useListSearch(DEFAULT_FILTERS, {
    autoSearch: true,
    initialFilters: isPlatformAdmin ? OPEN_FILTERS : undefined,
  })
  const canCreate = hasPermission('registrations.manage')

  const activeSince = activeSinceFromRecentDays(search.applied.recentDays)
  const query = useQuery({
    queryKey: ['customers', isPlatformAdmin, search.page, search.pageSize, search.applied, activeSince],
    queryFn: () =>
      customersApi.list(isPlatformAdmin, {
        page: search.page,
        pageSize: search.pageSize,
        q: search.applied.q || undefined,
        status: isPlatformAdmin ? search.applied.status || undefined : undefined,
        activeSince: isPlatformAdmin ? activeSince : undefined,
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
  const items = query.data?.items ?? []

  const chips = [
    search.applied.q
      ? { key: 'q', label: `${t('common.search')}: ${search.applied.q}` }
      : null,
    search.applied.recentDays
      ? {
          key: 'recentDays',
          label: t('listSearch.recentActive', { days: search.applied.recentDays }),
        }
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
