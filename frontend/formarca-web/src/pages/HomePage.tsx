import { Typography } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'

export function HomePage() {
  const { user, tenants, activeTenantId } = useAuth()
  const activeTenant = tenants.find((t) => t.id === activeTenantId)

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        Home
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        Bem-vindo{user?.displayName ? `, ${user.displayName}` : ''}. Esta é a
        fundação do Painel do Cliente — os módulos de negócio entram em
        entregas futuras.
      </Typography.Paragraph>
      {activeTenant ? (
        <Typography.Paragraph>
          Tenant ativo: <Typography.Text strong>{activeTenant.name}</Typography.Text>
        </Typography.Paragraph>
      ) : (
        <Typography.Paragraph type="secondary">
          Nenhum tenant ativo na sessão
          {user?.platformRole === 'PlatformAdmin'
            ? ' (esperado para Platform Admin sem membership).'
            : '.'}
        </Typography.Paragraph>
      )}
    </div>
  )
}
