import { useState, type MouseEvent } from 'react'
import { Alert, App, Button, Form, Input, Modal, Space } from 'antd'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { adminApi } from './api'

type TenantRef = {
  id: string
  status?: string
}

type TenantApprovalControlsProps = {
  tenant: TenantRef
  stopRowClick?: boolean
}

export function TenantApprovalControls({
  tenant,
  stopRowClick = false,
}: TenantApprovalControlsProps) {
  const { t } = useI18n()
  const { message, modal } = App.useApp()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [form] = Form.useForm<{ password: string }>()

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['admin', 'tenants'] }),
      queryClient.invalidateQueries({ queryKey: ['customers'] }),
    ])
  }

  const approveMutation = useMutation({
    mutationFn: (password: string) => adminApi.approveTenant(tenant.id, password),
    onSuccess: async () => {
      message.success(t('admin.approveSuccess'))
      setOpen(false)
      form.resetFields()
      await invalidate()
    },
  })

  const rejectMutation = useMutation({
    mutationFn: () => adminApi.rejectTenant(tenant.id),
    onSuccess: async () => {
      message.success(t('admin.rejectSuccess'))
      await invalidate()
    },
  })

  if (tenant.status !== 'Pending') {
    return null
  }

  const guardClick = stopRowClick
    ? (event: MouseEvent) => event.stopPropagation()
    : undefined

  return (
    <Space onClick={guardClick}>
      <Button
        type="primary"
        size="small"
        onClick={() => {
          form.resetFields()
          approveMutation.reset()
          setOpen(true)
        }}
      >
        {t('admin.approve')}
      </Button>
      <Button
        danger
        size="small"
        loading={rejectMutation.isPending}
        onClick={() => {
          modal.confirm({
            title: t('admin.rejectConfirmTitle'),
            content: t('admin.rejectConfirmContent'),
            okText: t('admin.reject'),
            okButtonProps: { danger: true },
            cancelText: t('common.cancel'),
            onOk: () => rejectMutation.mutateAsync(),
          })
        }}
      >
        {t('admin.reject')}
      </Button>

      <Modal
        title={t('admin.approveTitle')}
        open={open}
        onCancel={() => {
          setOpen(false)
          form.resetFields()
        }}
        onOk={() => form.submit()}
        confirmLoading={approveMutation.isPending}
        okText={t('admin.approve')}
        cancelText={t('common.cancel')}
        destroyOnHidden
      >
        {approveMutation.isError ? (
          <Alert
            type="error"
            showIcon
            message={
              approveMutation.error instanceof ApiError
                ? approveMutation.error.message
                : t('admin.approveFailed')
            }
            style={{ marginBottom: 16 }}
          />
        ) : null}
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) => approveMutation.mutate(values.password)}
        >
          <Form.Item
            label={t('admin.approvePassword')}
            name="password"
            rules={[
              { required: true, message: t('validation.required') },
              { min: 8, message: t('validation.passwordMin') },
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  )
}
