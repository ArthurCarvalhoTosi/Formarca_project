import { Alert, Pagination, Select, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState, type ReactNode } from 'react'
import { brandsApi } from '@/features/brands/api'
import { customersApi } from '@/features/customers/api'
import { ApiError, asList } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { recentActiveSince } from '@/shared/listSearch'
import { ProcessStatusDrawer } from './ProcessStatusDrawer'
import { trademarkApi } from './trademarkApi'
import {
  EMPTY_TRADEMARK_KPIS,
  TRADEMARK_PHASES,
  type TrademarkPhase,
} from './trademarkTypes'

type TrademarkDashboardProps = {
  viewSwitch: ReactNode
}

const PAGE_SIZE = 9
const OPEN_TONES = 'Attention,Danger,Paused'
const ACTIVE_TONES = 'Progress,Success'

export function TrademarkDashboard({ viewSwitch }: TrademarkDashboardProps) {
  const { t, dict, locale } = useI18n()
  const { isPlatformAdmin, hasPermission } = useAuth()
  const canManage = hasPermission('processes.manage')
  const [openOnly, setOpenOnly] = useState(true)
  const [phase, setPhase] = useState<TrademarkPhase | ''>('')
  const [statusId, setStatusId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [tenantId, setTenantId] = useState('')
  const [page, setPage] = useState(1)
  const [openProcessId, setOpenProcessId] = useState<string | null>(null)

  const showingOpen = openOnly && !phase && !statusId
  const filters = useMemo(
    () => ({
      phase: phase || undefined,
      statusId: statusId || undefined,
      brandId: brandId || undefined,
      tenantId: isPlatformAdmin ? tenantId || undefined : undefined,
      tone: showingOpen ? OPEN_TONES : undefined,
      includeWithoutStatus: showingOpen,
      page,
      pageSize: PAGE_SIZE,
    }),
    [phase, statusId, brandId, tenantId, isPlatformAdmin, page, showingOpen],
  )

  const dashboardQuery = useQuery({
    queryKey: ['trademark-dashboard', filters, showingOpen],
    queryFn: () =>
      trademarkApi.dashboard({
        ...filters,
        activeTone: showingOpen ? ACTIVE_TONES : undefined,
        activeSince: showingOpen ? recentActiveSince() : undefined,
      }),
  })

  const statusesQuery = useQuery({
    queryKey: ['trademark-statuses', 'active'],
    queryFn: () => trademarkApi.listStatuses(false),
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

  const kpis = dashboardQuery.data?.kpis ?? EMPTY_TRADEMARK_KPIS
  const items = dashboardQuery.data?.items ?? []
  const openCount = kpis.withoutStatus + kpis.actionRequired + kpis.paused + kpis.closed
  const hasFilters =
    Boolean(phase || statusId || brandId || tenantId) || (showingOpen && kpis.total > 0)
  const formatWhen = (value?: string | null) => {
    if (!value) return ''
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(value))
  }

  const resetPage = () => setPage(1)

  return (
    <div className="tm-page">
      <header className="tm-header">
        <div>
          <Typography.Title level={3} style={{ margin: 0 }}>
            {t('trademark.dashboardTitle')}
          </Typography.Title>
          <Typography.Paragraph type="secondary" style={{ margin: '6px 0 0' }}>
            {t('trademark.dashboardDescription')}
          </Typography.Paragraph>
        </div>
        {viewSwitch}
      </header>

      {dashboardQuery.isError ? (
        <Alert
          type="warning"
          showIcon
          message={t('trademark.unavailable')}
          description={
            dashboardQuery.error instanceof ApiError
              ? dashboardQuery.error.message
              : t('trademark.loadError')
          }
        />
      ) : (
        <>
          <section className="tm-kpis" aria-label={t('trademark.dashboardTitle')}>
            <Kpi label={t('trademark.kpiTotal')} value={kpis.total} tone="ink" />
            <Kpi label={t('trademark.kpiWithoutStatus')} value={kpis.withoutStatus} tone="neutral" />
            <Kpi label={t('trademark.kpiAction')} value={kpis.actionRequired} tone="attention" />
            <Kpi label={t('trademark.kpiPaused')} value={kpis.paused} tone="paused" />
            <Kpi label={t('trademark.kpiActive')} value={kpis.activeRights} tone="success" />
            <Kpi label={t('trademark.kpiClosed')} value={kpis.closed} tone="danger" />
          </section>

          {showingOpen ? (
            <Typography.Paragraph type="secondary" style={{ margin: '0 0 12px' }}>
              {t('trademark.openHint')}
            </Typography.Paragraph>
          ) : null}

          <div className="tm-rail" role="tablist" aria-label={t('trademark.filterPhase')}>
            <button
              type="button"
              className={`tm-phase${showingOpen ? ' is-active' : ''}`}
              onClick={() => {
                setOpenOnly(true)
                setPhase('')
                setStatusId('')
                resetPage()
              }}
            >
              <span className="tm-phase-count">{openCount}</span>
              <span>{t('trademark.openFilter')}</span>
            </button>
            <button
              type="button"
              className={`tm-phase${!openOnly && phase === '' ? ' is-active' : ''}`}
              onClick={() => {
                setOpenOnly(false)
                setPhase('')
                resetPage()
              }}
            >
              <span className="tm-phase-count">{kpis.total}</span>
              <span>{t('common.all')}</span>
            </button>
            {TRADEMARK_PHASES.map((item) => {
              const count = kpis.phases.find((entry) => entry.phase === item)?.count ?? 0
              return (
                <button
                  key={item}
                  type="button"
                  className={`tm-phase${phase === item ? ' is-active' : ''}`}
                  onClick={() => {
                    setOpenOnly(false)
                    setPhase((current) => (current === item ? '' : item))
                    resetPage()
                  }}
                >
                  <span className="tm-phase-count">{count}</span>
                  <span>{dict.trademark.phases[item]}</span>
                </button>
              )
            })}
          </div>

          <div className="tm-filters">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder={t('trademark.filterStatus')}
              value={statusId || undefined}
              onChange={(value) => {
                setStatusId(value ?? '')
                resetPage()
              }}
              options={(statusesQuery.data ?? []).map((status) => ({
                value: status.id,
                label: status.title,
              }))}
              style={{ minWidth: 260 }}
            />
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder={t('trademark.filterBrand')}
              value={brandId || undefined}
              onChange={(value) => {
                setBrandId(value ?? '')
                resetPage()
              }}
              options={(brandsQuery.data ?? []).map((brand) => ({
                value: brand.id,
                label: brand.name,
              }))}
              style={{ minWidth: 200 }}
            />
            {isPlatformAdmin ? (
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder={t('trademark.filterTenant')}
                value={tenantId || undefined}
                onChange={(value) => {
                  setTenantId(value ?? '')
                  resetPage()
                }}
                options={(tenantsQuery.data ?? []).map((tenant) => ({
                  value: tenant.id,
                  label: tenant.name,
                }))}
                style={{ minWidth: 200 }}
              />
            ) : null}
          </div>

          {dashboardQuery.isLoading ? (
            <div className="tm-grid">
              {Array.from({ length: 3 }, (_, index) => (
                <div key={index} className="tm-card tm-card-skeleton" />
              ))}
            </div>
          ) : items.length === 0 ? (
            <div className="tm-empty">
              <Typography.Title level={4}>
                {hasFilters ? t('trademark.noResultsTitle') : t('trademark.emptyTitle')}
              </Typography.Title>
              <Typography.Paragraph type="secondary">
                {hasFilters ? t('trademark.noResultsDescription') : t('trademark.emptyDescription')}
              </Typography.Paragraph>
            </div>
          ) : (
            <>
              <div className="tm-grid">
                {items.map((item) => {
                  const tone = item.status?.tone ?? 'Neutral'
                  const currentPhase = item.status?.phase
                  const currentIndex = currentPhase ? TRADEMARK_PHASES.indexOf(currentPhase) : -1
                  return (
                    <article key={item.processId} className={`tm-card tm-tone-${tone}`}>
                      <div className="tm-card-top">
                        <div>
                          <h3>{item.brandName}</h3>
                          <p>
                            {isPlatformAdmin && item.tenantName ? item.tenantName : null}
                            {item.processNumber
                              ? `${isPlatformAdmin && item.tenantName ? ' · ' : ''}${item.processNumber}`
                              : ''}
                          </p>
                        </div>
                        <span className={`tm-chip tm-tone-${tone}`}>
                          {item.status?.title ?? t('trademark.noStatus')}
                        </span>
                      </div>
                      <p className="tm-card-copy">
                        {item.status?.description ?? t('trademark.noStatusHint')}
                      </p>
                      <div className="tm-track" aria-hidden="true">
                        {TRADEMARK_PHASES.map((step, index) => {
                          const state =
                            currentIndex < 0
                              ? ''
                              : index < currentIndex
                                ? ' is-done'
                                : index === currentIndex
                                  ? ' is-current'
                                  : ''
                          return <span key={step} className={`tm-dot${state}`} />
                        })}
                      </div>
                      <div className="tm-card-foot">
                        <span>
                          {item.statusChangedAtUtc
                            ? t('trademark.updatedAt', { date: formatWhen(item.statusChangedAtUtc) })
                            : t('trademark.createdAt', { date: formatWhen(item.createdAtUtc) })}
                        </span>
                        <button type="button" onClick={() => setOpenProcessId(item.processId)}>
                          {canManage ? t('trademark.changeStatus') : t('trademark.openDetails')}
                        </button>
                      </div>
                    </article>
                  )
                })}
              </div>
              <div className="tm-pagination">
                <Pagination
                  current={dashboardQuery.data?.page ?? page}
                  pageSize={dashboardQuery.data?.pageSize ?? PAGE_SIZE}
                  total={dashboardQuery.data?.total ?? 0}
                  onChange={setPage}
                  showSizeChanger={false}
                />
              </div>
            </>
          )}
        </>
      )}

      <ProcessStatusDrawer
        processId={openProcessId}
        open={Boolean(openProcessId)}
        canManage={canManage}
        onClose={() => setOpenProcessId(null)}
      />
    </div>
  )
}

function Kpi({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <article className={`tm-kpi tm-tone-${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}
