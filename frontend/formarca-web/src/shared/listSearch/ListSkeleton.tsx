import { Card, Skeleton, Space } from 'antd'

type ListSkeletonProps = {
  cards?: boolean
}

export function ListSkeleton({ cards = false }: ListSkeletonProps) {
  if (cards) {
    return (
      <div className="brand-card-grid">
        {Array.from({ length: 4 }, (_, index) => (
          <Card key={index} className="brand-card">
            <Skeleton active paragraph={{ rows: 4 }} />
          </Card>
        ))}
      </div>
    )
  }

  return (
    <Space orientation="vertical" size="middle" style={{ width: '100%' }}>
      <Skeleton active title paragraph={{ rows: 1 }} />
      <Skeleton active title={false} paragraph={{ rows: 8 }} />
    </Space>
  )
}
