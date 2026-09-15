import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Form, Input, Typography } from 'antd'
import { Lock, Mail } from 'lucide-react'
import { BrandLogo } from '@/shared/brand/BrandLogo'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { MOCK_CLIENT_CREDENTIALS, MOCK_CREDENTIALS } from '@/shared/auth/mockAuth'
import { env } from '@/shared/config/env'
import { LocaleSwitcher, useI18n } from '@/shared/i18n'

type LoginFormValues = {
  email: string
  password: string
}

export function LoginPage() {
  const { status, login } = useAuth()
  const { t } = useI18n()
  const navigate = useNavigate()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const from =
    (location.state as { from?: { pathname?: string } } | null)?.from
      ?.pathname ?? '/'

  if (status === 'authenticated') {
    return <Navigate to={from} replace />
  }

  const onFinish = async (values: LoginFormValues) => {
    setError(null)
    setSubmitting(true)
    try {
      await login(values)
      navigate(from, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(
          err.status === 401
            ? t('auth.loginFailed')
            : err.message || t('auth.loginFailed'),
        )
      } else if (env.mockAuth) {
        setError(t('auth.loginRetry'))
      } else {
        setError(t('auth.apiUnreachable'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <div className="public-locale">
        <LocaleSwitcher />
      </div>
      <Card className="login-card" variant="borderless">
        <div className="login-brand">
          <BrandLogo height={32} />
          <Typography.Title level={4} className="login-brand-title">
            {t('auth.title')}
          </Typography.Title>
          <Typography.Text type="secondary">{t('auth.subtitle')}</Typography.Text>
        </div>

        {env.mockAuth ? (
          <Alert
            type="info"
            showIcon
            message={t('auth.mockBanner')}
            description={
              <>
                {t('auth.mockHint')}{' '}
                <Typography.Text code>{MOCK_CREDENTIALS.email}</Typography.Text>{' '}
                /{' '}
                <Typography.Text code>
                  {MOCK_CREDENTIALS.password}
                </Typography.Text>
                <br />
                {t('auth.mockClientHint', {
                  email: MOCK_CLIENT_CREDENTIALS.email,
                  password: MOCK_CLIENT_CREDENTIALS.password,
                })}
              </>
            }
            style={{ marginBottom: 16 }}
          />
        ) : null}

        {error ? (
          <Alert
            type="error"
            showIcon
            message={error}
            style={{ marginBottom: 16 }}
          />
        ) : null}

        <Form<LoginFormValues>
          layout="vertical"
          requiredMark={false}
          onFinish={onFinish}
          initialValues={
            env.mockAuth
              ? {
                  email: MOCK_CREDENTIALS.email,
                  password: MOCK_CREDENTIALS.password,
                }
              : { email: '', password: '' }
          }
        >
          <Form.Item
            label={t('auth.email')}
            name="email"
            rules={[
              { required: true, message: t('validation.required') },
              { type: 'email', message: t('validation.email') },
            ]}
          >
            <Input
              size="large"
              prefix={<Mail size={16} />}
              placeholder={t('auth.emailPlaceholder')}
              autoComplete="username"
            />
          </Form.Item>

          <Form.Item
            label={t('auth.password')}
            name="password"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Input.Password
              size="large"
              prefix={<Lock size={16} />}
              placeholder="••••••••"
              autoComplete="current-password"
            />
          </Form.Item>

          <Button
            type="primary"
            htmlType="submit"
            size="large"
            block
            loading={submitting}
          >
            {t('auth.signIn')}
          </Button>
        </Form>

        <Typography.Paragraph
          type="secondary"
          style={{ marginTop: 24, marginBottom: 0, fontSize: 12 }}
        >
          {t('auth.registerPrompt')}{' '}
          <Link to="/register">{t('auth.registerLink')}</Link>
        </Typography.Paragraph>

        <Typography.Paragraph
          type="secondary"
          style={{ marginTop: 8, marginBottom: 0, fontSize: 12 }}
        >
          {t('auth.accessProblems')}
        </Typography.Paragraph>
      </Card>
    </div>
  )
}
