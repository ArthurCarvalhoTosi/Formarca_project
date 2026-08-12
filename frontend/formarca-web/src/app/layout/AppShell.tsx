import { Layout, Menu, Typography, theme } from 'antd'
import type { MenuProps } from 'antd'
import {
  Briefcase,
  ClipboardList,
  FileText,
  Home,
  Info,
  Settings,
} from 'lucide-react'
import { useMemo } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { TenantSwitcher } from '@/features/tenants/TenantSwitcher'
import { useAuth } from '@/shared/auth/AuthContext'
import { HeaderUserMenu } from './HeaderUserMenu'

const { Header, Sider, Content } = Layout

function menuIcon(Icon: typeof Home) {
  return <Icon size={16} strokeWidth={1.75} />
}

export function AppShell() {
  const location = useLocation()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  const selectedKey = useMemo(() => {
    const path = location.pathname
    if (path.startsWith('/admin')) return 'admin'
    if (path.startsWith('/processos')) return 'processos'
    if (path.startsWith('/contratos')) return 'contratos'
    if (path.startsWith('/cadastro')) return 'cadastro'
    if (path.startsWith('/informacoes')) return 'informacoes'
    return 'home'
  }, [location.pathname])

  const items: MenuProps['items'] = [
    {
      key: 'home',
      icon: menuIcon(Home),
      label: 'Home',
    },
    {
      key: 'processos',
      icon: menuIcon(ClipboardList),
      label: 'Processos',
    },
    {
      key: 'contratos',
      icon: menuIcon(FileText),
      label: 'Contratos',
    },
    {
      key: 'cadastro',
      icon: menuIcon(Briefcase),
      label: 'Cadastro',
    },
    {
      key: 'informacoes',
      icon: menuIcon(Info),
      label: 'Informações',
    },
    ...(isPlatformAdmin
      ? [
          {
            type: 'divider' as const,
          },
          {
            key: 'admin',
            icon: menuIcon(Settings),
            label: 'Admin',
          },
        ]
      : []),
  ]

  return (
    <Layout className="app-shell">
      <Sider breakpoint="lg" collapsedWidth={64} width={232} theme="light">
        <div className="sider-brand">
          <Link to="/">
            <Typography.Title level={4} style={{ margin: 0 }}>
              Formarca
            </Typography.Title>
          </Link>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[selectedKey]}
          items={items}
          onClick={({ key }) => {
            if (key === 'admin') {
              navigate('/admin/tenants')
              return
            }
            navigate(key === 'home' ? '/' : `/${key}`)
          }}
        />
      </Sider>

      <Layout>
        <Header className="app-header" style={{ background: colorBgContainer }}>
          <TenantSwitcher />
          <div style={{ flex: 1 }} />
          <HeaderUserMenu />
        </Header>
        <Content className="app-content">
          <div
            className="app-content-inner"
            style={{
              background: colorBgContainer,
              borderRadius: borderRadiusLG,
            }}
          >
            <Outlet />
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}
