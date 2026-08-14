import { Empty, Typography } from 'antd'
import { useI18n } from '@/shared/i18n'

type PlaceholderPageProps = {
  title: string
  description?: string
}

export function PlaceholderPage({ title, description }: PlaceholderPageProps) {
  const { t } = useI18n()

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {title}
      </Typography.Title>
      <Empty
        description={description ?? t('placeholder.comingSoon')}
        image={Empty.PRESENTED_IMAGE_SIMPLE}
      />
    </div>
  )
}
