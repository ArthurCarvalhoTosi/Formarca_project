import { Layout, Menu, theme } from 'antd'
import type { MenuProps } from 'antd'
import {
  Briefcase,
  ClipboardList,
  FileText,
  Home,
  Info,
  Settings,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { TenantSwitcher } from '@/features/tenants/TenantSwitcher'
import { BrandLogo } from '@/shared/brand/BrandLogo'
import { useAuth } from '@/shared/auth/AuthContext'
import { LocaleSwitcher, useI18n } from '@/shared/i18n'
import { HeaderUserMenu } from './HeaderUserMenu'

const { Header, Sider, Content } = Layout

function menuIcon(Icon: typeof Home) {
  return <Icon size={16} strokeWidth={1.75} />
}

export function AppShell() {
  const location = useLocation()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const { t } = useI18n()
  const [collapsed, setCollapsed] = useState(false)
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  const selectedKey = useMemo(() => {
    const path = location.pathname
    if (path.startsWith('/admin')) return 'admin'
    if (path.startsWith('/processos')) return 'processos'
    if (path.startsWith('/contratos')) return 'contratos'
    if (
      path.startsWith('/clientes') ||
      path.startsWith('/cadastro') ||
      path.startsWith('/registration')
    ) {
      return 'clientes'
    }
    if (path.startsWith('/informacoes')) return 'informacoes'
    return 'home'
  }, [location.pathname])

  const items: MenuProps['items'] = [
    {
      key: 'home',
      icon: menuIcon(Home),
      label: t('nav.home'),
    },
    {
      key: 'processos',
      icon: menuIcon(ClipboardList),
      label: t('nav.processes'),
    },
    {
      key: 'contratos',
      icon: menuIcon(FileText),
      label: t('nav.contracts'),
    },
    {
      key: 'clientes',
      icon: menuIcon(Briefcase),
      label: t('nav.customers'),
    },
    {
      key: 'informacoes',
      icon: menuIcon(Info),
      label: t('nav.information'),
    },
    ...(isPlatformAdmin
      ? [
          {
            type: 'divider' as const,
          },
          {
            key: 'admin',
            icon: menuIcon(Settings),
            label: t('nav.admin'),
          },
        ]
      : []),
  ]

  return (
    <Layout className="app-shell">
      <Sider
        breakpoint="lg"
        collapsedWidth={64}
        width={232}
        theme="light"
        onCollapse={setCollapsed}
      >
        <div className="sider-brand">
          <Link to="/" aria-label="ForMarca">
            <BrandLogo height={collapsed ? 28 : 32} compact={collapsed} />
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
          <LocaleSwitcher />
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
