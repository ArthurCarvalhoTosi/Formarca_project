import { Alert, Badge, Button, Space, Tag, Typography } from 'antd'
import { SlidersHorizontal } from 'lucide-react'
import type { ReactNode } from 'react'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { FilterSidebar } from './FilterSidebar'
import { ListEmptyState } from './ListEmptyState'
import { ListSkeleton } from './ListSkeleton'
import type { FilterChip, UseListSearchResult } from './useListSearch'

type ListSearchLayoutProps<T extends Record<string, string>> = {
  title: string
  description?: string
  extra?: ReactNode
  views?: ReactNode
  banner?: ReactNode
  search: UseListSearchResult<T>
  chips?: FilterChip[]
  filters: ReactNode
  loading?: boolean
  error?: unknown
  errorTitle: string
  errorFallback: string
  skeletonCards?: boolean
  children: ReactNode
}

export function ListSearchLayout<T extends Record<string, string>>({
  title,
  description,
  extra,
  views,
  banner,
  search,
  chips = [],
  filters,
  loading = false,
  error,
  errorTitle,
  errorFallback,
  skeletonCards = false,
  children,
}: ListSearchLayoutProps<T>) {
  const { t } = useI18n()
  const errorMessage =
    error instanceof ApiError ? error.message : error ? errorFallback : null

  return (
    <div
      className={`list-search-shell${search.sidebarOpen ? ' is-sidebar-open' : ''}`}
    >
      <div className="list-search-main">
        <header className="page-header">
          <div className="page-header-copy">
            <Typography.Title level={3} className="page-header-title">
              {title}
            </Typography.Title>
            {description ? (
              <Typography.Paragraph type="secondary" className="page-header-description">
                {description}
              </Typography.Paragraph>
            ) : null}
          </div>
          <Space wrap className="page-header-actions">
            <Badge count={search.appliedCount} size="small">
              <Button
                icon={<SlidersHorizontal size={16} />}
                onClick={() =>
                  search.sidebarOpen ? search.collapseSidebar() : search.openSidebar()
                }
              >
                {t('listSearch.openFilters')}
              </Button>
            </Badge>
            {extra}
            {views}
          </Space>
        </header>

        {banner}

        {chips.length > 0 ? (
          <div className="filter-chip-row">
            {chips.map((chip) => (
              <Tag
                key={chip.key}
                closable
                className="filter-chip"
                onClose={() =>
                  chip.onClose
                    ? chip.onClose()
                    : search.removeFilter(chip.key as keyof T)
                }
              >
                {chip.label}
              </Tag>
            ))}
          </div>
        ) : null}

        {errorMessage ? (
          <Alert
            type="warning"
            showIcon
            message={errorTitle}
            description={errorMessage}
            style={{ marginBottom: 16 }}
          />
        ) : null}

        {!search.hasSearched ? (
          <div className="list-surface">
            <ListEmptyState />
          </div>
        ) : loading ? (
          <div className="list-surface">
            <ListSkeleton cards={skeletonCards} />
          </div>
        ) : errorMessage ? null : (
          children
        )}
      </div>

      <FilterSidebar
        open={search.sidebarOpen}
        onClose={search.collapseSidebar}
        onSearch={search.search}
        onReset={search.resetDraft}
      >
        {filters}
      </FilterSidebar>
    </div>
  )
}
