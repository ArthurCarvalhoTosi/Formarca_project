import { App, Button, Form, Input, Select, Table } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { contractsApi } from '@/features/contracts/api'
import { customersApi } from '@/features/customers/api'
import { asList } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import {
  activeSinceFromRecentDays,
  csvChips,
  joinCsv,
  ListEmptyState,
  ListSearchLayout,
  RECENT_ACTIVE_DAYS,
  splitCsv,
  toTablePagination,
  useListSearch,
} from '@/shared/listSearch'
import { formatClasses } from '@/shared/money'
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { brandsApi } from './api'
import { preIssueErrorMessage } from './preIssueError'
import { NICE_CLASSES, type Brand } from './types'

const DEFAULT_FILTERS = {
  name: '',
  inpiClass: '',
  tenantId: '',
  recentDays: '',
}

export function BrandsPage() {
  const { t } = useI18n()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { isPlatformAdmin, hasPermission } = useAuth()
  const queryClient = useQueryClient()
  const search = useListSearch(DEFAULT_FILTERS, {
    autoSearch: true,
    initialFilters: { recentDays: String(RECENT_ACTIVE_DAYS) },
  })
  const canCreate = hasPermission('brands.manage')
  const canPreIssue = hasPermission('contracts.manage')

  const filters = {
    name: search.applied.name.trim() || undefined,
    inpiClass: search.applied.inpiClass || undefined,
    tenantId: isPlatformAdmin ? search.applied.tenantId || undefined : undefined,
    createdFrom: activeSinceFromRecentDays(search.applied.recentDays),
    page: search.page,
    pageSize: search.pageSize,
  }

  const query = useQuery({
    queryKey: ['brands', filters],
    queryFn: () => brandsApi.list(filters),
    enabled: search.hasSearched,
  })

  const tenantsQuery = useQuery({
    queryKey: ['customers', 'options', isPlatformAdmin],
    queryFn: async () => asList(await customersApi.list(isPlatformAdmin, { pageSize: 100 })),
    enabled: isPlatformAdmin,
  })

  const preIssueMutation = useMutation({
    mutationFn: (brandId: string) => contractsApi.preIssue({ brandId }),
    onSuccess: async (contract) => {
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      message.success(t('brands.preIssueSuccess'))
      navigate(`/contratos/${contract.id}`)
    },
    onError: (error) => {
      message.error(preIssueErrorMessage(error, t))
    },
  })

  const tenantOptions = useMemo(
    () => (tenantsQuery.data ?? []).map((row) => ({ value: row.id, label: row.name })),
    [tenantsQuery.data],
  )

  const columns = useMemo<TableViewColumn<Brand>[]>(
    () => [
      { key: 'name', dataIndex: 'name', title: t('brands.colName') },
      {
        key: 'tenantName',
        dataIndex: 'tenantName',
        title: t('brands.colTenant'),
        adminOnly: true,
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'inpiClasses',
        dataIndex: 'inpiClasses',
        title: t('brands.colClasses'),
        render: (value: number[]) => formatClasses(value),
      },
      {
        key: 'servicesDescription',
        dataIndex: 'servicesDescription',
        title: t('brands.colDescription'),
        ellipsis: true,
      },
      {
        key: 'actions',
        title: t('common.actions'),
        hideable: false,
        adminOnly: true,
        render: (_: unknown, row: Brand) =>
          canPreIssue ? (
            <Button
              size="small"
              onClick={(event) => {
                event.stopPropagation()
                preIssueMutation.mutate(row.id)
              }}
              loading={preIssueMutation.isPending}
            >
              {t('brands.preIssue')}
            </Button>
          ) : null,
      },
    ],
    [canPreIssue, preIssueMutation, t],
  )

  const tableViews = useTableViews({ tableKey: 'brands', columns })

  return (
    <ListSearchLayout
      title={t('brands.title')}
      description={t('brands.description')}
      search={search}
      extra={
        canCreate ? (
          <Button type="primary" onClick={() => navigate('/marcas/novo')}>
            {t('brands.new')}
          </Button>
        ) : null
      }
      views={<TableViewToolbar controller={tableViews} />}
      loading={query.isLoading}
      error={query.error}
      errorTitle={t('brands.unavailable')}
      errorFallback={t('brands.loadError')}
      chips={[
        search.applied.name
          ? { key: 'name', label: `${t('brands.fields.name')}: ${search.applied.name}` }
          : null,
        ...csvChips(
          search.applied.inpiClass,
          'inpiClass',
          t('brands.filterClass'),
          (value) => value,
          (value) => search.removeCsvValue('inpiClass', value),
        ),
        ...csvChips(
          search.applied.tenantId,
          'tenantId',
          t('brands.filterTenant'),
          (value) => tenantOptions.find((row) => row.value === value)?.label ?? value,
          (value) => search.removeCsvValue('tenantId', value),
        ),
        search.applied.recentDays
          ? {
              key: 'recentDays',
              label: t('listSearch.recentActive', { days: search.applied.recentDays }),
            }
          : null,
      ].filter(Boolean) as { key: string; label: string }[]}
      filters={
        <>
          <Form.Item label={t('brands.fields.name')}>
            <Input
              allowClear
              placeholder={t('brands.searchPlaceholder')}
              value={search.draft.name}
              onChange={(event) => search.setDraftValue('name', event.target.value)}
            />
          </Form.Item>
          <Form.Item label={t('brands.filterClass')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('brands.filterClass')}
              value={splitCsv(search.draft.inpiClass)}
              onChange={(value) => search.setDraftValue('inpiClass', joinCsv(value))}
              options={NICE_CLASSES.map((n) => ({ value: String(n), label: String(n) }))}
            />
          </Form.Item>
          {isPlatformAdmin ? (
            <Form.Item label={t('brands.filterTenant')}>
              <Select
                mode="multiple"
                allowClear
                showSearch
                optionFilterProp="label"
                maxTagCount="responsive"
                placeholder={t('brands.filterTenant')}
                value={splitCsv(search.draft.tenantId)}
                onChange={(value) => search.setDraftValue('tenantId', joinCsv(value))}
                options={tenantOptions}
              />
            </Form.Item>
          ) : null}
        </>
      }
    >
      <div className="list-surface">
        <Table<Brand>
          rowKey="id"
          dataSource={query.data?.items ?? []}
          locale={{ emptyText: <ListEmptyState searched /> }}
          onRow={(record) => ({
            onClick: () => navigate(`/marcas/${record.id}`),
            style: { cursor: 'pointer' },
          })}
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
