import {
  Alert,
  App,
  Button,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
} from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, getApiErrorMessage } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { trademarkApi } from '@/features/processes/trademarkApi'
import {
  TRADEMARK_PHASES,
  TRADEMARK_TONES,
  type TrademarkPhase,
  type TrademarkStatus,
  type TrademarkStatusPayload,
  type TrademarkTone,
} from '@/features/processes/trademarkTypes'

type StatusForm = TrademarkStatusPayload

export function TrademarkStatusesPage() {
  const { t, dict } = useI18n()
  const { message, modal } = App.useApp()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<StatusForm>()
  const [editing, setEditing] = useState<TrademarkStatus | null>(null)
  const [open, setOpen] = useState(false)

  const query = useQuery({
    queryKey: ['trademark-statuses', 'all'],
    queryFn: () => trademarkApi.listStatuses(true),
  })

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['trademark-statuses'] }),
      queryClient.invalidateQueries({ queryKey: ['trademark-dashboard'] }),
    ])
  }

  const saveMutation = useMutation({
    mutationFn: (values: StatusForm) =>
      editing ? trademarkApi.update(editing.id, values) : trademarkApi.create(values),
    onSuccess: async () => {
      await invalidate()
      message.success(t('trademark.saved'))
      setOpen(false)
    },
  })

  const reorderMutation = useMutation({
    mutationFn: (orderedIds: string[]) => trademarkApi.reorder(orderedIds),
    onSuccess: async () => {
      await invalidate()
    },
    onError: (error) => {
      message.error(getApiErrorMessage(error, t('trademark.reorderFailed')))
    },
  })

  const rows = query.data ?? []

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({
      title: '',
      description: '',
      phase: 'Filing',
      tone: 'Progress',
      isActive: true,
      sortOrder: (rows.at(-1)?.sortOrder ?? 0) + 10,
    })
    setOpen(true)
  }

  const openEdit = (status: TrademarkStatus) => {
    setEditing(status)
    form.setFieldsValue({
      title: status.title,
      description: status.description,
      phase: status.phase,
      tone: status.tone,
      isActive: status.isActive,
      sortOrder: status.sortOrder,
    })
    setOpen(true)
  }

  const move = (index: number, direction: -1 | 1) => {
    const next = rows.map((row) => row.id)
    const target = index + direction
    if (target < 0 || target >= next.length) return
    const [id] = next.splice(index, 1)
    next.splice(target, 0, id)
    reorderMutation.mutate(next)
  }

  const remove = (status: TrademarkStatus) => {
    modal.confirm({
      title: t('common.confirmDelete'),
      content: status.title,
      okText: t('common.delete'),
      cancelText: t('common.cancel'),
      okButtonProps: { danger: true },
      onOk: async () => {
        try {
          await trademarkApi.remove(status.id)
          await invalidate()
          message.success(t('trademark.deleted'))
        } catch (error) {
          message.error(getApiErrorMessage(error, t('trademark.deleteFailed')))
          throw error
        }
      },
    })
  }

  return (
    <div>
      <div className="tm-admin-head">
        <div>
          <Typography.Title level={4} style={{ marginTop: 0 }}>
            {t('trademark.adminTitle')}
          </Typography.Title>
          <Typography.Paragraph type="secondary">{t('trademark.adminDescription')}</Typography.Paragraph>
        </div>
        <Button type="primary" onClick={openCreate}>
          {t('trademark.add')}
        </Button>
      </div>

      {query.isError ? (
        <Alert
          type="error"
          showIcon
          message={query.error instanceof ApiError ? query.error.message : t('trademark.loadCatalogError')}
        />
      ) : (
        <Table<TrademarkStatus>
          rowKey="id"
          loading={query.isLoading}
          dataSource={rows}
          pagination={false}
          scroll={{ x: 880 }}
          columns={[
            {
              title: t('trademark.colOrder'),
              dataIndex: 'sortOrder',
              width: 110,
              render: (_value, _row, index) => (
                <Space size={4}>
                  <Button
                    size="small"
                    aria-label={t('trademark.moveUp')}
                    disabled={index === 0 || reorderMutation.isPending}
                    onClick={() => move(index, -1)}
                  >
                    ↑
                  </Button>
                  <Button
                    size="small"
                    aria-label={t('trademark.moveDown')}
                    disabled={index === rows.length - 1 || reorderMutation.isPending}
                    onClick={() => move(index, 1)}
                  >
                    ↓
                  </Button>
                </Space>
              ),
            },
            {
              title: t('trademark.colTitle'),
              dataIndex: 'title',
              render: (title: string, row) => (
                <div>
                  <strong>{title}</strong>
                  <div className="tm-admin-desc">{row.description}</div>
                </div>
              ),
            },
            {
              title: t('trademark.colPhase'),
              dataIndex: 'phase',
              width: 140,
              render: (phase: TrademarkPhase) => dict.trademark.phases[phase] ?? phase,
            },
            {
              title: t('trademark.colTone'),
              dataIndex: 'tone',
              width: 150,
              render: (tone: TrademarkTone) => (
                <span className={`tm-chip tm-tone-${tone}`}>{dict.trademark.tones[tone] ?? tone}</span>
              ),
            },
            {
              title: t('trademark.colActive'),
              dataIndex: 'isActive',
              width: 100,
              render: (active: boolean) => (
                <Tag color={active ? 'success' : 'default'}>
                  {active ? t('trademark.active') : t('trademark.inactive')}
                </Tag>
              ),
            },
            {
              title: t('common.actions'),
              width: 160,
              render: (_value, row) => (
                <Space>
                  <Button size="small" onClick={() => openEdit(row)}>
                    {t('common.edit')}
                  </Button>
                  <Button size="small" danger onClick={() => remove(row)}>
                    {t('common.delete')}
                  </Button>
                </Space>
              ),
            },
          ]}
        />
      )}

      <Modal
        title={editing ? t('trademark.editTitle') : t('trademark.createTitle')}
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        okText={t('common.save')}
        cancelText={t('common.cancel')}
        confirmLoading={saveMutation.isPending}
        destroyOnHidden
      >
        {saveMutation.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 12 }}
            message={getApiErrorMessage(saveMutation.error, t('trademark.saveFailed'))}
          />
        ) : null}
        <Form form={form} layout="vertical" onFinish={(values) => saveMutation.mutate(values)}>
          <Form.Item
            label={t('trademark.fieldTitle')}
            name="title"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Input maxLength={200} />
          </Form.Item>
          <Form.Item
            label={t('trademark.fieldDescription')}
            name="description"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Input.TextArea rows={4} maxLength={2000} />
          </Form.Item>
          <Form.Item
            label={t('trademark.fieldPhase')}
            name="phase"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Select
              options={TRADEMARK_PHASES.map((phase) => ({
                value: phase,
                label: dict.trademark.phases[phase],
              }))}
            />
          </Form.Item>
          <Form.Item
            label={t('trademark.fieldTone')}
            name="tone"
            rules={[{ required: true, message: t('validation.required') }]}
          >
            <Select
              options={TRADEMARK_TONES.map((tone) => ({
                value: tone,
                label: dict.trademark.tones[tone],
              }))}
            />
          </Form.Item>
          <Form.Item label={t('trademark.fieldOrder')} name="sortOrder">
            <InputNumber min={0} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item label={t('trademark.fieldActive')} name="isActive" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
