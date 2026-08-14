import { Alert, App, Empty, Spin, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { mapRegistrationError, registrationApi } from './api'
import { toFormValues } from './payload'
import { RegistrationForm } from './RegistrationForm'
import type { RegistrationPayload } from './types'

export function RegistrationPage() {
  const { t } = useI18n()
  const { message } = App.useApp()
  const { activeTenantId } = useAuth()
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: ['registration', activeTenantId],
    queryFn: () => registrationApi.getCurrent(),
  })

  const mutation = useMutation({
    mutationFn: (values: RegistrationPayload) => registrationApi.update(values),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['registration'] })
      message.success(t('registration.saveSuccess'))
    },
  })

  if (query.isLoading) {
    return <Spin tip={t('common.loading')} />
  }

  if (query.isError) {
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

  const current = query.data

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('registration.panelTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('registration.panelDescription')}
      </Typography.Paragraph>

      {!current ? (
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
                t('registration.errors.saveFailed'),
                {
                  email: t('registration.errors.emailTaken'),
                  document: t('registration.errors.documentTaken'),
                },
              )}
              style={{ marginBottom: 16 }}
            />
          ) : null}
          <RegistrationForm
            key={current.id}
            initialValues={toFormValues(current)}
            submitting={mutation.isPending}
            submitLabel={t('registration.save')}
            onSubmit={async (values) => {
              await mutation.mutateAsync(values)
            }}
          />
        </>
      )}
    </div>
  )
}
