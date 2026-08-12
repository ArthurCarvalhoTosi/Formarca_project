import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '@/app/layout/AppShell'
import { AdminLayout } from '@/features/admin/AdminLayout'
import { GroupsPage } from '@/features/admin/GroupsPage'
import { TenantsPage } from '@/features/admin/TenantsPage'
import { UsersPage } from '@/features/admin/UsersPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { RequirePlatformAdmin } from '@/features/auth/RequirePlatformAdmin'
import { HomePage } from '@/pages/HomePage'
import { PlaceholderPage } from '@/pages/PlaceholderPage'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route index element={<HomePage />} />
            <Route
              path="processos"
              element={
                <PlaceholderPage
                  title="Processos"
                  description="Processos e marcas — placeholder da fundação."
                />
              }
            />
            <Route
              path="contratos"
              element={
                <PlaceholderPage
                  title="Contratos"
                  description="Contratos — placeholder da fundação."
                />
              }
            />
            <Route
              path="cadastro"
              element={
                <PlaceholderPage
                  title="Cadastro"
                  description="Cadastro do cliente — placeholder da fundação."
                />
              }
            />
            <Route
              path="informacoes"
              element={
                <PlaceholderPage
                  title="Informações"
                  description="Informações e alertas — placeholder da fundação."
                />
              }
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
    </BrowserRouter>
  )
}
