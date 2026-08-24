import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Alert, Button, Input, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { TenantApprovalControls } from '@/features/admin/TenantApprovalControls'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { env } from '@/shared/config/env'
import { useI18n } from '@/shared/i18n'
import type { Dictionary } from '@/shared/i18n'
import { customersApi } from './api'
import type { CustomerListItem } from './types'

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

function matchesSearch(row: CustomerListItem, query: string): boolean {
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
  const { isPlatformAdmin } = useAuth()
  const [search, setSearch] = useState('')

  const query = useQuery({
    queryKey: ['customers', isPlatformAdmin],
    queryFn: () => customersApi.list(isPlatformAdmin),
  })

  const filtered = useMemo(() => {
    const data = query.data ?? []
    return data.filter((row) => matchesSearch(row, search))
  }, [query.data, search])

  if (query.isLoading) {
    return <Spin tip={t('customers.loading')} />
  }

  if (query.isError) {
    const errorMessage =
      query.error instanceof ApiError
        ? query.error.message
        : t('customers.loadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('customers.unavailable')}
        description={errorMessage}
      />
    )
  }

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('customers.title')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('customers.description')}
      </Typography.Paragraph>

      {env.mockAuth ? (
        <Alert
          type="info"
          showIcon
          message={t('customers.mockHint')}
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <div>
        <div style={{
          display: 'flex',
          gap: 16,
          marginBottom: 16,
          flexWrap: 'wrap',
          alignItems: 'center',
        }}
      />
        <Input.Search
          allowClear
          placeholder={t('customers.searchPlaceholder')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onSearch={setSearch}
          style={{ maxWidth: 360, flex: 1 }}
        />
        <Button
          type="primary"
          onClick={() => navigate('/clientes/novo')}
        >
          {t('customers.new')}
        </Button>
      </div>

      <Table<CustomerListItem>
        rowKey="id"
        dataSource={filtered}
        locale={{ emptyText: t('customers.empty') }}
        onRow={(record) => ({
          onClick: () => navigate(`/clientes/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          { title: t('customers.colName'), dataIndex: 'name' },
          {
            title: t('customers.colDocument'),
            dataIndex: 'document',
            render: (value: string | null | undefined) => value || '—',
          },
          {
            title: t('customers.colEmail'),
            dataIndex: 'email',
            render: (value: string | null | undefined) => value || '—',
          },
          {
            title: t('customers.colStatus'),
            dataIndex: 'status',
            render: (status: string | undefined) => (
              <Tag color={statusColor(status)}>{statusLabel(status, dict)}</Tag>
            ),
          },
          ...(isPlatformAdmin
            ? [
                {
                  title: t('common.actions'),
                  key: 'actions',
                  render: (_: unknown, row: CustomerListItem) => (
                    <TenantApprovalControls tenant={row} stopRowClick />
                  ),
                },
              ]
            : []),
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
