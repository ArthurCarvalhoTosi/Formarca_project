import { Alert, Button, Layout, Menu, theme } from 'antd'
import type { MenuProps } from 'antd'
import {
  Briefcase,
  ClipboardList,
  FileText,
  Home,
  Info,
  PanelLeft,
  PanelLeftClose,
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

const NAV_COLLAPSED_KEY = 'formarca.navCollapsed'

const { Header, Sider, Content } = Layout

function readNavCollapsed(): boolean {
  try {
    return window.localStorage.getItem(NAV_COLLAPSED_KEY) === '1'
  } catch {
    return false
  }
}

function persistNavCollapsed(collapsed: boolean) {
  try {
    window.localStorage.setItem(NAV_COLLAPSED_KEY, collapsed ? '1' : '0')
  } catch {
    // Ignore quota / private-mode failures.
  }
}

function menuIcon(Icon: typeof Home) {
  return <Icon size={16} strokeWidth={1.75} />
}

export function AppShell() {
  const location = useLocation()
  const navigate = useNavigate()
  const { isPlatformAdmin, isOnboardingLimited, hasPermission } = useAuth()
  const { t } = useI18n()
  const [collapsed, setCollapsed] = useState(readNavCollapsed)
  const {
    token: { colorBgContainer },
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
    ...(!isOnboardingLimited && hasPermission('processes.read')
      ? [
          {
            key: 'processos',
            icon: menuIcon(ClipboardList),
            label: t('nav.processes'),
          },
        ]
      : []),
    ...(hasPermission('contracts.read')
      ? [
          {
            key: 'contratos',
            icon: menuIcon(FileText),
            label: t('nav.contracts'),
          },
        ]
      : []),
    ...(hasPermission('invoices.read')
      ? [
          {
            key: 'faturas',
            icon: menuIcon(Receipt),
            label: t('nav.invoices'),
          },
        ]
      : []),
    ...(hasPermission('registrations.read')
      ? [
          {
            key: 'clientes',
            icon: menuIcon(Briefcase),
            label: t('nav.customers'),
          },
        ]
      : []),
    ...(!isOnboardingLimited && hasPermission('brands.read')
      ? [
          {
            key: 'marcas',
            icon: menuIcon(Tag),
            label: t('nav.brands'),
          },
        ]
      : []),
    ...(!isOnboardingLimited
      ? [
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
        collapsible
        trigger={null}
        collapsed={collapsed}
        onCollapse={(next) => {
          setCollapsed(next)
          persistNavCollapsed(next)
        }}
        breakpoint="lg"
        collapsedWidth={64}
        width={232}
        theme="light"
      >
        <div className="sider-brand">
          <Link to="/" aria-label="ForMarca">
            <BrandLogo height={collapsed ? 28 : 32} compact={collapsed} />
          </Link>
          <Button
            type="text"
            className="sider-collapse-btn"
            aria-label={collapsed ? t('nav.expandMenu') : t('nav.collapseMenu')}
            icon={
              collapsed ? (
                <PanelLeft size={18} strokeWidth={1.75} />
              ) : (
                <PanelLeftClose size={18} strokeWidth={1.75} />
              )
            }
            onClick={() => {
              const next = !collapsed
              setCollapsed(next)
              persistNavCollapsed(next)
            }}
          />
        </div>
        <Menu
          mode="inline"
          selectedKeys={[selectedKey]}
          items={items}
          onClick={({ key }) => {
            setCollapsed(true)
            persistNavCollapsed(true)
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
          <div className="app-page">
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
