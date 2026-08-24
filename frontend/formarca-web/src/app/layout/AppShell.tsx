import { Alert, Layout, Menu, theme } from 'antd'
import type { MenuProps } from 'antd'
import {
  Briefcase,
  ClipboardList,
  FileText,
  Home,
  Info,
  Receipt,
  Settings,
  Tag,
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
  const { isPlatformAdmin, isOnboardingLimited } = useAuth()
  const { t } = useI18n()
  const [collapsed, setCollapsed] = useState(false)
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken()

  const selectedKey = useMemo(() => {
    const path = location.pathname
    if (path.startsWith('/admin')) return 'admin'
    if (path.startsWith('/processos') || path.startsWith('/processes')) return 'processos'
    if (path.startsWith('/contratos') || path.startsWith('/contracts')) return 'contratos'
    if (path.startsWith('/faturas') || path.startsWith('/invoices')) return 'faturas'
    if (path.startsWith('/marcas') || path.startsWith('/brands')) return 'marcas'
    if (
      path.startsWith('/clientes') ||
      path.startsWith('/cadastro') ||
      path.startsWith('/registration')
    ) {
      return 'clientes'
    }
    if (path.startsWith('/informacoes') || path.startsWith('/information')) {
      return 'informacoes'
    }
    return 'home'
  }, [location.pathname])

  const items: MenuProps['items'] = [
    {
      key: 'home',
      icon: menuIcon(Home),
      label: t('nav.home'),
    },
    ...(!isOnboardingLimited
      ? [
          {
            key: 'processos',
            icon: menuIcon(ClipboardList),
            label: t('nav.processes'),
          },
        ]
      : []),
    {
      key: 'contratos',
      icon: menuIcon(FileText),
      label: t('nav.contracts'),
    },
    {
      key: 'faturas',
      icon: menuIcon(Receipt),
      label: t('nav.invoices'),
    },
    {
      key: 'clientes',
      icon: menuIcon(Briefcase),
      label: t('nav.customers'),
    },
    ...(!isOnboardingLimited
      ? [
          {
            key: 'marcas',
            icon: menuIcon(Tag),
            label: t('nav.brands'),
          },
          {
            key: 'informacoes',
            icon: menuIcon(Info),
            label: t('nav.information'),
          },
        ]
      : []),
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
            {isOnboardingLimited ? (
              <Alert
                type="info"
                showIcon
                message={t('onboarding.bannerTitle')}
                description={t('onboarding.bannerDescription')}
                style={{ marginBottom: 16 }}
              />
            ) : null}
            <Outlet />
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}
