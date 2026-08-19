import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom'
import { AppShell } from '@/app/layout/AppShell'
import { AdminLayout } from '@/features/admin/AdminLayout'
import { GroupsPage } from '@/features/admin/GroupsPage'
import { TenantsPage } from '@/features/admin/TenantsPage'
import { UsersPage } from '@/features/admin/UsersPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { RequirePlatformAdmin } from '@/features/auth/RequirePlatformAdmin'
import { ContractsPage } from '@/features/contracts/ContractsPage'
import { CustomerFormPage } from '@/features/customers/CustomerFormPage'
import { CustomersPage } from '@/features/customers/CustomersPage'
import { PublicRegisterPage } from '@/features/registration/PublicRegisterPage'
import { HomePage } from '@/pages/HomePage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'
import { useI18n } from '@/shared/i18n'

function LegacyCustomerRedirect() {
  const { id } = useParams()
  return <Navigate to={`/clientes/${id}`} replace />
}

function AppRoutes() {
  const { t } = useI18n()

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<PublicRegisterPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<HomePage />} />
          <Route
            path="processos"
            element={
              <PlaceholderPage
                title={t('nav.processes')}
                description={t('placeholder.processesDescription')}
              />
            }
          />
          <Route path="contratos" element={<ContractsPage />} />
          <Route path="clientes" element={<CustomersPage />} />
          <Route path="clientes/novo" element={<CustomerFormPage />} />
          <Route path="clientes/:id" element={<CustomerFormPage />} />
          <Route path="cadastro" element={<Navigate to="/clientes" replace />} />
          <Route
            path="cadastro/clientes"
            element={<Navigate to="/clientes" replace />}
          />
          <Route
            path="cadastro/clientes/novo"
            element={<Navigate to="/clientes/novo" replace />}
          />
          <Route
            path="cadastro/clientes/:id"
            element={<LegacyCustomerRedirect />}
          />
          <Route
            path="registration"
            element={<Navigate to="/clientes" replace />}
          />
          <Route
            path="informacoes"
            element={
              <PlaceholderPage
                title={t('nav.information')}
                description={t('placeholder.informationDescription')}
              />
            }
          />

          <Route
            path="tenants"
            element={<Navigate to="/admin/tenants" replace />}
          />

          <Route path="admin" element={<RequirePlatformAdmin />}>
            <Route element={<AdminLayout />}>
              <Route index element={<Navigate to="tenants" replace />} />
              <Route path="tenants" element={<TenantsPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="groups" element={<GroupsPage />} />
            </Route>
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}
