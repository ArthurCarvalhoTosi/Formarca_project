import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Spin,
  Tag,
  Typography,
} from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, type ChangeEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { NICE_CLASSES } from '@/features/brands/types'
import { PayInvoicePanel } from '@/features/invoices/PayInvoicePanel'
import { maskCpf, maskTaxId, maskZipCode } from '@/features/registration/masks'
import {
  isBrazilUf,
  isValidCpf,
  isValidTaxId,
  isValidZipCode,
  onlyDigits,
} from '@/features/registration/validation'
import { ApiError, getApiErrorMessage } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { env } from '@/shared/config/env'
import { BRAZIL_UFS, useI18n } from '@/shared/i18n'
import { formatDate, parseClasses } from '@/shared/money'
import { AutentiqueSignEmbed } from './AutentiqueSignEmbed'
import { contractsApi } from './api'
import { PdfPreview } from './PdfPreview'
import {
  DEFAULT_MINIMUM_TERM,
  hasAutentiqueDocument,
  isAutentiqueRejected,
  type Contract,
  type UpdateContractRequest,
} from './types'

const PREVIEWABLE_STATUSES = new Set(['PreIssued', 'Issued', 'Signed'])

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

function isFormValidationError(error: unknown): boolean {
  return Boolean(error && typeof error === 'object' && 'errorFields' in error)
}

function toUpdatePayload(values: UpdateContractRequest): UpdateContractRequest {
  return {
    ...values,
    companyDocument: onlyDigits(values.companyDocument),
    representativeDocument: onlyDigits(values.representativeDocument),
    zipCode: onlyDigits(values.zipCode ?? ''),
    state: (values.state ?? '').trim().toUpperCase(),
  }
}

export function ContractDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { t, locale, dict } = useI18n()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { isPlatformAdmin, user } = useAuth()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<UpdateContractRequest>()
  const previousStatusRef = useRef<string | undefined>(undefined)
  const hasLoadedStatusRef = useRef(false)

  const query = useQuery({
    queryKey: ['contracts', id],
    queryFn: () => contractsApi.get(id!),
    enabled: Boolean(id),
    refetchInterval: (current) => {
      const data = current.state.data
      if (data?.status !== 'Issued') return false
      if (isAutentiqueRejected(data.autentiqueStatus)) return false
      return 5000
    },
  })

  const canPreview = Boolean(id) && PREVIEWABLE_STATUSES.has(query.data?.status ?? '')
  const canSign =
    query.data?.status === 'Issued' &&
    (isPlatformAdmin || (user?.permissions ?? []).includes('contracts.sign'))
  const isRejected = isAutentiqueRejected(query.data?.autentiqueStatus)
  const hasAutentique = Boolean(query.data && hasAutentiqueDocument(query.data))
  const contractSignUrl = query.data?.autentiqueSignUrl?.trim() || null

  const previewQuery = useQuery({
    queryKey: ['contracts', id, 'preview'],
    queryFn: () => contractsApi.preview(id!),
    enabled: canPreview,
  })

  const signLinkQuery = useQuery({
    queryKey: ['contracts', id, 'sign-link'],
    queryFn: () => contractsApi.getSignLink(id!),
    enabled: Boolean(id) && canSign && hasAutentique && !isRejected && !contractSignUrl,
  })

  useEffect(() => {
    const url = signLinkQuery.data?.signUrl?.trim()
    if (!id || !url) return
    queryClient.setQueryData(['contracts', id], (current: Contract | undefined) => {
      if (!current || current.autentiqueSignUrl?.trim()) return current
      return { ...current, autentiqueSignUrl: url }
    })
  }, [id, queryClient, signLinkQuery.data?.signUrl])

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['contracts'] }),
      queryClient.invalidateQueries({ queryKey: ['invoices'] }),
      queryClient.invalidateQueries({ queryKey: ['processes'] }),
    ])
  }

  useEffect(() => {
    const status = query.data?.status
    if (!status) return
    const becameSigned =
      hasLoadedStatusRef.current &&
      previousStatusRef.current === 'Issued' &&
      status === 'Signed'
    hasLoadedStatusRef.current = true
    previousStatusRef.current = status
    if (!becameSigned) return
    void Promise.all([
      queryClient.invalidateQueries({ queryKey: ['contracts'] }),
      queryClient.invalidateQueries({ queryKey: ['invoices'] }),
      queryClient.invalidateQueries({ queryKey: ['processes'] }),
    ]).then(() => {
      message.success(t('contracts.signSuccess'))
    })
  }, [query.data?.status, queryClient, message, t])

  const updateMutation = useMutation({
    mutationFn: (values: UpdateContractRequest) =>
      contractsApi.update(id!, toUpdatePayload(values)),
    onSuccess: async () => {
      await invalidate()
      message.success(t('contracts.saveSuccess'))
    },
  })

  const issueMutation = useMutation({
    mutationFn: async () => {
      const values = await form.validateFields()
      await contractsApi.update(id!, toUpdatePayload(values))
      return contractsApi.issue(id!)
    },
    onSuccess: async () => {
      await invalidate()
      message.success(t('contracts.issueSuccess'))
    },
    onError: (error) => {
      if (isFormValidationError(error)) return
      message.error(getApiErrorMessage(error, t('contracts.issueFailed')))
    },
  })

  const simulateMutation = useMutation({
    mutationFn: () => contractsApi.simulateAutentiqueSign(id!),
    onSuccess: async () => {
      await invalidate()
    },
    onError: (error) => {
      message.error(getApiErrorMessage(error, t('contracts.signFailed')))
    },
  })

  const downloadMutation = useMutation({
    mutationFn: () => contractsApi.download(id!),
    onError: (error) => {
      message.error(getApiErrorMessage(error, t('contracts.downloadFailed')))
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
  const canDownload = contract.status === 'Issued' || contract.status === 'Signed'
  const embedSignUrl = contractSignUrl || signLinkQuery.data?.signUrl || null
  const statusLabel =
    contract.status in dict.contracts.status
      ? dict.contracts.status[contract.status as keyof typeof dict.contracts.status]
      : contract.status
  const required = { required: true, message: t('validation.required') }

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

      {(updateMutation.isError ||
        (issueMutation.isError && !isFormValidationError(issueMutation.error)) ||
        simulateMutation.isError) ? (
        <Alert
          type="error"
          showIcon
          message={
            updateMutation.isError
              ? getApiErrorMessage(updateMutation.error, t('contracts.updateFailed'))
              : issueMutation.isError
                ? getApiErrorMessage(issueMutation.error, t('contracts.issueFailed'))
                : getApiErrorMessage(simulateMutation.error, t('contracts.signFailed'))
          }
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        {canEdit || isPlatformAdmin ? (
        <Col xs={24} xl={12}>
          <Card>
            <Form<UpdateContractRequest>
              key={contract.id}
              form={form}
              layout="vertical"
              disabled={!canEdit}
              initialValues={{
                companyName: contract.companyName ?? contract.tenantName ?? '',
                companyDocument: maskTaxId(contract.companyDocument),
                representativeDocument: maskCpf(contract.representativeDocument),
                brandName: contract.brandName,
                classes: parseClasses(contract.classes),
                amount: contract.amount,
                street: contract.street ?? '',
                number: contract.number ?? '',
                city: contract.city ?? '',
                state: contract.state ?? '',
                zipCode: maskZipCode(contract.zipCode ?? ''),
                minimumTerm: contract.minimumTerm || DEFAULT_MINIMUM_TERM,
                bodyMarkdown: contract.bodyMarkdown ?? '',
              }}
              onFinish={(values) => {
                void updateMutation.mutateAsync(values)
              }}
            >
              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.companyName')}
                    name="companyName"
                    rules={[required]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.companyDocument')}
                    name="companyDocument"
                    getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
                      maskTaxId(event.target.value)
                    }
                    rules={[
                      required,
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
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.representativeDocument')}
                    name="representativeDocument"
                    getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
                      maskCpf(event.target.value)
                    }
                    rules={[
                      required,
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
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.zipCode')}
                    name="zipCode"
                    getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
                      maskZipCode(event.target.value)
                    }
                    rules={[
                      required,
                      {
                        validator: (_, value: string) =>
                          isValidZipCode(value ?? '')
                            ? Promise.resolve()
                            : Promise.reject(new Error(t('validation.zipCode'))),
                      },
                    ]}
                  >
                    <Input inputMode="numeric" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={16}>
                  <Form.Item
                    label={t('contracts.fields.street')}
                    name="street"
                    rules={[required]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={8}>
                  <Form.Item
                    label={t('contracts.fields.number')}
                    name="number"
                    rules={[required]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.city')}
                    name="city"
                    rules={[required]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.state')}
                    name="state"
                    rules={[
                      required,
                      {
                        validator: (_, value: string) =>
                          isBrazilUf(value ?? '')
                            ? Promise.resolve()
                            : Promise.reject(new Error(t('validation.uf'))),
                      },
                    ]}
                  >
                    <Select
                      showSearch
                      optionFilterProp="label"
                      options={BRAZIL_UFS.map((uf) => ({
                        value: uf,
                        label: `${uf} — ${dict.ufs[uf]}`,
                      }))}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.brandName')}
                    name="brandName"
                    rules={[required]}
                  >
                    <Input />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.classes')}
                    name="classes"
                    rules={[required]}
                  >
                    <Select
                      mode="multiple"
                      options={NICE_CLASSES.map((n) => ({ value: n, label: String(n) }))}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.amount')}
                    name="amount"
                    rules={[required]}
                  >
                    <InputNumber
                      min={0}
                      step={0.01}
                      precision={2}
                      style={{ width: '100%' }}
                      prefix="R$"
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label={t('contracts.fields.minimumTerm')}
                    name="minimumTerm"
                    rules={[required]}
                  >
                    <Input placeholder={DEFAULT_MINIMUM_TERM} />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item
                label={t('contracts.fields.bodyMarkdown')}
                name="bodyMarkdown"
                extra={t('contracts.legalTextHint')}
                rules={[required]}
              >
                <Input.TextArea
                  rows={16}
                  style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}
                />
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
        </Col>
        ) : null}
        <Col xs={24} xl={canEdit || isPlatformAdmin ? 12 : 24}>
          {canPreview ? (
            <Card title={t('contracts.previewTitle')} style={{ marginBottom: 16 }}>
              <PdfPreview
                blob={previewQuery.data}
                isLoading={previewQuery.isLoading}
                isError={previewQuery.isError}
                errorMessage={
                  previewQuery.error instanceof ApiError
                    ? previewQuery.error.message
                    : t('contracts.previewFailed')
                }
                title={t('contracts.previewTitle')}
              />
            </Card>
          ) : null}
          {canSign && hasAutentique ? (
            <Card title={t('contracts.contractorSignLabel')}>
              <AutentiqueSignEmbed
                signUrl={embedSignUrl}
                isLoadingLink={signLinkQuery.isLoading}
                linkErrorMessage={
                  signLinkQuery.isError
                    ? getApiErrorMessage(
                        signLinkQuery.error,
                        t('contracts.autentiqueSignLinkFailed'),
                      )
                    : null
                }
                isRejected={isRejected}
                showSimulate={env.mockAuth && !isRejected}
                simulatePending={simulateMutation.isPending}
                onSimulate={() => simulateMutation.mutate()}
              />
            </Card>
          ) : null}
          {canSign && !hasAutentique ? (
            <Card title={t('contracts.contractorSignLabel')}>
              <Alert type="warning" showIcon message={t('contracts.autentiqueLegacyUnavailable')} />
            </Card>
          ) : null}
        </Col>
      </Row>

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
        {canDownload ? (
          <Button
            loading={downloadMutation.isPending}
            onClick={() => downloadMutation.mutate()}
          >
            {t('contracts.downloadPdf')}
          </Button>
        ) : null}
      </Space>

      <PayInvoicePanel contractId={contract.id} contractStatus={contract.status} />
    </div>
  )
}
