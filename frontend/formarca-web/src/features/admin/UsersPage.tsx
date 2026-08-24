import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { UserListItem } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'

export function UsersPage() {
  const { t } = useI18n()
  const query = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: () => adminApi.listUsers(),
  })

  const columns = useMemo<TableViewColumn<UserListItem>[]>(
    () => [
      { key: 'email', dataIndex: 'email', title: t('admin.colEmail') },
      { key: 'displayName', dataIndex: 'displayName', title: t('admin.colDisplayName') },
      { key: 'platformRole', dataIndex: 'platformRole', title: t('admin.colPlatformRole') },
      {
        key: 'isActive',
        dataIndex: 'isActive',
        title: t('admin.colActive'),
        render: (value: boolean | undefined) =>
          value === false ? t('common.no') : t('common.yes'),
      },
    ],
    [t],
  )

  const tableViews = useTableViews({ tableKey: 'admin.users', columns })

  if (query.isLoading) {
    return <Spin tip={t('admin.usersLoading')} />
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : t('admin.usersLoadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('admin.usersUnavailable')}
        description={message}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {t('admin.usersTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('admin.usersDescription')}
      </Typography.Paragraph>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <TableViewToolbar controller={tableViews} />
      </div>
      <Table<UserListItem>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.usersEmpty') }}
        columns={tableViews.visibleColumns}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
