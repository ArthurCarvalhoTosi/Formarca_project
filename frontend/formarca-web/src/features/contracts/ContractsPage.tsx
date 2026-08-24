import { Alert, DatePicker, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Dayjs } from 'dayjs'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatClasses, formatDate, formatMoney } from '@/shared/money'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { contractsApi } from './api'
import type { Contract, ContractStatus } from './types'

const STATUS_OPTIONS: ContractStatus[] = ['PreIssued', 'Issued', 'Signed', 'Cancelled']

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
  const [clientName, setClientName] = useState('')
  const [code, setCode] = useState('')
  const [brandName, setBrandName] = useState('')
  const [document, setDocument] = useState('')
  const [status, setStatus] = useState<string | undefined>()
  const [dates, setDates] = useState<[Dayjs | null, Dayjs | null] | null>(null)

  const filters = {
    clientName: clientName.trim() || undefined,
    code: code.trim() || undefined,
    brandName: brandName.trim() || undefined,
    document: document.trim() || undefined,
    status,
    from: dates?.[0]?.startOf('day').toISOString(),
    to: dates?.[1]?.endOf('day').toISOString(),
  }

  const query = useQuery({
    queryKey: ['contracts', filters],
    queryFn: () => contractsApi.list(filters),
  })

  const data = query.data ?? []

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

  if (query.isLoading) {
    return <Spin tip={t('contracts.loading')} />
  }

  if (query.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('contracts.unavailable')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('contracts.loadError')
        }
      />
    )
  }

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('contracts.pageTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('contracts.description')}
      </Typography.Paragraph>

      <div
        style={{
          display: 'flex',
          gap: 16,
          marginBottom: 16,
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <Space wrap>
          {isPlatformAdmin ? (
            <Input
              allowClear
              placeholder={t('contracts.filters.clientName')}
              value={clientName}
              onChange={(event) => setClientName(event.target.value)}
              style={{ width: 200 }}
            />
          ) : null}
          <Input
            allowClear
            placeholder={t('contracts.filters.code')}
            value={code}
            onChange={(event) => setCode(event.target.value)}
            style={{ width: 180 }}
          />
          <Input
            allowClear
            placeholder={t('contracts.filters.brandName')}
            value={brandName}
            onChange={(event) => setBrandName(event.target.value)}
            style={{ width: 180 }}
          />
          <Input
            allowClear
            placeholder={t('contracts.filters.document')}
            value={document}
            onChange={(event) => setDocument(event.target.value)}
            style={{ width: 180 }}
          />
          <Select
            allowClear
            placeholder={t('contracts.filters.status')}
            value={status}
            onChange={setStatus}
            style={{ width: 160 }}
            options={STATUS_OPTIONS.map((value) => ({
              value,
              label: statusLabel(value),
            }))}
          />
          <DatePicker.RangePicker
            value={dates}
            onChange={(value) => setDates(value)}
            placeholder={[t('contracts.filters.dates'), t('contracts.filters.dates')]}
          />
        </Space>
        <TableViewToolbar controller={tableViews} />
      </div>

      <Table<Contract>
        rowKey="id"
        dataSource={data}
        scroll={{ x: 1400 }}
        locale={{ emptyText: t('contracts.emptyDescription') }}
        onRow={(record) => ({
          onClick: () => navigate(`/contratos/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={tableViews.visibleColumns}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
