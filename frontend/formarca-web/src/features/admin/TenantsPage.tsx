import { Alert, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { Tenant } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import type { Dictionary } from '@/shared/i18n'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { TenantApprovalControls } from './TenantApprovalControls'

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

export function TenantsPage() {
  const { t, dict } = useI18n()

  const query = useQuery({
    queryKey: ['admin', 'tenants'],
    queryFn: () => adminApi.listTenants(),
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

  if (query.isLoading) {
    return <Spin tip={t('admin.tenantsLoading')} />
  }

  if (query.isError) {
    const errorMessage =
      query.error instanceof ApiError
        ? query.error.message
        : t('admin.tenantsLoadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('admin.tenantsUnavailable')}
        description={`${errorMessage} ${t('admin.tenantsLoadHint')}`}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {t('admin.tenantsTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('admin.tenantsDescription')}
      </Typography.Paragraph>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <TableViewToolbar controller={tableViews} />
      </div>
      <Table<Tenant>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.tenantsEmpty') }}
        columns={tableViews.visibleColumns}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
