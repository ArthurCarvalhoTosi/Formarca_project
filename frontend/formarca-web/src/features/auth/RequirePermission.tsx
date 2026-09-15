import { Navigate, Outlet } from 'react-router-dom'
import { Result } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'

type RequirePermissionProps = {
  permission: string
}

export function RequirePermission({ permission }: RequirePermissionProps) {
  const { hasPermission, status } = useAuth()
  const { t } = useI18n()

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  if (!hasPermission(permission)) {
    return (
      <Result
        status="403"
        title={t('auth.forbiddenTitle')}
        subTitle={t('auth.forbiddenSubtitle')}
      />
    )
  }

  return <Outlet />
}
