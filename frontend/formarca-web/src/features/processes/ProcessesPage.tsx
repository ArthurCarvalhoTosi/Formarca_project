import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { processesApi } from './api'
import type { ProcessRecord } from './types'

export function ProcessesPage() {
  const { t } = useI18n()
  const navigate = useNavigate()

  const query = useQuery({
    queryKey: ['processes'],
    queryFn: () => processesApi.list(),
  })

  const columns = useMemo<TableViewColumn<ProcessRecord>[]>(
    () => [
      {
        key: 'brandName',
        dataIndex: 'brandName',
        title: t('processes.colBrand'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'contractCode',
        dataIndex: 'contractCode',
        title: t('processes.colContract'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'processNumber',
        dataIndex: 'processNumber',
        title: t('processes.colNumber'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('processes.colStatus'),
        render: (value: string) =>
          value === 'Generated' ? t('processes.statusGenerated') : value,
      },
    ],
    [t],
  )

  const tableViews = useTableViews({ tableKey: 'processes', columns })

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
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <TableViewToolbar controller={tableViews} />
      </div>
      <Table<ProcessRecord>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('processes.empty') }}
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
