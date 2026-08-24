import { Alert, DatePicker, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Dayjs } from 'dayjs'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatClasses, formatDate, formatMoney } from '@/shared/money'
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

      <Space wrap style={{ marginBottom: 16 }}>
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

      <Table<Contract>
        rowKey="id"
        dataSource={data}
        scroll={{ x: 1400 }}
        locale={{ emptyText: t('contracts.emptyDescription') }}
        onRow={(record) => ({
          onClick: () => navigate(`/contratos/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          { title: t('contracts.colCode'), dataIndex: 'code', fixed: 'left' },
          ...(isPlatformAdmin
            ? [
                {
                  title: t('contracts.colClient'),
                  dataIndex: 'tenantName',
                  render: (value: string | null | undefined) => value || '—',
                },
              ]
            : []),
          { title: t('contracts.colCnpj'), dataIndex: 'companyDocument' },
          { title: t('contracts.colCpf'), dataIndex: 'representativeDocument' },
          { title: t('contracts.colBrand'), dataIndex: 'brandName' },
          {
            title: t('contracts.colStatus'),
            dataIndex: 'status',
            render: (value: string) => (
              <Tag color={statusColor(value)}>{statusLabel(value)}</Tag>
            ),
          },
          {
            title: t('contracts.colClientSign'),
            dataIndex: 'clientSignedAt',
            render: (value: string | null | undefined) =>
              value ? t('contracts.signed') : t('contracts.notSigned'),
          },
          {
            title: t('contracts.colFormarcaSign'),
            dataIndex: 'formarcaSignedAt',
            render: (value: string | null | undefined) =>
              value ? t('contracts.signed') : t('contracts.notSigned'),
          },
          {
            title: t('contracts.colGeneratedAt'),
            dataIndex: 'generatedAtUtc',
            render: (value: string | null | undefined) => formatDate(value, locale),
          },
          {
            title: t('contracts.colSignedAt'),
            dataIndex: 'signedAtUtc',
            render: (value: string | null | undefined) => formatDate(value, locale),
          },
          {
            title: t('contracts.colAmount'),
            dataIndex: 'amount',
            render: (value: number) => formatMoney(value ?? 0, locale),
          },
          {
            title: t('contracts.colPaid'),
            dataIndex: 'paidAmount',
            render: (value: number | null | undefined) =>
              value == null ? '—' : formatMoney(value, locale),
          },
          {
            title: t('contracts.colClasses'),
            dataIndex: 'classes',
            render: (value: number[] | string) => formatClasses(value),
          },
          {
            title: t('contracts.colPaymentMethod'),
            dataIndex: 'paymentMethod',
            render: (value: string | null | undefined) => {
              if (value === 'Pix') return t('invoices.pix')
              if (value === 'Card') return t('invoices.card')
              return '—'
            },
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
