import { Form, Input, Table } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { Group } from './types'
import { useI18n } from '@/shared/i18n'
import {
  ListEmptyState,
  ListSearchLayout,
  toTablePagination,
  useListSearch,
} from '@/shared/listSearch'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'

const DEFAULT_FILTERS = {
  q: '',
}

function matches(row: Group, query: string): boolean {
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  return [row.name, row.description, row.tenantId].some((value) =>
    (value ?? '').toLowerCase().includes(needle),
  )
}

export function GroupsPage() {
  const { t } = useI18n()
  const search = useListSearch(DEFAULT_FILTERS)

  const query = useQuery({
    queryKey: ['admin', 'groups', search.page, search.pageSize],
    queryFn: () => adminApi.listGroups({ page: search.page, pageSize: search.pageSize }),
    enabled: search.hasSearched,
  })

  const columns = useMemo<TableViewColumn<Group>[]>(
    () => [
      { key: 'name', dataIndex: 'name', title: t('admin.colName') },
      {
        key: 'tenantId',
        dataIndex: 'tenantId',
        title: t('admin.colScope'),
        render: (tenantId: string | null | undefined) =>
          tenantId ? t('admin.scopeTenant', { id: tenantId }) : t('admin.scopePlatform'),
      },
      { key: 'description', dataIndex: 'description', title: t('admin.colDescription') },
    ],
    [t],
  )

  const tableViews = useTableViews({ tableKey: 'admin.groups', columns })
  const items = useMemo(
    () => (query.data?.items ?? []).filter((row) => matches(row, search.applied.q)),
    [query.data?.items, search.applied.q],
  )

  return (
    <ListSearchLayout
      title={t('admin.groupsTitle')}
      description={t('admin.groupsDescription')}
      search={search}
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('admin.groupsUnavailable')}
      errorFallback={t('admin.groupsLoadError')}
      chips={
        search.applied.q
          ? [{ key: 'q', label: `${t('common.search')}: ${search.applied.q}` }]
          : []
      }
      filters={
        <Form.Item label={t('common.search')}>
          <Input
            allowClear
            placeholder={t('admin.searchPlaceholder')}
            value={search.draft.q}
            onChange={(event) => search.setDraftValue('q', event.target.value)}
          />
        </Form.Item>
      }
    >
      <div className="list-surface">
        <Table<Group>
          rowKey="id"
          dataSource={items}
          locale={{ emptyText: <ListEmptyState searched /> }}
          columns={tableViews.visibleColumns}
          pagination={toTablePagination(
            {
              total: query.data?.total ?? 0,
              page: query.data?.page ?? search.page,
              pageSize: query.data?.pageSize ?? search.pageSize,
            },
            search.setPage,
          )}
          sticky
        />
      </div>
    </ListSearchLayout>
  )
}
