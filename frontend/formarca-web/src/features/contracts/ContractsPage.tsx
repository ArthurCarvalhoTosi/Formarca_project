import { DatePicker, Form, Input, Select, Table, Tag } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import dayjs from 'dayjs'
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
import { formatClasses, formatDate, formatMoney } from '@/shared/money'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { contractsApi } from './api'
import type { Contract, ContractStatus } from './types'

const STATUS_OPTIONS: ContractStatus[] = ['PreIssued', 'Issued', 'Signed', 'Cancelled']

const DEFAULT_FILTERS = {
  clientName: '',
  code: '',
  brandName: '',
  document: '',
  status: '',
  from: '',
  to: '',
  recentDays: '',
}

const OPEN_FILTERS = {
  status: 'PreIssued,Issued,Signed',
  recentDays: String(RECENT_ACTIVE_DAYS),
}

function statusColor(status?: string) {
  switch (status) {
    case 'PreIssued':
      return 'default'
    case 'Issued':
      return 'processing'
    case 'Signed':
      return 'success'
    case 'Cancelled':
      return 'error'
    default:
      return 'default'
  }
}

export function ContractsPage() {
  const { t, locale, dict } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const search = useListSearch(DEFAULT_FILTERS, {
    autoSearch: true,
    initialFilters: OPEN_FILTERS,
  })

  const filters = {
    clientName: search.applied.clientName.trim() || undefined,
    code: search.applied.code.trim() || undefined,
    brandName: search.applied.brandName.trim() || undefined,
    document: search.applied.document.trim() || undefined,
    status: search.applied.status || undefined,
    from: search.applied.from || undefined,
    to: search.applied.to || undefined,
    activeSince: activeSinceFromRecentDays(search.applied.recentDays),
    page: search.page,
    pageSize: search.pageSize,
  }

  const query = useQuery({
    queryKey: ['contracts', filters],
    queryFn: () => contractsApi.list(filters),
    enabled: search.hasSearched,
  })

  const statusLabel = useMemo(
    () => (value: string | undefined) => {
      if (!value) return '—'
      if (value in dict.contracts.status) {
        return dict.contracts.status[value as keyof typeof dict.contracts.status]
      }
      return value
    },
    [dict.contracts.status],
  )

  const columns = useMemo<TableViewColumn<Contract>[]>(
    () => [
      { key: 'code', dataIndex: 'code', title: t('contracts.colCode'), fixed: 'left' },
      {
        key: 'tenantName',
        dataIndex: 'tenantName',
        title: t('contracts.colClient'),
        adminOnly: true,
        render: (value: string | null | undefined) => value || '—',
      },
      { key: 'companyDocument', dataIndex: 'companyDocument', title: t('contracts.colCnpj') },
      {
        key: 'representativeDocument',
        dataIndex: 'representativeDocument',
        title: t('contracts.colCpf'),
      },
      { key: 'brandName', dataIndex: 'brandName', title: t('contracts.colBrand') },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('contracts.colStatus'),
        render: (value: string) => (
          <Tag color={statusColor(value)}>{statusLabel(value)}</Tag>
        ),
      },
      {
        key: 'clientSignedAt',
        dataIndex: 'clientSignedAt',
        title: t('contracts.colClientSign'),
        render: (value: string | null | undefined) =>
          value ? t('contracts.signed') : t('contracts.notSigned'),
      },
      {
        key: 'formarcaSignedAt',
        dataIndex: 'formarcaSignedAt',
        title: t('contracts.colFormarcaSign'),
        render: (value: string | null | undefined) =>
          value ? t('contracts.signed') : t('contracts.notSigned'),
      },
      {
        key: 'generatedAtUtc',
        dataIndex: 'generatedAtUtc',
        title: t('contracts.colGeneratedAt'),
        render: (value: string | null | undefined) => formatDate(value, locale),
      },
      {
        key: 'signedAtUtc',
        dataIndex: 'signedAtUtc',
        title: t('contracts.colSignedAt'),
        render: (value: string | null | undefined) => formatDate(value, locale),
      },
      {
        key: 'amount',
        dataIndex: 'amount',
        title: t('contracts.colAmount'),
        render: (value: number) => formatMoney(value ?? 0, locale),
      },
      {
        key: 'paidAmount',
        dataIndex: 'paidAmount',
        title: t('contracts.colPaid'),
        render: (value: number | null | undefined) =>
          value == null ? '—' : formatMoney(value, locale),
      },
      {
        key: 'classes',
        dataIndex: 'classes',
        title: t('contracts.colClasses'),
        render: (value: number[] | string) => formatClasses(value),
      },
      {
        key: 'paymentMethod',
        dataIndex: 'paymentMethod',
        title: t('contracts.colPaymentMethod'),
        render: (value: string | null | undefined) => {
          if (value === 'Pix') return t('invoices.pix')
          if (value === 'Card') return t('invoices.card')
          return '—'
        },
      },
    ],
    [locale, statusLabel, t],
  )

  const tableViews = useTableViews({ tableKey: 'contracts', columns })

  return (
    <ListSearchLayout
      title={t('contracts.pageTitle')}
      description={t('contracts.description')}
      search={search}
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('contracts.unavailable')}
      errorFallback={t('contracts.loadError')}
      chips={[
        search.applied.clientName
          ? { key: 'clientName', label: `${t('contracts.filters.clientName')}: ${search.applied.clientName}` }
          : null,
        search.applied.code
          ? { key: 'code', label: `${t('contracts.filters.code')}: ${search.applied.code}` }
          : null,
        search.applied.brandName
          ? { key: 'brandName', label: `${t('contracts.filters.brandName')}: ${search.applied.brandName}` }
          : null,
        search.applied.document
          ? { key: 'document', label: `${t('contracts.filters.document')}: ${search.applied.document}` }
          : null,
        ...csvChips(
          search.applied.status,
          'status',
          t('contracts.filters.status'),
          (value) => statusLabel(value),
          (value) => search.removeCsvValue('status', value),
        ),
        search.applied.recentDays
          ? {
              key: 'recentDays',
              label: t('listSearch.recentActive', { days: search.applied.recentDays }),
            }
          : null,
        search.applied.from || search.applied.to
          ? {
              key: 'from',
              label: `${t('contracts.filters.dates')}: ${formatDate(search.applied.from, locale)} – ${formatDate(search.applied.to, locale)}`,
              onClose: () => search.removeFilters(['from', 'to']),
            }
          : null,
      ].filter(Boolean) as { key: string; label: string }[]}
      filters={
        <>
          {isPlatformAdmin ? (
            <Form.Item label={t('contracts.filters.clientName')}>
              <Input
                allowClear
                placeholder={t('contracts.filters.clientName')}
                value={search.draft.clientName}
                onChange={(event) => search.setDraftValue('clientName', event.target.value)}
              />
            </Form.Item>
          ) : null}
          <Form.Item label={t('contracts.filters.code')}>
            <Input
              allowClear
              placeholder={t('contracts.filters.code')}
              value={search.draft.code}
              onChange={(event) => search.setDraftValue('code', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('contracts.filters.brandName')}>
            <Input
              allowClear
              placeholder={t('contracts.filters.brandName')}
              value={search.draft.brandName}
              onChange={(event) => search.setDraftValue('brandName', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('contracts.filters.document')}>
            <Input
              allowClear
              placeholder={t('contracts.filters.document')}
              value={search.draft.document}
              onChange={(event) => search.setDraftValue('document', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('contracts.filters.status')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('contracts.filters.status')}
              value={splitCsv(search.draft.status)}
              onChange={(value) => search.setDraftValue('status', joinCsv(value))}
              options={STATUS_OPTIONS.map((value) => ({
                value,
                label: statusLabel(value),
              }))}
            />
          </Form.Item>
          <Form.Item label={t('contracts.filters.dates')}>
            <DatePicker.RangePicker
              value={
                search.draft.from && search.draft.to
                  ? [dayjs(search.draft.from), dayjs(search.draft.to)]
                  : null
              }
              onChange={(value) => {
                search.updateDraft({
                  from: value?.[0]?.startOf('day').toISOString() ?? '',
                  to: value?.[1]?.endOf('day').toISOString() ?? '',
                })
              }}
            />
          </Form.Item>
        </>
      }
    >
      <div className="list-surface">
        <Table<Contract>
          rowKey="id"
          dataSource={query.data?.items ?? []}
          scroll={{ x: 1400 }}
          locale={{ emptyText: <ListEmptyState searched /> }}
          onRow={(record) => ({
            onClick: () => navigate(`/contratos/${record.id}`),
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
