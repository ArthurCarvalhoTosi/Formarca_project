import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/shared/auth/AuthContext'

export function RequireFullAccess() {
  const { isOnboardingLimited } = useAuth()
  if (isOnboardingLimited) {
    return <Navigate to="/" replace />
  }
  return <Outlet />
}
