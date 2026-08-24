import { Alert, App, Button, Col, Form, InputNumber, Row, Spin, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { invoicesApi } from './api'
import { INSTALLMENT_NUMBERS, type InvoiceSettings } from './types'

type SettingsForm = {
  pixDiscount: number
  generalDiscount: number
  cardCash: number
} & Record<`n${(typeof INSTALLMENT_NUMBERS)[number]}`, number>

function toFormValues(settings: InvoiceSettings): SettingsForm {
  const values = {
    pixDiscount: settings.pixDiscount,
    generalDiscount: settings.generalDiscount,
    cardCash: settings.interestRates.cardCash,
  } as SettingsForm
  for (const n of INSTALLMENT_NUMBERS) {
    values[`n${n}`] = settings.interestRates[`${n}`]
  }
  return values
}

function toPayload(values: SettingsForm): InvoiceSettings {
  return {
    pixDiscount: values.pixDiscount,
    generalDiscount: values.generalDiscount,
    interestRates: {
      '1': values.n1,
      '2': values.n2,
      '3': values.n3,
      '4': values.n4,
      '5': values.n5,
      '6': values.n6,
      '7': values.n7,
      '8': values.n8,
      '9': values.n9,
      '10': values.n10,
      '11': values.n11,
      '12': values.n12,
      cardCash: values.cardCash,
    },
  }
}

export function InvoiceSettingsPage() {
  const { t } = useI18n()
  const { message } = App.useApp()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<SettingsForm>()

  const query = useQuery({
    queryKey: ['invoice-settings'],
    queryFn: () => invoicesApi.getSettings(),
  })

  const mutation = useMutation({
    mutationFn: (values: SettingsForm) => invoicesApi.updateSettings(toPayload(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['invoice-settings'] })
      message.success(t('admin.invoiceSettingsSaved'))
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
        message={t('admin.invoiceSettingsLoadError')}
        description={
          query.error instanceof ApiError
            ? query.error.message
            : t('admin.invoiceSettingsLoadError')
        }
      />
    )
  }

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {t('admin.invoiceSettingsTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('admin.invoiceSettingsDescription')}
      </Typography.Paragraph>

      {mutation.isError ? (
        <Alert
          type="error"
          showIcon
          message={
            mutation.error instanceof ApiError
              ? mutation.error.message
              : t('admin.invoiceSettingsLoadError')
          }
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <Form<SettingsForm>
        form={form}
        layout="vertical"
        initialValues={toFormValues(query.data)}
        onFinish={(values) => {
          void mutation.mutateAsync(values)
        }}
      >
        <Row gutter={16}>
          {INSTALLMENT_NUMBERS.map((n) => (
            <Col xs={12} md={8} lg={6} key={n}>
              <Form.Item
                label={t('admin.interestN', { n })}
                name={`n${n}`}
                rules={[{ required: true, message: t('validation.required') }]}
              >
                <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          ))}
          <Col xs={12} md={8} lg={6}>
            <Form.Item
              label={t('admin.cardCashInterest')}
              name="cardCash"
              rules={[{ required: true, message: t('validation.required') }]}
            >
              <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={12} md={8} lg={6}>
            <Form.Item
              label={t('admin.pixDiscount')}
              name="pixDiscount"
              rules={[{ required: true, message: t('validation.required') }]}
            >
              <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={12} md={8} lg={6}>
            <Form.Item
              label={t('admin.generalDiscount')}
              name="generalDiscount"
              rules={[{ required: true, message: t('validation.required') }]}
            >
              <InputNumber min={0} step={0.01} precision={2} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Button type="primary" htmlType="submit" loading={mutation.isPending}>
          {t('common.save')}
        </Button>
      </Form>
    </div>
  )
}
