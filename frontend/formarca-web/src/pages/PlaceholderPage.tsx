import { Empty, Typography } from 'antd'

type PlaceholderPageProps = {
  title: string
  description?: string
}

export function PlaceholderPage({
  title,
  description = 'Módulo previsto no Painel do Cliente. Em breve.',
}: PlaceholderPageProps) {
  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {title}
      </Typography.Title>
      <Empty description={description} image={Empty.PRESENTED_IMAGE_SIMPLE} />
    </div>
  )
}
