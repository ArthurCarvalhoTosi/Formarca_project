import { Button, Form, Input, Select, Table, Tag } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { customersApi } from '@/features/customers/api'
import { asList } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
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
import { formatMoney } from '@/shared/money'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { invoicesApi } from './api'
import type { Invoice } from './types'

const DEFAULT_FILTERS = {
  status: '',
  tenantId: '',
  contractId: '',
  recentDays: '',
}

const OPEN_FILTERS = {
  status: 'PendingPayment,AwaitingApproval,Rejected,Paid',
  recentDays: String(RECENT_ACTIVE_DAYS),
}

function statusColor(status?: string) {
  switch (status) {
    case 'PendingPayment':
      return 'default'
    case 'AwaitingApproval':
      return 'warning'
    case 'Paid':
      return 'success'
    case 'Rejected':
      return 'error'
    default:
      return 'default'
  }
}

export function InvoicesPage() {
  const { t, locale, dict } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const search = useListSearch(DEFAULT_FILTERS, {
    autoSearch: true,
    initialFilters: OPEN_FILTERS,
  })
  const canOpenSettings = isPlatformAdmin

  const filters = {
    status: search.applied.status || undefined,
    tenantId: isPlatformAdmin ? search.applied.tenantId || undefined : undefined,
    contractId: search.applied.contractId || undefined,
    activeSince: activeSinceFromRecentDays(search.applied.recentDays),
    page: search.page,
    pageSize: search.pageSize,
  }

  const query = useQuery({
    queryKey: ['invoices', filters],
    queryFn: () => invoicesApi.list(filters),
    enabled: search.hasSearched,
  })

  const tenantsQuery = useQuery({
    queryKey: ['customers', 'options', isPlatformAdmin],
    queryFn: async () => asList(await customersApi.list(isPlatformAdmin, { pageSize: 100 })),
    enabled: isPlatformAdmin,
  })

  const invoiceStatusLabel = (status: string) =>
    status in dict.invoices.status
      ? dict.invoices.status[status as keyof typeof dict.invoices.status]
      : status

  const columns = useMemo<TableViewColumn<Invoice>[]>(() => {
    const statusText = (status: string) =>
      status in dict.invoices.status
        ? dict.invoices.status[status as keyof typeof dict.invoices.status]
        : status
    return [
      {
        key: 'contractCode',
        dataIndex: 'contractCode',
        title: t('invoices.colContract'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'tenantName',
        dataIndex: 'tenantName',
        title: t('invoices.colTenant'),
        adminOnly: true,
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'paymentMethod',
        dataIndex: 'paymentMethod',
        title: t('invoices.colMethod'),
        render: (value: string | null | undefined) => {
          if (value === 'Pix') return t('invoices.pix')
          if (value === 'Card') return t('invoices.card')
          return '—'
        },
      },
      {
        key: 'installments',
        dataIndex: 'installments',
        title: t('invoices.colInstallments'),
        render: (value: number | null | undefined) => value ?? '—',
      },
      {
        key: 'contractAmount',
        dataIndex: 'contractAmount',
        title: t('invoices.colAmount'),
        render: (value: number) => formatMoney(value ?? 0, locale),
      },
      {
        key: 'expectedAmount',
        dataIndex: 'expectedAmount',
        title: t('invoices.colExpected'),
        render: (value: number) => formatMoney(value ?? 0, locale),
      },
      {
        key: 'receivedAmount',
        dataIndex: 'receivedAmount',
        title: t('invoices.colReceived'),
        render: (value: number) => formatMoney(value ?? 0, locale),
      },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('invoices.colStatus'),
        render: (status: string) => (
          <Tag color={statusColor(status)}>{statusText(status)}</Tag>
        ),
      },
    ]
  }, [dict.invoices.status, locale, t])

  const tableViews = useTableViews({ tableKey: 'invoices', columns })

  return (
    <ListSearchLayout
      title={t('invoices.title')}
      description={t('invoices.description')}
      search={search}
      extra={
        canOpenSettings ? (
          <Button onClick={() => navigate('/admin/invoice-settings')}>
            {t('admin.invoiceSettings')}
          </Button>
        ) : null
      }
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('invoices.unavailable')}
      errorFallback={t('invoices.loadError')}
      chips={[
        ...csvChips(
          search.applied.status,
          'status',
          t('invoices.filterStatus'),
          invoiceStatusLabel,
          (value) => search.removeCsvValue('status', value),
        ),
        ...csvChips(
          search.applied.tenantId,
          'tenantId',
          t('invoices.filterTenant'),
          (value) => (tenantsQuery.data ?? []).find((row) => row.id === value)?.name ?? value,
          (value) => search.removeCsvValue('tenantId', value),
        ),
        search.applied.contractId
          ? { key: 'contractId', label: `${t('invoices.filterContract')}: ${search.applied.contractId}` }
          : null,
        search.applied.recentDays
          ? {
              key: 'recentDays',
              label: t('listSearch.recentActive', { days: search.applied.recentDays }),
            }
          : null,
      ].filter(Boolean) as { key: string; label: string }[]}
      filters={
        <>
          <Form.Item label={t('invoices.filterStatus')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('invoices.filterStatus')}
              value={splitCsv(search.draft.status)}
              onChange={(value) => search.setDraftValue('status', joinCsv(value))}
              options={['PendingPayment', 'AwaitingApproval', 'Paid', 'Rejected'].map((value) => ({
                value,
                label: invoiceStatusLabel(value),
              }))}
            />
          </Form.Item>
          <Form.Item label={t('invoices.filterContract')}>
            <Input
              allowClear
              placeholder={t('invoices.filterContract')}
              value={search.draft.contractId}
              onChange={(event) => search.setDraftValue('contractId', event.target.value)}
            />
          </Form.Item>
          {isPlatformAdmin ? (
            <Form.Item label={t('invoices.filterTenant')}>
              <Select
                mode="multiple"
                allowClear
                showSearch
                optionFilterProp="label"
                maxTagCount="responsive"
                placeholder={t('invoices.filterTenant')}
                value={splitCsv(search.draft.tenantId)}
                onChange={(value) => search.setDraftValue('tenantId', joinCsv(value))}
                options={(tenantsQuery.data ?? []).map((row) => ({
                  value: row.id,
                  label: row.name,
                }))}
              />
            </Form.Item>
          ) : null}
        </>
      }
    >
      <div className="list-surface">
        <Table<Invoice>
          rowKey="id"
          dataSource={query.data?.items ?? []}
          locale={{ emptyText: <ListEmptyState searched /> }}
          onRow={(record) => ({
            onClick: () => navigate(`/contratos/${record.contractId}`),
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
