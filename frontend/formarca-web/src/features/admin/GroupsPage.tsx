import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { Group } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'

export function GroupsPage() {
  const { t } = useI18n()
  const query = useQuery({
    queryKey: ['admin', 'groups'],
    queryFn: () => adminApi.listGroups(),
  })

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
      <Table<Group>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.groupsEmpty') }}
        columns={[
          { title: t('admin.colName'), dataIndex: 'name' },
          {
            title: t('admin.colScope'),
            dataIndex: 'tenantId',
            render: (tenantId: string | null | undefined) =>
              tenantId
                ? t('admin.scopeTenant', { id: tenantId })
                : t('admin.scopePlatform'),
          },
          { title: t('admin.colDescription'), dataIndex: 'description' },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
