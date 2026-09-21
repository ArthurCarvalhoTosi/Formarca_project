import { Form, Input, Table } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { adminApi } from './api'
import type { UserListItem } from './types'
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

function matches(row: UserListItem, query: string): boolean {
  const needle = query.trim().toLowerCase()
  if (!needle) return true
  return [row.email, row.displayName, row.platformRole].some((value) =>
    (value ?? '').toLowerCase().includes(needle),
  )
}

export function UsersPage() {
  const { t } = useI18n()
  const search = useListSearch(DEFAULT_FILTERS)

  const query = useQuery({
    queryKey: ['admin', 'users', search.page, search.pageSize],
    queryFn: () => adminApi.listUsers({ page: search.page, pageSize: search.pageSize }),
    enabled: search.hasSearched,
  })

  const columns = useMemo<TableViewColumn<UserListItem>[]>(
    () => [
      { key: 'email', dataIndex: 'email', title: t('admin.colEmail') },
      { key: 'displayName', dataIndex: 'displayName', title: t('admin.colDisplayName') },
      { key: 'platformRole', dataIndex: 'platformRole', title: t('admin.colPlatformRole') },
      {
        key: 'isActive',
        dataIndex: 'isActive',
        title: t('admin.colActive'),
        render: (value: boolean | undefined) =>
          value === false ? t('common.no') : t('common.yes'),
      },
    ],
    [t],
  )

  const tableViews = useTableViews({ tableKey: 'admin.users', columns })
  const items = useMemo(
    () => (query.data?.items ?? []).filter((row) => matches(row, search.applied.q)),
    [query.data?.items, search.applied.q],
  )

  return (
    <ListSearchLayout
      title={t('admin.usersTitle')}
      description={t('admin.usersDescription')}
      search={search}
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('admin.usersUnavailable')}
      errorFallback={t('admin.usersLoadError')}
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
        <Table<UserListItem>
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
