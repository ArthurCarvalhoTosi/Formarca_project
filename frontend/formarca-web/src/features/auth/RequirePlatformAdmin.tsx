import { Navigate, Outlet } from 'react-router-dom'
import { Result } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'

export function RequirePlatformAdmin() {
  const { isPlatformAdmin, status } = useAuth()
  const { t } = useI18n()

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  if (!isPlatformAdmin) {
    return (
      <Result
        status="403"
        title={t('admin.restrictedTitle')}
        subTitle={t('admin.restrictedSubtitle')}
      />
    )
  }

  return <Outlet />
}
