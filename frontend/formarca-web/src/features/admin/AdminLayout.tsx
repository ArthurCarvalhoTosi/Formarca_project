import { Menu } from 'antd'
import type { MenuProps } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/i18n'

export function AdminLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const { t } = useI18n()

  const selected = location.pathname.split('/')[2] ?? 'tenants'

  const items: MenuProps['items'] = [
    { key: 'tenants', label: t('admin.tenants') },
    { key: 'users', label: t('admin.users') },
    { key: 'groups', label: t('admin.groups') },
  ]

  return (
    <div>
      <Menu
        mode="horizontal"
        selectedKeys={[selected]}
        items={items}
        onClick={({ key }) => navigate(`/admin/${key}`)}
        style={{ marginBottom: 16 }}
      />
      <Outlet />
    </div>
  )
}
