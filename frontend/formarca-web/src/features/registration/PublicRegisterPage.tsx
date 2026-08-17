import { useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { Alert, Button, Result, Spin, Typography } from 'antd'
import { BrandLogo } from '@/shared/brand/BrandLogo'
import { useAuth } from '@/shared/auth/AuthContext'
import { env } from '@/shared/config/env'
import { LocaleSwitcher, useI18n } from '@/shared/i18n'
import { mapRegistrationError, registrationApi } from './api'
import { RegistrationForm } from './RegistrationForm'
import type { RegistrationPayload } from './types'

export function PublicRegisterPage() {
  const { status } = useAuth()
  const { t } = useI18n()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  if (status === 'loading') {
    return (
      <div className="register-page">
        <Spin size="large" tip={t('session.loading')} />
      </div>
    )
  }

  if (status === 'authenticated') {
    return <Navigate to="/cadastro/clientes" replace />
  }

  const onSubmit = async (values: RegistrationPayload) => {
    setError(null)
    setSubmitting(true)
    try {
      await registrationApi.submit(values)
      setSubmitted(true)
    } catch (err) {
      setError(
        mapRegistrationError(err, t('registration.errors.submitFailed'), {
          email: t('registration.errors.emailTaken'),
          document: t('registration.errors.documentTaken'),
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="register-page">
      <div className="public-locale">
        <LocaleSwitcher />
      </div>
      <div className="register-shell">
        <div className="register-brand">
          <BrandLogo height={32} />
          <Typography.Title level={3} className="login-brand-title">
            {t('registration.publicTitle')}
          </Typography.Title>
          <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
            {t('registration.publicSubtitle')}
          </Typography.Paragraph>
        </div>

        {env.mockAuth ? (
          <Alert
            type="info"
            showIcon
            message={t('customers.mockHint')}
            style={{ marginBottom: 16 }}
          />
        ) : null}

        {submitted ? (
          <Result
            status="success"
            title={t('registration.publicSuccessTitle')}
            subTitle={t('registration.publicSuccessDescription')}
            extra={
              <Link to="/login">
                <Button type="primary">{t('registration.goToLogin')}</Button>
              </Link>
            }
          />
        ) : (
          <>
            {error ? (
              <Alert
                type="error"
                showIcon
                message={error}
                style={{ marginBottom: 16 }}
              />
            ) : null}
            <RegistrationForm
              submitting={submitting}
              submitLabel={t('registration.publicSubmit')}
              onSubmit={onSubmit}
            />
            <Typography.Paragraph
              type="secondary"
              style={{ marginTop: 24, marginBottom: 0 }}
            >
              <Link to="/login">{t('auth.signIn')}</Link>
            </Typography.Paragraph>
          </>
        )}
      </div>
    </div>
  )
}
