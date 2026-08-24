import { Alert, App, Button, Input, Select, Space, Spin, Table, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { contractsApi } from '@/features/contracts/api'
import { customersApi } from '@/features/customers/api'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { formatClasses } from '@/shared/money'
import { brandsApi } from './api'
import { NICE_CLASSES, type Brand } from './types'

export function BrandsPage() {
  const { t } = useI18n()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { isPlatformAdmin, activeTenantId } = useAuth()
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [inpiClass, setInpiClass] = useState<number | undefined>()
  const [tenantId, setTenantId] = useState<string | undefined>(
    isPlatformAdmin ? undefined : activeTenantId ?? undefined,
  )

  const filters = {
    name: name.trim() || undefined,
    inpiClass,
    tenantId: isPlatformAdmin ? tenantId : undefined,
  }

  const query = useQuery({
    queryKey: ['brands', filters],
    queryFn: () => brandsApi.list(filters),
  })

  const tenantsQuery = useQuery({
    queryKey: ['customers', isPlatformAdmin],
    queryFn: () => customersApi.list(isPlatformAdmin),
    enabled: isPlatformAdmin,
  })

  const preIssueMutation = useMutation({
    mutationFn: (brandId: string) => contractsApi.preIssue({ brandId }),
    onSuccess: async (contract) => {
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      message.success(t('brands.preIssueSuccess'))
      navigate(`/contratos/${contract.id}`)
    },
  })

  const data = query.data ?? []
  const tenantOptions = useMemo(
    () =>
      (tenantsQuery.data ?? []).map((row) => ({ value: row.id, label: row.name })),
    [tenantsQuery.data],
  )

  if (query.isLoading) {
    return <Spin tip={t('brands.loading')} />
  }

  if (query.isError) {
    const description =
      query.error instanceof ApiError ? query.error.message : t('brands.loadError')
    return (
      <Alert
        type="warning"
        showIcon
        message={t('brands.unavailable')}
        description={description}
      />
    )
  }

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('brands.title')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('brands.description')}
      </Typography.Paragraph>

      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          allowClear
          placeholder={t('brands.searchPlaceholder')}
          value={name}
          onChange={(event) => setName(event.target.value)}
          onSearch={setName}
          style={{ width: 260 }}
        />
        <Select
          allowClear
          placeholder={t('brands.filterClass')}
          value={inpiClass}
          onChange={setInpiClass}
          style={{ width: 140 }}
          options={NICE_CLASSES.map((n) => ({ value: n, label: String(n) }))}
        />
        {isPlatformAdmin ? (
          <Select
            allowClear
            placeholder={t('brands.filterTenant')}
            value={tenantId}
            onChange={setTenantId}
            style={{ minWidth: 200 }}
            options={tenantOptions}
            showSearch
            optionFilterProp="label"
          />
        ) : null}
        {isPlatformAdmin ? (
          <Button type="primary" onClick={() => navigate('/marcas/novo')}>
            {t('brands.new')}
          </Button>
        ) : null}
      </Space>

      <Table<Brand>
        rowKey="id"
        dataSource={data}
        locale={{ emptyText: t('brands.empty') }}
        onRow={(record) => ({
          onClick: () => navigate(`/marcas/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          { title: t('brands.colName'), dataIndex: 'name' },
          ...(isPlatformAdmin
            ? [
                {
                  title: t('brands.colTenant'),
                  dataIndex: 'tenantName',
                  render: (value: string | null | undefined) => value || '—',
                },
              ]
            : []),
          {
            title: t('brands.colClasses'),
            dataIndex: 'inpiClasses',
            render: (value: number[]) => formatClasses(value),
          },
          {
            title: t('brands.colDescription'),
            dataIndex: 'servicesDescription',
            ellipsis: true,
          },
          ...(isPlatformAdmin
            ? [
                {
                  title: t('common.actions'),
                  key: 'actions',
                  render: (_: unknown, row: Brand) => (
                    <Button
                      size="small"
                      onClick={(event) => {
                        event.stopPropagation()
                        preIssueMutation.mutate(row.id)
                      }}
                      loading={preIssueMutation.isPending}
                    >
                      {t('brands.preIssue')}
                    </Button>
                  ),
                },
              ]
            : []),
        ]}
        pagination={{ pageSize: 10 }}
      />
    </div>
  )
}
