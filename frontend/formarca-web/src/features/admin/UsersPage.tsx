import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { UserListItem } from './types'
import { ApiError } from '@/shared/api/types'

export function UsersPage() {
  const query = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: () => adminApi.listUsers(),
  })

  if (query.isLoading) {
    return <Spin tip="Carregando usuários..." />
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : 'Não foi possível carregar usuários.'
    return (
      <Alert
        type="warning"
        showIcon
        message="Usuários indisponíveis"
        description={message}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        Usuários
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        Listagem administrativa. Convites e vínculo a tenants serão adicionados
        depois.
      </Typography.Paragraph>
      <Table<UserListItem>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: 'Nenhum usuário encontrado.' }}
        columns={[
          { title: 'E-mail', dataIndex: 'email' },
          { title: 'Nome', dataIndex: 'displayName' },
          { title: 'Papel de plataforma', dataIndex: 'platformRole' },
          {
            title: 'Ativo',
            dataIndex: 'isActive',
            render: (value: boolean | undefined) =>
              value === false ? 'Não' : 'Sim',
          },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
