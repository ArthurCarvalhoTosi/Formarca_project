import { Alert, Button, Space, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatMoney } from '@/shared/money'
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

      <Table<Invoice>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('invoices.empty') }}
        onRow={(record) => ({
          onClick: () => navigate(`/contratos/${record.contractId}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          {
            title: t('invoices.colContract'),
            dataIndex: 'contractCode',
            render: (value: string | null | undefined) => value || '—',
          },
          ...(isPlatformAdmin
            ? [
                {
                  title: t('invoices.colTenant'),
                  dataIndex: 'tenantName',
                  render: (value: string | null | undefined) => value || '—',
                },
              ]
            : []),
          {
            title: t('invoices.colMethod'),
            dataIndex: 'paymentMethod',
            render: (value: string | null | undefined) => {
              if (value === 'Pix') return t('invoices.pix')
              if (value === 'Card') return t('invoices.card')
              return '—'
            },
          },
          {
            title: t('invoices.colInstallments'),
            dataIndex: 'installments',
            render: (value: number | null | undefined) => value ?? '—',
          },
          {
            title: t('invoices.colAmount'),
            dataIndex: 'contractAmount',
            render: (value: number) => formatMoney(value ?? 0, locale),
          },
          {
            title: t('invoices.colExpected'),
            dataIndex: 'expectedAmount',
            render: (value: number) => formatMoney(value ?? 0, locale),
          },
          {
            title: t('invoices.colReceived'),
            dataIndex: 'receivedAmount',
            render: (value: number) => formatMoney(value ?? 0, locale),
          },
          {
            title: t('invoices.colStatus'),
            dataIndex: 'status',
            render: (status: string) => (
              <Tag color={statusColor(status)}>
                {status in dict.invoices.status
                  ? dict.invoices.status[status as keyof typeof dict.invoices.status]
                  : status}
              </Tag>
            ),
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
