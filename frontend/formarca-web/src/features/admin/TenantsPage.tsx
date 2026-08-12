import { Alert, Spin, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { adminApi } from './api'
import type { Tenant } from './types'
import { ApiError } from '@/shared/api/types'

export function TenantsPage() {
  const query = useQuery({
    queryKey: ['admin', 'tenants'],
    queryFn: () => adminApi.listTenants(),
  })

  if (query.isLoading) {
    return <Spin tip="Carregando tenants..." />
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError
        ? query.error.message
        : 'Não foi possível carregar tenants.'
    return (
      <Alert
        type="warning"
        showIcon
        message="Tenants indisponíveis"
        description={`${message} A API pode ainda estar offline — esta tela aceita lista vazia quando o backend responder.`}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        Tenants
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        CRUD mínimo da fundação (listagem). Criação/edição completa virá com a
        API admin.
      </Typography.Paragraph>
      <Table<Tenant>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: 'Nenhum tenant encontrado.' }}
        columns={[
          { title: 'Nome', dataIndex: 'name' },
          { title: 'Slug', dataIndex: 'slug' },
          { title: 'Documento', dataIndex: 'document' },
          { title: 'Status', dataIndex: 'status' },
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
