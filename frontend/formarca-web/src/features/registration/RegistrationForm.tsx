import { App, Button, Card, Col, Form, Input, InputNumber, Row, Select } from 'antd'
import { useRef, type ChangeEvent } from 'react'
import { BRAZIL_UFS, useI18n } from '@/shared/i18n'
import { maskPhone, maskTaxId, maskZipCode } from './masks'
import type { RegistrationPayload } from './types'
import {
  isBrazilUf,
  isValidBrPhone,
  isValidTaxId,
  isValidZipCode,
  onlyDigits,
} from './validation'
import { lookupZipCode } from './viaCep'

type RegistrationFormProps = {
  initialValues?: Partial<RegistrationPayload>
  submitting?: boolean
  disabled?: boolean
  submitLabel: string
  onSubmit: (values: RegistrationPayload) => Promise<void>
}

export function RegistrationForm({
  initialValues,
  submitting = false,
  disabled = false,
  submitLabel,
  onSubmit,
}: RegistrationFormProps) {
  const { t, dict } = useI18n()
  const { message } = App.useApp()
  const [form] = Form.useForm<RegistrationPayload>()
  const representativeDocumentEdited = useRef(
    onlyDigits(initialValues?.legalRepresentativeDocument ?? '') !== '' &&
      onlyDigits(initialValues?.legalRepresentativeDocument ?? '') !==
        onlyDigits(initialValues?.document ?? ''),
  )

  const required = { required: true, message: t('validation.required') }

  const syncRepresentativeDocument = (document: string) => {
    if (representativeDocumentEdited.current) return
    form.setFieldValue('legalRepresentativeDocument', document)
  }

  const fillAddressFromZip = async (zipCode: string) => {
    if (!isValidZipCode(zipCode)) return
    try {
      const result = await lookupZipCode(zipCode)
      if (!result) {
        message.warning(t('registration.viaCepFailed'))
        return
      }
      form.setFieldValue(['address', 'street'], result.street)
      form.setFieldValue(['address', 'neighborhood'], result.neighborhood)
      form.setFieldValue(['address', 'city'], result.city)
      form.setFieldValue(['address', 'state'], result.state)
    } catch {
      message.warning(t('registration.viaCepFailed'))
    }
  }

  return (
    <Form<RegistrationPayload>
      form={form}
      layout="vertical"
      requiredMark
      disabled={disabled || submitting}
      initialValues={initialValues}
      onFinish={(values) => {
        void onSubmit(values)
      }}
    >
      <Card title={t('registration.sections.personal')} style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.name')}
              name="name"
              rules={[required]}
            >
              <Input
                placeholder={t('registration.placeholders.name')}
                autoComplete="organization"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.email')}
              name="email"
              rules={[required, { type: 'email', message: t('validation.email') }]}
            >
              <Input
                placeholder={t('registration.placeholders.email')}
                autoComplete="email"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.phone')}
              name="phone"
              getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) =>
                maskPhone(event.target.value)
              }
              rules={[
                required,
                {
                  validator: (_, value: string) =>
                    isValidBrPhone(value ?? '')
                      ? Promise.resolve()
                      : Promise.reject(new Error(t('validation.phone'))),
                },
              ]}
            >
              <Input
                placeholder={t('registration.placeholders.phone')}
                inputMode="tel"
                autoComplete="tel"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.taxId')}
              name="document"
              getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) => {
                const document = maskTaxId(event.target.value)
                syncRepresentativeDocument(document)
                return document
              }}
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
              <Input
                placeholder={t('registration.placeholders.taxId')}
                inputMode="numeric"
              />
            </Form.Item>
          </Col>
        </Row>
      </Card>

      <Card title={t('registration.sections.address')} style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col xs={24} md={8}>
            <Form.Item
              label={t('registration.fields.zipCode')}
              name={['address', 'zipCode']}
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
              <Input
                placeholder={t('registration.placeholders.zipCode')}
                inputMode="numeric"
                onBlur={(event) => {
                  void fillAddressFromZip(event.target.value)
                }}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={16}>
            <Form.Item
              label={t('registration.fields.street')}
              name={['address', 'street']}
              rules={[required]}
            >
              <Input
                placeholder={t('registration.placeholders.street')}
                autoComplete="address-line1"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item
              label={t('registration.fields.number')}
              name={['address', 'number']}
              rules={[required]}
            >
              <Input placeholder={t('registration.placeholders.number')} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item
              label={t('registration.fields.complement')}
              name={['address', 'complement']}
            >
              <Input
                placeholder={t('registration.placeholders.complement')}
                autoComplete="address-line2"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item
              label={t('registration.fields.neighborhood')}
              name={['address', 'neighborhood']}
              rules={[required]}
            >
              <Input
                placeholder={t('registration.placeholders.neighborhood')}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.city')}
              name={['address', 'city']}
              rules={[required]}
            >
              <Input
                placeholder={t('registration.placeholders.city')}
                autoComplete="address-level2"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.state')}
              name={['address', 'state']}
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
        </Row>
      </Card>

      <Card title={t('registration.sections.commercial')} style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.legalRepresentativeName')}
              name="legalRepresentativeName"
              rules={[required]}
            >
              <Input
                placeholder={t('registration.placeholders.legalRepresentativeName')}
                autoComplete="name"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.legalRepresentativeDocument')}
              name="legalRepresentativeDocument"
              getValueFromEvent={(event: ChangeEvent<HTMLInputElement>) => {
                const document = maskTaxId(event.target.value)
                const digits = onlyDigits(document)
                representativeDocumentEdited.current =
                  digits !== '' &&
                  digits !== onlyDigits(form.getFieldValue('document') ?? '')
                return document
              }}
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
              <Input
                placeholder={t(
                  'registration.placeholders.legalRepresentativeDocument',
                )}
                inputMode="numeric"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              label={t('registration.fields.quotedAmount')}
              name="quotedAmount"
            >
              <InputNumber
                min={0}
                step={0.01}
                precision={2}
                style={{ width: '100%' }}
                prefix="R$"
                placeholder={t('registration.placeholders.quotedAmount')}
              />
            </Form.Item>
          </Col>
          <Col xs={24}>
            <Form.Item
              label={t('registration.fields.budgetNotes')}
              name="budgetNotes"
            >
              <Input.TextArea
                rows={3}
                placeholder={t('registration.placeholders.budgetNotes')}
              />
            </Form.Item>
          </Col>
        </Row>
      </Card>

      <Button type="primary" htmlType="submit" size="large" loading={submitting}>
        {submitLabel}
      </Button>
    </Form>
  )
}
