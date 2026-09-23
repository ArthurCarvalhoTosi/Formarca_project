import { Card, Form, Pagination, Select, Segmented, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { brandsApi } from '@/features/brands/api'
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
import {
  TableViewToolbar,
  useTableViews,
  type TableViewColumn,
} from '@/shared/tableViews'
import { BrandFunnelCard } from './BrandFunnelCard'
import { TrademarkDashboard } from './TrademarkDashboard'
import { processesApi } from './api'
import {
  EMPTY_DASHBOARD_KPIS,
  FUNNEL_STAGES,
  type ProcessRecord,
} from './types'

const DEFAULT_FILTERS = {
  brandId: '',
  funnelStage: '',
  tenantId: '',
  recentDays: '',
}

const OPEN_FILTERS = {
  funnelStage: 'Brand,Contract,Invoice,Process',
  recentDays: String(RECENT_ACTIVE_DAYS),
}

export function ProcessesPage() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const search = useListSearch(DEFAULT_FILTERS, {
    autoSearch: true,
    initialFilters: OPEN_FILTERS,
  })
  const [params, setParams] = useSearchParams()
  const viewParam = params.get('view')
  const view = viewParam === 'table' || viewParam === 'funnel' ? viewParam : 'status'
  const setView = (next: string) => {
    const query = new URLSearchParams(params)
    if (next === 'status') query.delete('view')
    else query.set('view', next)
    setParams(query, { replace: true })
  }
  const viewSwitch = (
    <Segmented
      value={view}
      onChange={(value) => setView(String(value))}
      options={[
        { label: t('trademark.viewStatus'), value: 'status' },
        { label: t('trademark.viewFunnel'), value: 'funnel' },
        { label: t('processes.viewTable'), value: 'table' },
      ]}
    />
  )

  const dashboardFilters = {
    brandId: search.applied.brandId || undefined,
    funnelStage: search.applied.funnelStage || undefined,
    activeSince: activeSinceFromRecentDays(search.applied.recentDays),
    tenantId: isPlatformAdmin ? search.applied.tenantId || undefined : undefined,
    page: search.page,
    pageSize: search.pageSize,
  }

  const listFilters = {
    brandId: search.applied.brandId || undefined,
    tenantId: isPlatformAdmin ? search.applied.tenantId || undefined : undefined,
    page: search.page,
    pageSize: search.pageSize,
  }

  const dashboardQuery = useQuery({
    queryKey: ['processes', 'dashboard', dashboardFilters],
    queryFn: () => processesApi.dashboard(dashboardFilters),
    enabled: search.hasSearched && view === 'funnel',
  })

  const tableQuery = useQuery({
    queryKey: ['processes', listFilters],
    queryFn: () => processesApi.list(listFilters),
    enabled: search.hasSearched && view === 'table',
  })

  const brandsQuery = useQuery({
    queryKey: ['brands', 'options'],
    queryFn: async () => asList(await brandsApi.list({ pageSize: 100 })),
  })

  const tenantsQuery = useQuery({
    queryKey: ['customers', 'options', isPlatformAdmin],
    queryFn: async () => asList(await customersApi.list(isPlatformAdmin, { pageSize: 100 })),
    enabled: isPlatformAdmin,
  })

  const columns = useMemo<TableViewColumn<ProcessRecord>[]>(
    () => [
      {
        key: 'brandName',
        dataIndex: 'brandName',
        title: t('processes.colBrand'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'tenantName',
        dataIndex: 'tenantId',
        title: t('processes.colTenant'),
        adminOnly: true,
        render: (_: unknown, row: ProcessRecord) =>
          (tenantsQuery.data ?? []).find((tenant) => tenant.id === row.tenantId)?.name || '—',
      },
      {
        key: 'contractCode',
        dataIndex: 'contractCode',
        title: t('processes.colContract'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'processNumber',
        dataIndex: 'processNumber',
        title: t('processes.colNumber'),
        render: (value: string | null | undefined) => value || '—',
      },
      {
        key: 'status',
        dataIndex: 'status',
        title: t('processes.colStatus'),
        render: (value: string, row: ProcessRecord) =>
          row.trademarkStatusTitle ||
          (value === 'Generated' ? t('processes.statusGenerated') : value),
      },
    ],
    [t, tenantsQuery.data],
  )

  const tableViews = useTableViews({ tableKey: 'processes', columns })
  const kpis = dashboardQuery.data?.kpis ?? EMPTY_DASHBOARD_KPIS
  const activeQuery = view === 'table' ? tableQuery : dashboardQuery

  if (view === 'status') {
    return <TrademarkDashboard viewSwitch={viewSwitch} />
  }
  const stageLabel = (stage: string) => {
    if (stage === 'Brand') return t('processes.stageBrand')
    if (stage === 'Contract') return t('processes.stageContract')
    if (stage === 'Invoice') return t('processes.stageInvoice')
    if (stage === 'Process') return t('processes.stageProcess')
    return stage
  }

  return (
    <ListSearchLayout
      title={t('processes.title')}
      description={t('processes.dashboardDescription')}
      search={search}
      extra={viewSwitch}
      views={view === 'table' && search.hasSearched ? <TableViewToolbar controller={tableViews} /> : null}
      loading={activeQuery.isLoading}
      error={activeQuery.error}
      errorTitle={t('processes.unavailable')}
      errorFallback={t('processes.loadError')}
      skeletonCards={view === 'funnel'}
      chips={[
        ...csvChips(
          search.applied.brandId,
          'brandId',
          t('processes.filterBrand'),
          (value) => (brandsQuery.data ?? []).find((row) => row.id === value)?.name ?? value,
          (value) => search.removeCsvValue('brandId', value),
        ),
        ...csvChips(
          search.applied.funnelStage,
          'funnelStage',
          t('processes.filterStage'),
          stageLabel,
          (value) => search.removeCsvValue('funnelStage', value),
        ),
        search.applied.recentDays
          ? {
              key: 'recentDays',
              label: t('listSearch.recentActive', { days: search.applied.recentDays }),
            }
          : null,
        ...csvChips(
          search.applied.tenantId,
          'tenantId',
          t('processes.filterTenant'),
          (value) => (tenantsQuery.data ?? []).find((row) => row.id === value)?.name ?? value,
          (value) => search.removeCsvValue('tenantId', value),
        ),
      ].filter(Boolean) as { key: string; label: string }[]}
      filters={
        <>
          <Form.Item label={t('processes.filterBrand')}>
            <Select
              mode="multiple"
              allowClear
              showSearch
              optionFilterProp="label"
              maxTagCount="responsive"
              placeholder={t('processes.filterBrand')}
              value={splitCsv(search.draft.brandId)}
              onChange={(value) => search.setDraftValue('brandId', joinCsv(value))}
              options={(brandsQuery.data ?? []).map((row) => ({
                value: row.id,
                label: row.name,
              }))}
            />
          </Form.Item>
          <Form.Item label={t('processes.filterStage')}>
            <Select
              mode="multiple"
              allowClear
              maxTagCount="responsive"
              placeholder={t('processes.filterStage')}
              value={splitCsv(search.draft.funnelStage)}
              onChange={(value) => search.setDraftValue('funnelStage', joinCsv(value))}
              options={FUNNEL_STAGES.map((value) => ({
                value,
                label: stageLabel(value),
              }))}
            />
          </Form.Item>
          {isPlatformAdmin ? (
            <Form.Item label={t('processes.filterTenant')}>
              <Select
                mode="multiple"
                allowClear
                showSearch
                optionFilterProp="label"
                maxTagCount="responsive"
                placeholder={t('processes.filterTenant')}
                value={splitCsv(search.draft.tenantId)}
                onChange={(value) => search.setDraftValue('tenantId', joinCsv(value))}
                options={(tenantsQuery.data ?? []).map((row) => ({
                  value: row.id,
                  label: row.name,
                }))}
              />
            </Form.Item>
          ) : null}
        </>
      }
    >
      {view === 'funnel' ? (
        <>
          <div className="kpi-grid">
            <Card className="kpi-card">
              <Typography.Text type="secondary">{t('processes.kpiBrands')}</Typography.Text>
              <Typography.Title level={3} style={{ margin: '4px 0 0' }}>
                {kpis.brands}
              </Typography.Title>
            </Card>
            <Card className="kpi-card">
              <Typography.Text type="secondary">{t('processes.kpiContracts')}</Typography.Text>
              <Typography.Title level={3} style={{ margin: '4px 0 0' }}>
                {kpis.contractsInProgress}
              </Typography.Title>
            </Card>
            <Card className="kpi-card">
              <Typography.Text type="secondary">{t('processes.kpiInvoices')}</Typography.Text>
              <Typography.Title level={3} style={{ margin: '4px 0 0' }}>
                {kpis.invoicesPending}
              </Typography.Title>
            </Card>
            <Card className="kpi-card">
              <Typography.Text type="secondary">{t('processes.kpiProcesses')}</Typography.Text>
              <Typography.Title level={3} style={{ margin: '4px 0 0' }}>
                {kpis.processesGenerated}
              </Typography.Title>
            </Card>
          </div>
          {(dashboardQuery.data?.items ?? []).length === 0 ? (
            <div className="list-surface">
              <ListEmptyState searched />
            </div>
          ) : (
            <>
              <div className="brand-card-grid">
                {(dashboardQuery.data?.items ?? []).map((item) => (
                  <BrandFunnelCard key={item.brandId} item={item} />
                ))}
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
                <Pagination
                  current={dashboardQuery.data?.page ?? search.page}
                  pageSize={dashboardQuery.data?.pageSize ?? search.pageSize}
                  total={dashboardQuery.data?.total ?? 0}
                  showSizeChanger
                  onChange={search.setPage}
                />
              </div>
            </>
          )}
        </>
      ) : (
        <div className="list-surface">
          <Table<ProcessRecord>
            rowKey="id"
            dataSource={tableQuery.data?.items ?? []}
            locale={{ emptyText: <ListEmptyState searched /> }}
            onRow={(record) => ({
              onClick: () => navigate(`/contratos/${record.contractId}`),
              style: { cursor: 'pointer' },
            })}
            columns={tableViews.visibleColumns}
            pagination={toTablePagination(
              {
                total: tableQuery.data?.total ?? 0,
                page: tableQuery.data?.page ?? search.page,
                pageSize: tableQuery.data?.pageSize ?? search.pageSize,
              },
              search.setPage,
            )}
            sticky
          />
        </div>
      )}
    </ListSearchLayout>
  )
}
