import { Navigate, Outlet } from 'react-router-dom'
import { Result } from 'antd'
import { useAuth } from '@/shared/auth/AuthContext'

export function RequirePlatformAdmin() {
  const { isPlatformAdmin, status } = useAuth()

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  if (!isPlatformAdmin) {
    return (
      <Result
        status="403"
        title="Acesso restrito"
        subTitle="Esta área é exclusiva para administradores da plataforma Formarca."
      />
    )
  }

  return <Outlet />
}
