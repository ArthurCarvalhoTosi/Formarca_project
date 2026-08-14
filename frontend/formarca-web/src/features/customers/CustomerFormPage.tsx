import { Alert, App, Button, Empty, Spin, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { mapRegistrationError } from '@/features/registration/api'
import { toFormValues } from '@/features/registration/payload'
import { RegistrationForm } from '@/features/registration/RegistrationForm'
import type { RegistrationPayload } from '@/features/registration/types'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { customersApi } from './api'

export function CustomerFormPage() {
  const { id } = useParams<{ id: string }>()
  const isNew = !id
  const { t } = useI18n()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { activeTenantId } = useAuth()
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: ['customers', id, activeTenantId],
    queryFn: () => customersApi.get(id!, activeTenantId),
    enabled: !isNew,
  })

  const goToList = () => navigate('/cadastro/clientes')

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['customers'] }),
      queryClient.invalidateQueries({ queryKey: ['registration'] }),
      queryClient.invalidateQueries({ queryKey: ['admin', 'tenants'] }),
    ])
  }

  const createMutation = useMutation({
    mutationFn: (values: RegistrationPayload) => customersApi.create(values),
    onSuccess: async () => {
      await invalidate()
      message.success(t('registration.saveSuccess'))
      goToList()
    },
  })

  const updateMutation = useMutation({
    mutationFn: (values: RegistrationPayload) =>
      customersApi.update(id!, values, activeTenantId),
    onSuccess: async () => {
      await invalidate()
      message.success(t('registration.saveSuccess'))
      goToList()
    },
  })

  const mutation = isNew ? createMutation : updateMutation

  if (!isNew && query.isLoading) {
    return <Spin tip={t('common.loading')} />
  }

  if (!isNew && query.isError) {
    const description =
      query.error instanceof ApiError
        ? query.error.message
        : t('registration.errors.loadFailed')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('registration.errors.loadFailed')}
        description={description}
      />
    )
  }

  const current = isNew ? undefined : query.data

  return (
    <div>
      <Button type="link" onClick={goToList} style={{ paddingLeft: 0 }}>
        {t('common.back')}
      </Button>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {isNew ? t('customers.new') : t('registration.panelTitle')}
      </Typography.Title>
      {!isNew ? (
        <Typography.Paragraph type="secondary">
          {t('registration.panelDescription')}
        </Typography.Paragraph>
      ) : null}

      {!isNew && !current ? (
        <Empty
          description={
            <span>
              <strong>{t('registration.emptyTitle')}</strong>
              <br />
              {t('registration.emptyDescription')}
            </span>
          }
        />
      ) : (
        <>
          {mutation.isError ? (
            <Alert
              type="error"
              showIcon
              message={mapRegistrationError(
                mutation.error,
                t(
                  isNew
                    ? 'registration.errors.submitFailed'
                    : 'registration.errors.saveFailed',
                ),
                {
                  email: t('registration.errors.emailTaken'),
                  document: t('registration.errors.documentTaken'),
                },
              )}
              style={{ marginBottom: 16 }}
            />
          ) : null}
          <RegistrationForm
            key={current?.id ?? 'new'}
            initialValues={current ? toFormValues(current) : undefined}
            submitting={mutation.isPending}
            submitLabel={
              isNew ? t('registration.publicSubmit') : t('registration.save')
            }
            onSubmit={async (values) => {
              await mutation.mutateAsync(values)
            }}
          />
        </>
      )}
    </div>
  )
}
