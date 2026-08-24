import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { processesApi } from './api'
import type { ProcessRecord } from './types'

export function ProcessesPage() {
  const { t } = useI18n()
  const navigate = useNavigate()

  const query = useQuery({
    queryKey: ['processes'],
    queryFn: () => processesApi.list(),
  })

  if (query.isLoading) {
    return <Spin tip={t('processes.loading')} />
  }

  if (query.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('processes.loadError')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('processes.loadError')
        }
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('processes.title')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('processes.description')}
      </Typography.Paragraph>
      <Table<ProcessRecord>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('processes.empty') }}
        onRow={(record) => ({
          onClick: () => navigate(`/contratos/${record.contractId}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          {
            title: t('processes.colBrand'),
            dataIndex: 'brandName',
            render: (value: string | null | undefined) => value || '—',
          },
          {
            title: t('processes.colContract'),
            dataIndex: 'contractCode',
            render: (value: string | null | undefined) => value || '—',
          },
          {
            title: t('processes.colNumber'),
            dataIndex: 'processNumber',
            render: (value: string | null | undefined) => value || '—',
          },
          {
            title: t('processes.colStatus'),
            dataIndex: 'status',
            render: (value: string) =>
              value === 'Generated' ? t('processes.statusGenerated') : value,
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
