import { Typography } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'

export function HomePage() {
  const { user, tenants, activeTenantId } = useAuth()
  const { t } = useI18n()
  const activeTenant = tenants.find((tenant) => tenant.id === activeTenantId)

  const welcome = user?.displayName
    ? t('home.welcomeNamed', { name: user.displayName })
    : t('home.welcome')

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('home.title')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {welcome} {t('home.foundation')}
      </Typography.Paragraph>
      {activeTenant ? (
        <Typography.Paragraph>
          {t('home.activeTenant')}{' '}
          <Typography.Text strong>{activeTenant.name}</Typography.Text>
        </Typography.Paragraph>
      ) : (
        <Typography.Paragraph type="secondary">
          {user?.platformRole === 'PlatformAdmin'
            ? t('home.noActiveTenantAdmin')
            : t('home.noActiveTenant')}
        </Typography.Paragraph>
      )}
    </div>
  )
}
