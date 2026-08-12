import { Avatar, Dropdown, Space, Typography } from 'antd'
import type { MenuProps } from 'antd'
import { LogOut, User } from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthContext'

export function HeaderUserMenu() {
  const { user, logout, isPlatformAdmin } = useAuth()

  const items: MenuProps['items'] = [
    {
      key: 'profile',
      label: (
        <div>
          <Typography.Text strong>
            {user?.displayName || user?.email}
          </Typography.Text>
          <br />
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {isPlatformAdmin ? 'Platform Admin' : 'Usuário'}
          </Typography.Text>
        </div>
      ),
      disabled: true,
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogOut size={14} />,
      label: 'Sair',
      onClick: () => {
        void logout()
      },
    },
  ]

  return (
    <Dropdown menu={{ items }} placement="bottomRight" trigger={['click']}>
      <Space className="user-menu-trigger" size={8}>
        <Avatar size="small" icon={<User size={14} />} />
        <Typography.Text>{user?.email}</Typography.Text>
      </Space>
    </Dropdown>
  )
}
