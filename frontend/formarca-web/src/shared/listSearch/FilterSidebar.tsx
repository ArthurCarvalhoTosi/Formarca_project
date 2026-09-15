import { Button, Drawer, Form, Grid, Typography } from 'antd'
import type { ReactNode } from 'react'
import { useI18n } from '@/shared/i18n'

type FilterSidebarProps = {
  open: boolean
  onClose: () => void
  onSearch: () => void
  onReset: () => void
  children: ReactNode
}

export function FilterSidebar({
  open,
  onClose,
  onSearch,
  onReset,
  children,
}: FilterSidebarProps) {
  const { t } = useI18n()
  const screens = Grid.useBreakpoint()
  const overlay = !screens.lg

  return (
    <Drawer
      className="filter-sidebar-drawer"
      title={t('listSearch.title')}
      placement="right"
      width={320}
      open={open}
      onClose={onClose}
      mask={overlay}
      maskClosable={overlay}
      getContainer={overlay ? undefined : false}
      destroyOnHidden={false}
      extra={
        <Button type="text" size="small" onClick={onClose}>
          {t('listSearch.collapse')}
        </Button>
      }
    >
      <Form
        layout="vertical"
        onFinish={onSearch}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && (event.target as HTMLElement).tagName !== 'TEXTAREA') {
            event.preventDefault()
            onSearch()
          }
        }}
      >
        {children}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
          <Button type="primary" htmlType="submit" block>
            {t('listSearch.apply')}
          </Button>
          <Button htmlType="button" block onClick={onReset}>
            {t('listSearch.clear')}
          </Button>
        </div>
        <Typography.Paragraph type="secondary" style={{ marginTop: 16, marginBottom: 0 }}>
          {t('listSearch.emptyHint')}
        </Typography.Paragraph>
      </Form>
    </Drawer>
  )
}
