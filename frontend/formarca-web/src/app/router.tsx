import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom'
import { AppShell } from '@/app/layout/AppShell'
import { AdminLayout } from '@/features/admin/AdminLayout'
import { GroupsPage } from '@/features/admin/GroupsPage'
import { TenantsPage } from '@/features/admin/TenantsPage'
import { UsersPage } from '@/features/admin/UsersPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { RequireFullAccess } from '@/features/auth/RequireFullAccess'
import { RequirePermission } from '@/features/auth/RequirePermission'
import { RequirePlatformAdmin } from '@/features/auth/RequirePlatformAdmin'
import { BrandFormPage } from '@/features/brands/BrandFormPage'
import { BrandsPage } from '@/features/brands/BrandsPage'
import { ContractDetailPage } from '@/features/contracts/ContractDetailPage'
import { ContractsPage } from '@/features/contracts/ContractsPage'
import { CustomerFormPage } from '@/features/customers/CustomerFormPage'
import { CustomersPage } from '@/features/customers/CustomersPage'
import { InvoiceSettingsPage } from '@/features/invoices/InvoiceSettingsPage'
import { InvoicesPage } from '@/features/invoices/InvoicesPage'
import { ProcessesPage } from '@/features/processes/ProcessesPage'
import { PublicRegisterPage } from '@/features/registration/PublicRegisterPage'
import { HomePage } from '@/pages/HomePage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'
import { useI18n } from '@/shared/i18n'

function LegacyIdRedirect({ prefix }: { prefix: string }) {
  const { id } = useParams()
  return <Navigate to={`${prefix}/${id}`} replace />
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

          <Route element={<RequireFullAccess />}>
            <Route path="processos" element={<ProcessesPage />} />
            <Route path="processes" element={<Navigate to="/processos" replace />} />
            <Route path="marcas" element={<BrandsPage />} />
            <Route element={<RequirePermission permission="brands.manage" />}>
              <Route path="marcas/novo" element={<BrandFormPage />} />
            </Route>
            <Route path="marcas/:id" element={<BrandFormPage />} />
            <Route path="brands" element={<Navigate to="/marcas" replace />} />
            <Route path="brands/new" element={<Navigate to="/marcas/novo" replace />} />
            <Route
              path="brands/:id"
              element={<LegacyIdRedirect prefix="/marcas" />}
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
            <Route path="information" element={<Navigate to="/informacoes" replace />} />
          </Route>

          <Route path="contratos" element={<ContractsPage />} />
          <Route path="contratos/:id" element={<ContractDetailPage />} />
          <Route path="contracts" element={<Navigate to="/contratos" replace />} />
          <Route
            path="contracts/:id"
            element={<LegacyIdRedirect prefix="/contratos" />}
          />

          <Route path="faturas" element={<InvoicesPage />} />
          <Route path="invoices" element={<Navigate to="/faturas" replace />} />

          <Route path="clientes" element={<CustomersPage />} />
          <Route element={<RequirePermission permission="registrations.manage" />}>
            <Route path="clientes/novo" element={<CustomerFormPage />} />
          </Route>
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
            element={<LegacyIdRedirect prefix="/clientes" />}
          />
          <Route
            path="registration"
            element={<Navigate to="/clientes" replace />}
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
              <Route path="invoice-settings" element={<InvoiceSettingsPage />} />
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
