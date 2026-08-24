import {
  Alert,
  App,
  Button,
  Form,
  Input,
  Select,
  Space,
  Spin,
  Typography,
} from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { contractsApi } from '@/features/contracts/api'
import { customersApi } from '@/features/customers/api'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { brandsApi } from './api'
import { NICE_CLASSES, type BrandPayload } from './types'

export function BrandFormPage() {
  const { id } = useParams<{ id: string }>()
  const isNew = !id
  const [search] = useSearchParams()
  const presetTenantId = search.get('tenantId')
  const { t } = useI18n()
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const { isPlatformAdmin, activeTenantId } = useAuth()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<BrandPayload>()

  const query = useQuery({
    queryKey: ['brands', id],
    queryFn: () => brandsApi.get(id!),
    enabled: !isNew,
  })

  const tenantsQuery = useQuery({
    queryKey: ['customers', true],
    queryFn: () => customersApi.list(true),
    enabled: isPlatformAdmin,
  })

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ['brands'] })
  }

  const saveMutation = useMutation({
    mutationFn: (values: BrandPayload) =>
      isNew ? brandsApi.create(values) : brandsApi.update(id!, values),
    onSuccess: async () => {
      await invalidate()
      message.success(t('brands.saveSuccess'))
      navigate('/marcas')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => brandsApi.remove(id!),
    onSuccess: async () => {
      await invalidate()
      message.success(t('brands.deleteSuccess'))
      navigate('/marcas')
    },
  })

  const preIssueMutation = useMutation({
    mutationFn: () => contractsApi.preIssue({ brandId: id! }),
    onSuccess: async (contract) => {
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      message.success(t('brands.preIssueSuccess'))
      navigate(`/contratos/${contract.id}`)
    },
  })

  if (!isNew && query.isLoading) {
    return <Spin tip={t('common.loading')} />
  }

  if (!isNew && query.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message={t('brands.loadError')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('brands.loadError')
        }
      />
    )
  }

  const current = query.data
  const initialTenantId =
    current?.tenantId ?? presetTenantId ?? activeTenantId ?? undefined

  return (
    <div>
      <Button type="link" onClick={() => navigate('/marcas')} style={{ paddingLeft: 0 }}>
        {t('common.back')}
      </Button>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {isNew ? t('brands.formCreateTitle') : t('brands.formEditTitle')}
      </Typography.Title>

      {saveMutation.isError ? (
        <Alert
          type="error"
          showIcon
          message={
            saveMutation.error instanceof ApiError
              ? saveMutation.error.message
              : t('brands.saveFailed')
          }
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <Form<BrandPayload>
        form={form}
        layout="vertical"
        disabled={!isPlatformAdmin}
        initialValues={{
          name: current?.name,
          servicesDescription: current?.servicesDescription,
          inpiClasses: current?.inpiClasses ?? [],
          tenantId: initialTenantId,
        }}
        onFinish={(values) => {
          void saveMutation.mutateAsync(values)
        }}
      >
        {isPlatformAdmin ? (
          <Form.Item
            label={t('brands.fields.tenant')}
            name="tenantId"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              disabled={Boolean(presetTenantId) && isNew}
              options={(tenantsQuery.data ?? []).map((row) => ({
                value: row.id,
                label: row.name,
              }))}
            />
          </Form.Item>
        ) : null}

        <Form.Item
          label={t('brands.fields.name')}
          name="name"
          rules={[{ required: true, message: t('validation.required') }]}
        >
          <Input placeholder={t('brands.placeholders.name')} />
        </Form.Item>

        <Form.Item
          label={t('brands.fields.inpiClasses')}
          name="inpiClasses"
          rules={[{ required: true, message: t('validation.required') }]}
        >
          <Select
            mode="multiple"
            options={NICE_CLASSES.map((n) => ({ value: n, label: String(n) }))}
            placeholder={t('brands.fields.inpiClasses')}
          />
        </Form.Item>

        <Form.Item
          label={t('brands.fields.servicesDescription')}
          name="servicesDescription"
          rules={[{ required: true, message: t('validation.required') }]}
        >
          <Input.TextArea
            rows={4}
            placeholder={t('brands.placeholders.servicesDescription')}
          />
        </Form.Item>

        <Space wrap>
          {isPlatformAdmin ? (
            <Button
              type="primary"
              htmlType="submit"
              loading={saveMutation.isPending}
            >
              {t('common.save')}
            </Button>
          ) : null}
          {!isNew && isPlatformAdmin ? (
            <Button
              onClick={() => preIssueMutation.mutate()}
              loading={preIssueMutation.isPending}
            >
              {t('brands.preIssue')}
            </Button>
          ) : null}
          {!isNew && isPlatformAdmin ? (
            <Button
              danger
              loading={deleteMutation.isPending}
              onClick={() => {
                modal.confirm({
                  title: t('brands.deleteConfirmTitle'),
                  content: t('brands.deleteConfirmContent'),
                  okText: t('common.delete'),
                  okButtonProps: { danger: true },
                  cancelText: t('common.cancel'),
                  onOk: () => deleteMutation.mutateAsync(),
                })
              }}
            >
              {t('common.delete')}
            </Button>
          ) : null}
        </Space>
      </Form>
    </div>
  )
}
