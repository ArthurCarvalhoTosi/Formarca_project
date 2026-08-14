import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'

export function RequireAuth() {
  const { status } = useAuth()
  const { t } = useI18n()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'grid',
          placeItems: 'center',
        }}
      >
        <Spin size="large" tip={t('session.loading')} />
      </div>
    )
  }

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}
