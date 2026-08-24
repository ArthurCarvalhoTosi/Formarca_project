import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { Group } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'

export function GroupsPage() {
  const { t } = useI18n()
  const query = useQuery({
    queryKey: ['admin', 'groups'],
    queryFn: () => adminApi.listGroups(),
  })

  const columns = useMemo<TableViewColumn<Group>[]>(
    () => [
      { key: 'name', dataIndex: 'name', title: t('admin.colName') },
      {
        key: 'tenantId',
        dataIndex: 'tenantId',
        title: t('admin.colScope'),
        render: (tenantId: string | null | undefined) =>
          tenantId
            ? t('admin.scopeTenant', { id: tenantId })
            : t('admin.scopePlatform'),
      },
      { key: 'description', dataIndex: 'description', title: t('admin.colDescription') },
    ],
    [t],
  )

  const tableViews = useTableViews({ tableKey: 'admin.groups', columns })

  if (query.isLoading) {
    return <Spin tip={t('admin.groupsLoading')} />
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : t('admin.groupsLoadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('admin.groupsUnavailable')}
        description={message}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {t('admin.groupsTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('admin.groupsDescription')}
      </Typography.Paragraph>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <TableViewToolbar controller={tableViews} />
      </div>
      <Table<Group>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.groupsEmpty') }}
        columns={tableViews.visibleColumns}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
