import { Empty, Typography } from 'antd'
import { Search } from 'lucide-react'
import { useI18n } from '@/shared/i18n'

type ListEmptyStateProps = {
  searched?: boolean
}

export function ListEmptyState({ searched = false }: ListEmptyStateProps) {
  const { t } = useI18n()

  return (
    <div className="list-empty-state">
      <Empty
        image={
          <div className="list-empty-state-icon" aria-hidden>
            <Search size={36} strokeWidth={1.5} />
          </div>
        }
        description={
          <div>
            <Typography.Title level={4} style={{ marginTop: 8, marginBottom: 4 }}>
              {searched ? t('listSearch.noResultsTitle') : t('listSearch.emptyTitle')}
            </Typography.Title>
            <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
              {searched
                ? t('listSearch.noResultsDescription')
                : t('listSearch.emptyDescription')}
            </Typography.Paragraph>
          </div>
        }
      />
    </div>
  )
}
