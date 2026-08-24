import { Alert, App, Button, Card, Form, Input, InputNumber, Select, Space, Spin, Tag, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { NICE_CLASSES } from '@/features/brands/types'
import { PayInvoicePanel } from '@/features/invoices/PayInvoicePanel'
import { maskCpf, maskTaxId } from '@/features/registration/masks'
import { isValidCpf, isValidTaxId, onlyDigits } from '@/features/registration/validation'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatDate, parseClasses } from '@/shared/money'
import { contractsApi } from './api'
import type { UpdateContractRequest } from './types'
import type { ChangeEvent } from 'react'

function statusColor(status?: string) {
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

export function ContractDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { t, locale, dict } = useI18n()
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const { isPlatformAdmin } = useAuth()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<UpdateContractRequest>()

  const query = useQuery({
    queryKey: ['contracts', id],
    queryFn: () => contractsApi.get(id!),
    enabled: Boolean(id),
  })

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['contracts'] }),
      queryClient.invalidateQueries({ queryKey: ['invoices'] }),
      queryClient.invalidateQueries({ queryKey: ['processes'] }),
    ])
  }

  const updateMutation = useMutation({
    mutationFn: (values: UpdateContractRequest) =>
      contractsApi.update(id!, {
        ...values,
        companyDocument: onlyDigits(values.companyDocument),
        representativeDocument: onlyDigits(values.representativeDocument),
      }),
    onSuccess: async () => {
      await invalidate()
      message.success(t('contracts.saveSuccess'))
    },
  })

  const issueMutation = useMutation({
    mutationFn: () => contractsApi.issue(id!),
    onSuccess: async () => {
      await invalidate()
      message.success(t('contracts.issueSuccess'))
    },
  })

  const signMutation = useMutation({
    mutationFn: () => contractsApi.sign(id!),
    onSuccess: async () => {
      await invalidate()
      message.success(t('contracts.signSuccess'))
    },
  })

  const downloadMutation = useMutation({
    mutationFn: (format: 'pdf' | 'docx') => contractsApi.download(id!, format),
    onError: (err) => {
      message.error(
        err instanceof ApiError ? err.message : t('contracts.downloadFailed'),
      )
    },
  })

  if (query.isLoading) {
    return <Spin tip={t('common.loading')} />
  }

  if (query.isError || !query.data) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('contracts.loadError')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('contracts.loadError')
        }
      />
    )
  }

  const contract = query.data
  const canEdit = contract.status === 'PreIssued' && isPlatformAdmin
  const canIssue = contract.status === 'PreIssued' && isPlatformAdmin
  const canSign = contract.status === 'Issued'
  const canDownload = contract.status === 'Issued' || contract.status === 'Signed'
  const statusLabel =
    contract.status in dict.contracts.status
      ? dict.contracts.status[contract.status as keyof typeof dict.contracts.status]
      : contract.status

  return (
    <div>
      <Button type="link" onClick={() => navigate('/contratos')} style={{ paddingLeft: 0 }}>
        {t('common.back')}
      </Button>
      <Space align="center" wrap style={{ marginBottom: 8 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {t('contracts.detailTitle')} {contract.code}
        </Typography.Title>
        <Tag color={statusColor(contract.status)}>{statusLabel}</Tag>
      </Space>
      <Typography.Paragraph type="secondary">
        {t('contracts.colGeneratedAt')}: {formatDate(contract.generatedAtUtc, locale)}
        {' · '}
        {t('contracts.colFormarcaSign')}:{' '}
        {contract.formarcaSignedAt ? t('contracts.signed') : t('contracts.notSigned')}
        {' · '}
        {t('contracts.colClientSign')}:{' '}
        {contract.clientSignedAt ? t('contracts.signed') : t('contracts.notSigned')}
      </Typography.Paragraph>

      {updateMutation.isError ? (
        <Alert
          type="error"
          showIcon
          message={
            updateMutation.error instanceof ApiError
              ? updateMutation.error.message
              : t('contracts.updateFailed')
          }
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <Card style={{ marginBottom: 16 }}>
        <Form<UpdateContractRequest>
          form={form}
          layout="vertical"
          disabled={!canEdit}
          initialValues={{
            companyDocument: maskTaxId(contract.companyDocument),
            representativeDocument: maskCpf(contract.representativeDocument),
            brandName: contract.brandName,
            classes: parseClasses(contract.classes),
            amount: contract.amount,
          }}
          onFinish={(values) => {
            void updateMutation.mutateAsync(values)
          }}
        >
          <Form.Item
            label={t('contracts.fields.companyDocument')}
            name="companyDocument"
            getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
              maskTaxId(event.target.value)
            }
            rules={[
              { required: true, message: t('validation.required') },
              {
                validator: (_, value: string) =>
                  isValidTaxId(value ?? '')
                    ? Promise.resolve()
                    : Promise.reject(new Error(t('validation.taxId'))),
              },
            ]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label={t('contracts.fields.representativeDocument')}
            name="representativeDocument"
            getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
              maskCpf(event.target.value)
            }
            rules={[
              { required: true, message: t('validation.required') },
              {
                validator: (_, value: string) =>
                  isValidCpf(value ?? '')
                    ? Promise.resolve()
                    : Promise.reject(new Error(t('validation.cpf'))),
              },
            ]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label={t('contracts.fields.brandName')}
            name="brandName"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            label={t('contracts.fields.classes')}
            name="classes"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Select
              mode="multiple"
              options={NICE_CLASSES.map((n) => ({ value: n, label: String(n) }))}
            />
          </Form.Item>
          <Form.Item
            label={t('contracts.fields.amount')}
            name="amount"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} prefix="R$" />
          </Form.Item>
          {canEdit ? (
            <Button
              type="primary"
              htmlType="submit"
              loading={updateMutation.isPending}
            >
              {t('contracts.saveSnapshot')}
            </Button>
          ) : null}
        </Form>
      </Card>

      <Space wrap style={{ marginBottom: 24 }}>
        {canIssue ? (
          <Button
            type="primary"
            loading={issueMutation.isPending}
            onClick={() => issueMutation.mutate()}
          >
            {t('contracts.issue')}
          </Button>
        ) : null}
        {canSign ? (
          <Button
            type="primary"
            loading={signMutation.isPending}
            onClick={() => {
              modal.confirm({
                title: t('contracts.signConfirmTitle'),
                content: t('contracts.signConfirmContent'),
                okText: t('contracts.sign'),
                cancelText: t('common.cancel'),
                onOk: () => signMutation.mutateAsync(),
              })
            }}
          >
            {t('contracts.sign')}
          </Button>
        ) : null}
        {canDownload ? (
          <>
            <Button
              loading={downloadMutation.isPending}
              onClick={() => downloadMutation.mutate('pdf')}
            >
              {t('contracts.downloadPdf')}
            </Button>
            <Button
              loading={downloadMutation.isPending}
              onClick={() => downloadMutation.mutate('docx')}
            >
              {t('contracts.downloadWord')}
            </Button>
          </>
        ) : null}
      </Space>

      <PayInvoicePanel contractId={contract.id} contractStatus={contract.status} />
    </div>
  )
}
