import { Alert, Spin, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { Tenant } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import type { Dictionary } from '@/shared/i18n'
import { TenantApprovalControls } from './TenantApprovalControls'

function statusColor(status?: string) {
  switch (status) {
    case 'Pending':
      return 'warning'
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
      <Table<Tenant>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.tenantsEmpty') }}
        columns={[
          { title: t('admin.colName'), dataIndex: 'name' },
          { title: t('admin.colSlug'), dataIndex: 'slug' },
          { title: t('admin.colDocument'), dataIndex: 'document' },
          {
            title: t('admin.colStatus'),
            dataIndex: 'status',
            render: (status: string | undefined) => (
              <Tag color={statusColor(status)}>{statusLabel(status, dict)}</Tag>
            ),
          },
          {
            title: t('common.actions'),
            key: 'actions',
            render: (_: unknown, tenant: Tenant) => (
              <TenantApprovalControls tenant={tenant} />
            ),
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
