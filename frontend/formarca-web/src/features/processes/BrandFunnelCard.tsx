import { Card, Progress, Tag, Typography } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import type { ProcessDashboardItem } from './types'

function contractColor(status?: string) {
  switch (status) {
    case 'PreIssued':
      return 'default'
    case 'Issued':
      return 'processing'
    case 'Signed':
      return 'success'
    case 'Cancelled':
      return 'error'
    default:
      return 'default'
  }
}

function invoiceColor(status?: string) {
  switch (status) {
    case 'PendingPayment':
      return 'default'
    case 'AwaitingApproval':
      return 'warning'
    case 'Paid':
      return 'success'
    case 'Rejected':
      return 'error'
    default:
      return 'default'
  }
}

function funnelPercent(stage: ProcessDashboardItem['funnelStage']): number {
  switch (stage) {
    case 'Brand':
      return 25
    case 'Contract':
      return 50
    case 'Invoice':
      return 75
    case 'Process':
      return 100
    default:
      return 0
  }
}

type BrandFunnelCardProps = {
  item: ProcessDashboardItem
}

export function BrandFunnelCard({ item }: BrandFunnelCardProps) {
  const { t, dict } = useI18n()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()

  const contractLabel = item.contract
    ? item.contract.status in dict.contracts.status
      ? dict.contracts.status[item.contract.status as keyof typeof dict.contracts.status]
      : item.contract.status
    : t('processes.waitingContract')

  const invoiceLabel = item.invoice
    ? item.invoice.status in dict.invoices.status
      ? dict.invoices.status[item.invoice.status as keyof typeof dict.invoices.status]
      : item.invoice.status
    : item.contract?.status === 'Issued'
      ? t('processes.waitingSignature')
      : t('processes.waitingInvoice')

  const processLabel = item.process
    ? item.process.processNumber || t('processes.processGenerated')
    : t('processes.waitingPayment')

  const go = (path: string | null) => {
    if (path) navigate(path)
  }

  return (
    <Card className="brand-card" hoverable>
      <Typography.Title level={5} style={{ marginTop: 0, marginBottom: 4 }}>
        {item.brandName}
      </Typography.Title>
      {isPlatformAdmin && item.tenantName ? (
        <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
          {item.tenantName}
        </Typography.Paragraph>
      ) : null}
      <Progress
        percent={funnelPercent(item.funnelStage)}
        showInfo={false}
        strokeColor="var(--fm-primary)"
      />
      <div className="brand-card-funnel">
        <button
          type="button"
          className="brand-card-step"
          onClick={() => go(`/marcas/${item.brandId}`)}
        >
          <span className="brand-card-step-label">{t('processes.funnelBrand')}</span>
          <Tag>{item.brandName}</Tag>
        </button>
        <button
          type="button"
          className={`brand-card-step${item.contract ? '' : ' is-disabled'}`}
          onClick={() => go(item.contract ? `/contratos/${item.contract.id}` : null)}
        >
          <span className="brand-card-step-label">{t('processes.funnelContract')}</span>
          <Tag color={contractColor(item.contract?.status)}>
            {item.contract ? `${item.contract.code} · ${contractLabel}` : contractLabel}
          </Tag>
        </button>
        <button
          type="button"
          className={`brand-card-step${item.invoice || item.contract ? '' : ' is-disabled'}`}
          onClick={() => {
            if (item.contract) {
              go(`/faturas?searched=1&contractId=${item.contract.id}`)
            }
          }}
        >
          <span className="brand-card-step-label">{t('processes.funnelInvoice')}</span>
          <Tag color={invoiceColor(item.invoice?.status)}>{invoiceLabel}</Tag>
        </button>
        <button
          type="button"
          className={`brand-card-step${item.process || item.contract ? '' : ' is-disabled'}`}
          onClick={() => {
            if (item.contract) go(`/contratos/${item.contract.id}`)
          }}
        >
          <span className="brand-card-step-label">{t('processes.funnelProcess')}</span>
          <Tag color={item.process ? 'success' : 'default'}>{processLabel}</Tag>
        </button>
      </div>
    </Card>
  )
}
