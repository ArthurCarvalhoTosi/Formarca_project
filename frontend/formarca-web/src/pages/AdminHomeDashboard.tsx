import { Alert, Skeleton, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { adminApi } from '@/features/admin/api'
import { invoicesApi } from '@/features/invoices/api'
import type { Invoice } from '@/features/invoices/types'
import { processesApi } from '@/features/processes/api'
import type { ProcessDashboardItem } from '@/features/processes/types'
import { ApiError } from '@/shared/api/types'
import { useI18n, type Dictionary } from '@/shared/i18n'
import { formatMoney } from '@/shared/money'

const LIST_SIZE = 5
const OPEN_BRAND_STAGES = 'Brand,Contract,Invoice'

const CLIENTS_HREF = '/admin/tenants?searched=1&status=Pending,Onboarding,Suspended,Active&recentDays=7'
const BRANDS_HREF = `/processos?view=funnel&searched=1&funnelStage=${OPEN_BRAND_STAGES},Process&recentDays=7`
const APPROVAL_HREF = '/faturas?searched=1&status=AwaitingApproval'
const PAYMENT_HREF = '/faturas?searched=1&status=PendingPayment'
const OPEN_INVOICES_HREF =
  '/faturas?searched=1&status=AwaitingApproval,PendingPayment,Rejected,Paid&recentDays=7'

function tenantStatusColor(status?: string) {
  switch (status) {
    case 'Pending':
      return 'warning'
    case 'Onboarding':
      return 'processing'
    default:
      return 'default'
  }
}

function invoiceStatusColor(status?: string) {
  switch (status) {
    case 'AwaitingApproval':
      return 'warning'
    case 'PendingPayment':
      return 'processing'
    case 'Rejected':
      return 'error'
    default:
      return 'default'
  }
}

function statusText(
  status: string | undefined,
  labels: Record<string, string>,
): string {
  if (!status) return '—'
  return status in labels ? labels[status] : status
}

function brandHint(item: ProcessDashboardItem, dict: Dictionary): string {
  if (item.funnelStage === 'Brand' || !item.contract) return dict.processes.waitingContract
  if (item.funnelStage === 'Invoice' || item.invoice) {
    if (item.invoice && item.invoice.status in dict.invoices.status) {
      return dict.invoices.status[item.invoice.status as keyof Dictionary['invoices']['status']]
    }
    return dict.processes.waitingPayment
  }
  if (item.contract.status === 'Issued') return dict.processes.waitingSignature
  if (item.contract.status in dict.contracts.status) {
    return dict.contracts.status[item.contract.status as keyof Dictionary['contracts']['status']]
  }
  return dict.processes.waitingInvoice
}

function Count({
  loading,
  value,
}: {
  loading: boolean
  value: number | null
}) {
  if (loading) return <Skeleton.Input active size="small" style={{ width: 48, height: 28 }} />
  return <strong>{value ?? '—'}</strong>
}

export function AdminHomeDashboard() {
  const { t, dict, locale } = useI18n()

  const pendingQuery = useQuery({
    queryKey: ['admin-home', 'tenants', 'Pending'],
    queryFn: () => adminApi.listTenants({ status: 'Pending', page: 1, pageSize: LIST_SIZE }),
  })

  const onboardingQuery = useQuery({
    queryKey: ['admin-home', 'tenants', 'Onboarding'],
    queryFn: () => adminApi.listTenants({ status: 'Onboarding', page: 1, pageSize: LIST_SIZE }),
  })

  const brandsQuery = useQuery({
    queryKey: ['admin-home', 'open-brands'],
    queryFn: () =>
      processesApi.dashboard({
        funnelStage: OPEN_BRAND_STAGES,
        page: 1,
        pageSize: LIST_SIZE,
      }),
  })

  const approvalQuery = useQuery({
    queryKey: ['admin-home', 'invoices', 'AwaitingApproval'],
    queryFn: () => invoicesApi.list({ status: 'AwaitingApproval', page: 1, pageSize: LIST_SIZE }),
  })

  const paymentQuery = useQuery({
    queryKey: ['admin-home', 'invoices', 'PendingPayment'],
    queryFn: () => invoicesApi.list({ status: 'PendingPayment', page: 1, pageSize: LIST_SIZE }),
  })

  const pendingClients = pendingQuery.data?.items ?? []
  const onboardingClients = onboardingQuery.data?.items ?? []
  const clientRows = [...pendingClients, ...onboardingClients].slice(0, LIST_SIZE)
  const tenantsLoading = pendingQuery.isLoading || onboardingQuery.isLoading
  const tenantsError = pendingQuery.isError || onboardingQuery.isError

  const brandRows = brandsQuery.data?.items ?? []
  const kpis = brandsQuery.data?.kpis
  const invoiceRows: Invoice[] = [
    ...(approvalQuery.data?.items ?? []),
    ...(paymentQuery.data?.items ?? []),
  ].slice(0, LIST_SIZE)

  const errorText = (error: unknown) =>
    error instanceof ApiError && error.message.trim() ? error.message : t('home.opsLoadError')

  return (
    <section className="home-section home-ops" aria-labelledby="home-ops-title">
      <div>
        <Typography.Title level={4} id="home-ops-title" className="home-section-title">
          {t('home.opsTitle')}
        </Typography.Title>
        <Typography.Paragraph type="secondary" className="home-ops-lead">
          {t('home.opsLead')}
        </Typography.Paragraph>
      </div>

      <div className="home-kpis">
        <Link className="home-kpi home-kpi-attention" to={CLIENTS_HREF}>
          <span>{t('home.kpiPendingClients')}</span>
          <Count
            loading={tenantsLoading}
            value={tenantsError ? null : (pendingQuery.data?.total ?? 0)}
          />
          <em>{t('home.kpiPendingClientsHint', { count: onboardingQuery.data?.total ?? 0 })}</em>
        </Link>
        <Link className="home-kpi home-kpi-brand" to={BRANDS_HREF}>
          <span>{t('home.kpiPendingBrands')}</span>
          <Count
            loading={brandsQuery.isLoading}
            value={brandsQuery.isError ? null : (brandsQuery.data?.total ?? 0)}
          />
          <em>{t('home.kpiPendingBrandsHint')}</em>
        </Link>
        <Link className="home-kpi home-kpi-attention" to={APPROVAL_HREF}>
          <span>{t('home.kpiInvoicesApproval')}</span>
          <Count
            loading={approvalQuery.isLoading}
            value={approvalQuery.isError ? null : (approvalQuery.data?.total ?? 0)}
          />
          <em>{t('home.kpiInvoicesApprovalHint')}</em>
        </Link>
        <Link className="home-kpi home-kpi-payment" to={PAYMENT_HREF}>
          <span>{t('home.kpiInvoicesPayment')}</span>
          <Count
            loading={paymentQuery.isLoading}
            value={paymentQuery.isError ? null : (paymentQuery.data?.total ?? 0)}
          />
          <em>{t('home.kpiInvoicesPaymentHint')}</em>
        </Link>
      </div>

      <div className="home-boards">
        <article className="home-board">
          <header className="home-board-head">
            <Typography.Title level={5} className="home-board-title">
              {t('home.boardClients')}
            </Typography.Title>
            <Link className="home-see-all" to={CLIENTS_HREF}>
              {t('home.seeAll')}
            </Link>
          </header>
          {tenantsError ? (
            <Alert type="warning" showIcon message={errorText(pendingQuery.error ?? onboardingQuery.error)} />
          ) : tenantsLoading ? (
            <Skeleton active paragraph={{ rows: 3 }} title={false} />
          ) : clientRows.length === 0 ? (
            <Typography.Paragraph type="secondary" className="home-board-empty">
              {t('home.emptyClients')}
            </Typography.Paragraph>
          ) : (
            <ul className="home-board-list">
              {clientRows.map((tenant) => (
                <li key={tenant.id}>
                  <Link className="home-board-link" to={`/clientes/${tenant.id}`}>
                    <span className="home-board-row">
                      <span className="home-board-name">{tenant.name}</span>
                      <Tag color={tenantStatusColor(tenant.status)}>
                        {statusText(tenant.status, dict.status)}
                      </Tag>
                    </span>
                    <span className="home-board-meta">{tenant.email || tenant.document || '—'}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </article>

        <article className="home-board">
          <header className="home-board-head">
            <Typography.Title level={5} className="home-board-title">
              {t('home.boardBrands')}
            </Typography.Title>
            <Link className="home-see-all" to={BRANDS_HREF}>
              {t('home.seeAll')}
            </Link>
          </header>
          {brandsQuery.isError ? (
            <Alert type="warning" showIcon message={errorText(brandsQuery.error)} />
          ) : brandsQuery.isLoading ? (
            <Skeleton active paragraph={{ rows: 3 }} title={false} />
          ) : brandRows.length === 0 ? (
            <Typography.Paragraph type="secondary" className="home-board-empty">
              {t('home.emptyBrands')}
            </Typography.Paragraph>
          ) : (
            <ul className="home-board-list">
              {brandRows.map((item) => (
                <li key={item.brandId}>
                  <Link className="home-board-link" to={`/marcas/${item.brandId}`}>
                    <span className="home-board-name">{item.brandName}</span>
                    <span className="home-board-meta">
                      {[item.tenantName, brandHint(item, dict)].filter(Boolean).join(' · ')}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </article>

        <article className="home-board">
          <header className="home-board-head">
            <Typography.Title level={5} className="home-board-title">
              {t('home.boardInvoices')}
            </Typography.Title>
            <Link className="home-see-all" to={OPEN_INVOICES_HREF}>
              {t('home.seeAll')}
            </Link>
          </header>
          {approvalQuery.isError || paymentQuery.isError ? (
            <Alert
              type="warning"
              showIcon
              message={errorText(approvalQuery.error ?? paymentQuery.error)}
            />
          ) : approvalQuery.isLoading || paymentQuery.isLoading ? (
            <Skeleton active paragraph={{ rows: 3 }} title={false} />
          ) : invoiceRows.length === 0 ? (
            <Typography.Paragraph type="secondary" className="home-board-empty">
              {t('home.emptyInvoices')}
            </Typography.Paragraph>
          ) : (
            <ul className="home-board-list">
              {invoiceRows.map((invoice) => (
                <li key={invoice.id}>
                  <Link className="home-board-link" to={`/contratos/${invoice.contractId}`}>
                    <span className="home-board-row">
                      <span className="home-board-name">
                        {invoice.contractCode || invoice.tenantName || t('home.boardInvoices')}
                      </span>
                      <Tag color={invoiceStatusColor(invoice.status)}>
                        {statusText(invoice.status, dict.invoices.status)}
                      </Tag>
                    </span>
                    <span className="home-board-meta">
                      {[invoice.tenantName, formatMoney(invoice.expectedAmount, locale)]
                        .filter(Boolean)
                        .join(' · ')}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      <div>
        <Typography.Title level={5} className="home-section-title">
          {t('home.quickTitle')}
        </Typography.Title>
        <ul className="home-quick">
          <li>
            <Link to="/admin/tenants?searched=1&status=Onboarding">
              {t('home.quickOnboarding')}
              <strong>{onboardingQuery.isLoading ? '…' : (onboardingQuery.data?.total ?? 0)}</strong>
            </Link>
          </li>
          <li>
            <Link to="/contratos?searched=1&status=PreIssued,Issued,Signed&recentDays=7">
              {t('home.quickContracts')}
              <strong>{brandsQuery.isLoading ? '…' : (kpis?.contractsInProgress ?? '—')}</strong>
            </Link>
          </li>
          <li>
            <Link to="/processos">
              {t('home.quickProcesses')}
              <strong>{brandsQuery.isLoading ? '…' : (kpis?.processesGenerated ?? '—')}</strong>
            </Link>
          </li>
          <li>
            <Link to="/marcas?searched=1&recentDays=7">
              {t('home.quickBrands')}
              <strong>{brandsQuery.isLoading ? '…' : (kpis?.brands ?? '—')}</strong>
            </Link>
          </li>
        </ul>
      </div>
    </section>
  )
}
