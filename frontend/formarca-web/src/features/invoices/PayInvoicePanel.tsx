import { Alert, App, Button, Card, Form, Radio, Select, Space, Spin, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useAuth } from '@/shared/auth/AuthContext'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { formatMoney } from '@/shared/money'
import { invoicesApi } from './api'
import { calculateExpectedAmount } from './calc'
import { INSTALLMENT_NUMBERS } from './types'
import type { InvoicePaymentMethod, SubmitPaymentRequest } from './types'

type PayInvoicePanelProps = {
  contractId: string
  contractStatus: string
}

export function PayInvoicePanel({ contractId, contractStatus }: PayInvoicePanelProps) {
  const { t, locale, dict } = useI18n()
  const { message, modal } = App.useApp()
  const { isPlatformAdmin, refreshSession } = useAuth()
  const queryClient = useQueryClient()
  const [method, setMethod] = useState<InvoicePaymentMethod>('Pix')
  const [installments, setInstallments] = useState(1)

  const invoiceQuery = useQuery({
    queryKey: ['invoices', 'contract', contractId],
    queryFn: () => invoicesApi.getByContract(contractId),
  })

  const settingsQuery = useQuery({
    queryKey: ['invoice-settings'],
    queryFn: () => invoicesApi.getSettings(),
  })

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['invoices'] }),
      queryClient.invalidateQueries({ queryKey: ['contracts'] }),
      queryClient.invalidateQueries({ queryKey: ['processes'] }),
      queryClient.invalidateQueries({ queryKey: ['customers'] }),
      queryClient.invalidateQueries({ queryKey: ['admin', 'tenants'] }),
    ])
    await refreshSession()
  }

  const payMutation = useMutation({
    mutationFn: (payload: SubmitPaymentRequest) =>
      invoicesApi.submitPayment(invoiceQuery.data!.id, payload),
    onSuccess: async () => {
      await invalidate()
      message.success(t('invoices.submitSuccess'))
    },
  })

  const approveMutation = useMutation({
    mutationFn: () =>
      invoicesApi.approve(invoiceQuery.data!.id, {
        receivedAmount: invoiceQuery.data!.expectedAmount,
      }),
    onSuccess: async () => {
      await invalidate()
      message.success(t('invoices.approveSuccess'))
    },
  })

  const rejectMutation = useMutation({
    mutationFn: () => invoicesApi.reject(invoiceQuery.data!.id),
    onSuccess: async () => {
      await invalidate()
      message.success(t('invoices.rejectSuccess'))
    },
  })

  const invoice = invoiceQuery.data
  const settings = settingsQuery.data
  const expectedPreview = useMemo(() => {
    if (!invoice || !settings) return null
    return calculateExpectedAmount(
      invoice.contractAmount,
      invoice.discount,
      settings,
      method,
      method === 'Pix' ? 1 : installments,
    )
  }, [invoice, settings, method, installments])

  if (invoiceQuery.isLoading) {
    return <Spin tip={t('invoices.loading')} />
  }

  if (invoiceQuery.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('invoices.unavailable')}
        description={
          invoiceQuery.error instanceof ApiError
            ? invoiceQuery.error.message
            : t('invoices.loadError')
        }
      />
    )
  }

  if (!invoice) {
    if (contractStatus !== 'Signed') {
      return (
        <Alert type="info" showIcon message={t('invoices.noInvoice')} />
      )
    }
    return null
  }

  const statusLabel =
    invoice.status in dict.invoices.status
      ? dict.invoices.status[invoice.status as keyof typeof dict.invoices.status]
      : invoice.status

  const canPay = invoice.status === 'PendingPayment' || invoice.status === 'Rejected'
  const canReview = isPlatformAdmin && invoice.status === 'AwaitingApproval'

  return (
    <Card title={t('invoices.payTitle')}>
      <Typography.Paragraph type="secondary">
        {t('invoices.payDescription')}
      </Typography.Paragraph>
      <Typography.Paragraph>
        {t('invoices.colStatus')}: <strong>{statusLabel}</strong>
        {' · '}
        {t('invoices.colAmount')}: {formatMoney(invoice.contractAmount, locale)}
        {' · '}
        {t('invoices.colExpected')}: {formatMoney(invoice.expectedAmount, locale)}
      </Typography.Paragraph>

      {invoice.status === 'AwaitingApproval' ? (
        <Alert type="info" showIcon message={t('invoices.awaitingApproval')} style={{ marginBottom: 16 }} />
      ) : null}
      {invoice.status === 'Paid' ? (
        <Alert type="success" showIcon message={t('invoices.paidMessage')} style={{ marginBottom: 16 }} />
      ) : null}
      {invoice.status === 'Rejected' ? (
        <Alert type="error" showIcon message={t('invoices.rejectedMessage')} style={{ marginBottom: 16 }} />
      ) : null}

      {canPay ? (
        <Form
          layout="vertical"
          onFinish={() => {
            void payMutation.mutateAsync({
              paymentMethod: method,
              installments: method === 'Pix' ? 1 : installments,
            })
          }}
        >
          <Form.Item label={t('invoices.method')} required>
            <Radio.Group
              value={method}
              onChange={(event) => setMethod(event.target.value)}
              options={[
                { value: 'Pix', label: t('invoices.pix') },
                { value: 'Card', label: t('invoices.card') },
              ]}
            />
          </Form.Item>
          {method === 'Card' ? (
            <Form.Item label={t('invoices.installments')} required>
              <Select
                value={installments}
                onChange={setInstallments}
                style={{ maxWidth: 200 }}
                options={INSTALLMENT_NUMBERS.map((n) => ({
                  value: n,
                  label: `${n}x`,
                }))}
              />
            </Form.Item>
          ) : null}
          {expectedPreview != null ? (
            <Typography.Paragraph>
              {t('invoices.expectedAmount')}:{' '}
              <Typography.Text strong>
                {formatMoney(expectedPreview, locale)}
              </Typography.Text>
            </Typography.Paragraph>
          ) : null}
          {payMutation.isError ? (
            <Alert
              type="error"
              showIcon
              message={
                payMutation.error instanceof ApiError
                  ? payMutation.error.message
                  : t('invoices.submitFailed')
              }
              style={{ marginBottom: 16 }}
            />
          ) : null}
          <Button type="primary" htmlType="submit" loading={payMutation.isPending}>
            {t('invoices.submitPayment')}
          </Button>
        </Form>
      ) : null}

      {canReview ? (
        <Space>
          <Button
            type="primary"
            loading={approveMutation.isPending}
            onClick={() => {
              modal.confirm({
                title: t('invoices.approveConfirmTitle'),
                okText: t('invoices.approve'),
                cancelText: t('common.cancel'),
                onOk: () => approveMutation.mutateAsync(),
              })
            }}
          >
            {t('invoices.approve')}
          </Button>
          <Button
            danger
            loading={rejectMutation.isPending}
            onClick={() => {
              modal.confirm({
                title: t('invoices.rejectConfirmTitle'),
                content: t('invoices.rejectConfirmContent'),
                okText: t('invoices.reject'),
                okButtonProps: { danger: true },
                cancelText: t('common.cancel'),
                onOk: () => rejectMutation.mutateAsync(),
              })
            }}
          >
            {t('invoices.reject')}
          </Button>
        </Space>
      ) : null}
    </Card>
  )
}
