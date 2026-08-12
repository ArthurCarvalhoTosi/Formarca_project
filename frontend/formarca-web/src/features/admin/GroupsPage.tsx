import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { Group } from './types'
import { ApiError } from '@/shared/api/types'

export function GroupsPage() {
  const query = useQuery({
    queryKey: ['admin', 'groups'],
    queryFn: () => adminApi.listGroups(),
  })

  if (query.isLoading) {
    return <Spin tip="Carregando grupos..." />
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : 'Não foi possível carregar grupos.'
    return (
      <Alert
        type="warning"
        showIcon
        message="Grupos indisponíveis"
        description={message}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        Grupos
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        Grupos e permissões por tenant / plataforma (stub de listagem).
      </Typography.Paragraph>
      <Table<Group>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: 'Nenhum grupo encontrado.' }}
        columns={[
          { title: 'Nome', dataIndex: 'name' },
          {
            title: 'Escopo',
            dataIndex: 'tenantId',
            render: (tenantId: string | null | undefined) =>
              tenantId ? `Tenant ${tenantId}` : 'Plataforma',
          },
          { title: 'Descrição', dataIndex: 'description' },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
