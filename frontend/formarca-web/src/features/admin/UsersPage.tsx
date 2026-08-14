import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { UserListItem } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'

export function UsersPage() {
  const { t } = useI18n()
  const query = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: () => adminApi.listUsers(),
  })

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
      <Table<UserListItem>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.usersEmpty') }}
        columns={[
          { title: t('admin.colEmail'), dataIndex: 'email' },
          { title: t('admin.colDisplayName'), dataIndex: 'displayName' },
          { title: t('admin.colPlatformRole'), dataIndex: 'platformRole' },
          {
            title: t('admin.colActive'),
            dataIndex: 'isActive',
            render: (value: boolean | undefined) =>
              value === false ? t('common.no') : t('common.yes'),
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
