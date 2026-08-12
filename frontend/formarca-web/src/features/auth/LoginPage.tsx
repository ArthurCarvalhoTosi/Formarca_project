import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Form, Input, Typography } from 'antd'
import { Lock, Mail } from 'lucide-react'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'

type LoginFormValues = {
  email: string
  password: string
}

export function LoginPage() {
  const { status, login } = useAuth()
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
        setError(err.message || 'Falha no login')
      } else {
        setError('Não foi possível conectar à API. Verifique VITE_API_URL.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <Card className="login-card" variant="borderless">
        <div className="login-brand">
          <Typography.Title level={2} style={{ marginBottom: 4 }}>
            Formarca
          </Typography.Title>
          <Typography.Text type="secondary">
            Painel do Cliente — acesse com suas credenciais
          </Typography.Text>
        </div>

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
          initialValues={{ email: '', password: '' }}
        >
          <Form.Item
            label="E-mail"
            name="email"
            rules={[
              { required: true, message: 'Informe o e-mail' },
              { type: 'email', message: 'E-mail inválido' },
            ]}
          >
            <Input
              size="large"
              prefix={<Mail size={16} />}
              placeholder="voce@empresa.com"
              autoComplete="username"
            />
          </Form.Item>

          <Form.Item
            label="Senha"
            name="password"
            rules={[{ required: true, message: 'Informe a senha' }]}
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
            Entrar
          </Button>
        </Form>

        <Typography.Paragraph
          type="secondary"
          style={{ marginTop: 24, marginBottom: 0, fontSize: 12 }}
        >
          Problemas de acesso? Contate o administrador Formarca.{' '}
          <Link to="/">Voltar</Link>
        </Typography.Paragraph>
      </Card>
    </div>
  )
}
