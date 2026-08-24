import { Alert, Button, Space, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatMoney } from '@/shared/money'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { invoicesApi } from './api'
import type { Invoice } from './types'

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

  const query = useQuery({
    queryKey: ['invoices'],
    queryFn: () => invoicesApi.list(),
  })

  const columns = useMemo<TableViewColumn<Invoice>[]>(
    () => [
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
          <Tag color={statusColor(status)}>
            {status in dict.invoices.status
              ? dict.invoices.status[status as keyof typeof dict.invoices.status]
              : status}
          </Tag>
        ),
      },
    ],
    [dict.invoices.status, locale, t],
  )

  const tableViews = useTableViews({ tableKey: 'invoices', columns })

  if (query.isLoading) {
    return <Spin tip={t('invoices.loading')} />
  }

  if (query.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('invoices.unavailable')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('invoices.loadError')
        }
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Space align="start" style={{ width: '100%', justifyContent: 'space-between' }}>
        <div>
          <Typography.Title level={3} style={{ marginTop: 0 }}>
            {t('invoices.title')}
          </Typography.Title>
          <Typography.Paragraph type="secondary">
            {t('invoices.description')}
          </Typography.Paragraph>
        </div>
        {isPlatformAdmin ? (
          <Button onClick={() => navigate('/admin/invoice-settings')}>
            {t('admin.invoiceSettings')}
          </Button>
        ) : null}
      </Space>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <TableViewToolbar controller={tableViews} />
      </div>

      <Table<Invoice>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('invoices.empty') }}
        onRow={(record) => ({
          onClick: () => navigate(`/contratos/${record.contractId}`),
          style: { cursor: 'pointer' },
        })}
        columns={tableViews.visibleColumns}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
