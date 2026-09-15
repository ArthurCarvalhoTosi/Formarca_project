import { Select, Typography } from 'antd'
import { Building2 } from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'

export function TenantSwitcher() {
  const { tenants, activeTenantId, switchTenant, status } = useAuth()
  const { t } = useI18n()

  if (status !== 'authenticated' || tenants.length <= 1) {
    return null
  }

  return (
    <div className="tenant-switcher">
      <Building2 size={16} aria-hidden />
      <Typography.Text type="secondary" style={{ whiteSpace: 'nowrap' }}>
        {t('header.company')}
      </Typography.Text>
      <Select
        style={{ minWidth: 200 }}
        value={activeTenantId ?? undefined}
        options={tenants.map((tenant) => ({
          value: tenant.id,
          label: tenant.name,
        }))}
        onChange={(value) => {
          void switchTenant(value)
        }}
        placeholder={t('header.selectTenant')}
      />
    </div>
  )
}
