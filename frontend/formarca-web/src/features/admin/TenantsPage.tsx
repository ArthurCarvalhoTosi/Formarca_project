import { useState } from 'react'
import {
  Alert,
  App,
  Button,
  Form,
  Input,
  Modal,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from './api'
import type { Tenant } from './types'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import type { Dictionary } from '@/shared/i18n'

function statusColor(status?: string) {
  switch (status) {
    case 'Pending':
      return 'warning'
    case 'Active':
      return 'success'
    case 'Inactive':
      return 'default'
    case 'Suspended':
      return 'error'
    default:
      return 'default'
  }
}

function statusLabel(status: string | undefined, dict: Dictionary): string {
  if (!status) return '—'
  if (status in dict.status) {
    return dict.status[status as keyof Dictionary['status']]
  }
  return status
}

export function TenantsPage() {
  const { t, dict } = useI18n()
  const { message, modal } = App.useApp()
  const queryClient = useQueryClient()
  const [approving, setApproving] = useState<Tenant | null>(null)
  const [form] = Form.useForm<{ password: string }>()

  const query = useQuery({
    queryKey: ['admin', 'tenants'],
    queryFn: () => adminApi.listTenants(),
  })

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['admin', 'tenants'] })

  const approveMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      adminApi.approveTenant(id, password),
    onSuccess: () => {
      message.success(t('admin.approveSuccess'))
      setApproving(null)
      form.resetFields()
      void invalidate()
    },
  })

  const rejectMutation = useMutation({
    mutationFn: (id: string) => adminApi.rejectTenant(id),
    onSuccess: () => {
      message.success(t('admin.rejectSuccess'))
      void invalidate()
    },
  })

  const confirmReject = (tenant: Tenant) => {
    modal.confirm({
      title: t('admin.rejectConfirmTitle'),
      content: t('admin.rejectConfirmContent'),
      okText: t('admin.reject'),
      okButtonProps: { danger: true },
      cancelText: t('common.cancel'),
      onOk: () => rejectMutation.mutateAsync(tenant.id),
    })
  }

  if (query.isLoading) {
    return <Spin tip={t('admin.tenantsLoading')} />
  }

  if (query.isError) {
    const errorMessage =
      query.error instanceof ApiError
        ? query.error.message
        : t('admin.tenantsLoadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('admin.tenantsUnavailable')}
        description={`${errorMessage} ${t('admin.tenantsLoadHint')}`}
      />
    )
  }

  const data = query.data ?? []

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {t('admin.tenantsTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('admin.tenantsDescription')}
      </Typography.Paragraph>
      <Table<Tenant>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('admin.tenantsEmpty') }}
        columns={[
          { title: t('admin.colName'), dataIndex: 'name' },
          { title: t('admin.colSlug'), dataIndex: 'slug' },
          { title: t('admin.colDocument'), dataIndex: 'document' },
          {
            title: t('admin.colStatus'),
            dataIndex: 'status',
            render: (status: string | undefined) => (
              <Tag color={statusColor(status)}>{statusLabel(status, dict)}</Tag>
            ),
          },
          {
            title: t('common.actions'),
            key: 'actions',
            render: (_: unknown, tenant: Tenant) =>
              tenant.status === 'Pending' ? (
                <Space>
                  <Button
                    type="primary"
                    size="small"
                    onClick={() => {
                      form.resetFields()
                      setApproving(tenant)
                    }}
                  >
                    {t('admin.approve')}
                  </Button>
                  <Button
                    danger
                    size="small"
                    loading={rejectMutation.isPending}
                    onClick={() => confirmReject(tenant)}
                  >
                    {t('admin.reject')}
                  </Button>
                </Space>
              ) : null,
          },
        ]}
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={t('admin.approveTitle')}
        open={Boolean(approving)}
        onCancel={() => {
          setApproving(null)
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
          onFinish={(values) => {
            if (!approving) return
            approveMutation.mutate({
              id: approving.id,
              password: values.password,
            })
          }}
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
    </div>
  )
}
