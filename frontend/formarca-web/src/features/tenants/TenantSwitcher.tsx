import { Select, Typography } from 'antd'
import { Building2 } from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthContext'

export function TenantSwitcher() {
  const { tenants, activeTenantId, switchTenant, status } = useAuth()

  if (status !== 'authenticated' || tenants.length <= 1) {
    return null
  }

  return (
    <div className="tenant-switcher">
      <Building2 size={16} aria-hidden />
      <Typography.Text type="secondary" style={{ whiteSpace: 'nowrap' }}>
        Empresa
      </Typography.Text>
      <Select
        style={{ minWidth: 200 }}
        value={activeTenantId ?? undefined}
        options={tenants.map((t) => ({
          value: t.id,
          label: t.name,
        }))}
        onChange={(value) => {
          void switchTenant(value)
        }}
        placeholder="Selecione o tenant"
      />
    </div>
  )
}
