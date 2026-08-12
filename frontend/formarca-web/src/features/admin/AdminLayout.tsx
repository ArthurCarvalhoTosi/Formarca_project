import { Menu } from 'antd'
import type { MenuProps } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'

const items: MenuProps['items'] = [
  { key: 'tenants', label: 'Tenants' },
  { key: 'users', label: 'Usuários' },
  { key: 'groups', label: 'Grupos' },
]

export function AdminLayout() {
  const location = useLocation()
  const navigate = useNavigate()

  const selected = location.pathname.split('/')[2] ?? 'tenants'

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
