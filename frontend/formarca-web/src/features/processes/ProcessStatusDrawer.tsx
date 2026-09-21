import { Alert, Button, Drawer, Form, Input, Select, Spin, Timeline, Typography } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { ApiError } from '@/shared/api/types'
import { useI18n } from '@/shared/i18n'
import { trademarkApi } from './trademarkApi'
import { TRADEMARK_PHASES, type TrademarkPhase, type TrademarkStatus } from './trademarkTypes'

type StatusForm = {
  trademarkStatusId: string
  processNumber: string
  note: string
}

type ProcessStatusDrawerProps = {
  processId: string | null
  open: boolean
  canManage: boolean
  onClose: () => void
}

function groupStatuses(statuses: TrademarkStatus[], label: (phase: TrademarkPhase) => string) {
  return TRADEMARK_PHASES.map((phase) => ({
    label: label(phase),
    options: statuses
      .filter((status) => status.phase === phase)
      .map((status) => ({ value: status.id, label: status.title })),
  })).filter((group) => group.options.length > 0)
}

export function ProcessStatusDrawer({ processId, open, canManage, onClose }: ProcessStatusDrawerProps) {
  const { t, dict, locale } = useI18n()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<StatusForm>()

  const detailQuery = useQuery({
    queryKey: ['trademark-detail', processId],
    queryFn: () => trademarkApi.detail(processId!),
    enabled: open && Boolean(processId),
  })

  const statusesQuery = useQuery({
    queryKey: ['trademark-statuses', 'active'],
    queryFn: () => trademarkApi.listStatuses(false),
    enabled: open && canManage,
  })

  useEffect(() => {
    if (!detailQuery.data) return
    form.setFieldsValue({
      trademarkStatusId: detailQuery.data.status?.id,
      processNumber: detailQuery.data.processNumber ?? '',
      note: '',
    })
  }, [detailQuery.data, form])

  const mutation = useMutation({
    mutationFn: (values: StatusForm) =>
      trademarkApi.changeStatus(processId!, {
        trademarkStatusId: values.trademarkStatusId,
        processNumber: values.processNumber,
        note: values.note,
      }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['trademark-dashboard'] }),
        queryClient.invalidateQueries({ queryKey: ['trademark-detail', processId] }),
        queryClient.invalidateQueries({ queryKey: ['processes'] }),
      ])
      form.setFieldValue('note', '')
    },
  })

  const detail = detailQuery.data
  const formatWhen = (value?: string | null) => {
    if (!value) return ''
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
  }

  return (
    <Drawer
      title={detail?.brandName || t('trademark.openDetails')}
      open={open}
      onClose={onClose}
      width={480}
      destroyOnHidden
    >
      {detailQuery.isLoading ? <Spin /> : null}
      {detailQuery.isError ? (
        <Alert
          type="error"
          showIcon
          message={
            detailQuery.error instanceof ApiError ? detailQuery.error.message : t('trademark.loadError')
          }
        />
      ) : null}
      {detail ? (
        <div className="tm-drawer">
          <Typography.Paragraph type="secondary" style={{ marginBottom: 4 }}>
            {detail.tenantName}
            {detail.contractCode ? ` · ${t('trademark.contract')} ${detail.contractCode}` : ''}
          </Typography.Paragraph>
          {detail.status ? (
            <>
              <span className={`tm-chip tm-tone-${detail.status.tone}`}>{detail.status.title}</span>
              <Typography.Paragraph style={{ marginTop: 12 }}>{detail.status.description}</Typography.Paragraph>
            </>
          ) : (
            <Typography.Paragraph>{t('trademark.noStatusHint')}</Typography.Paragraph>
          )}

          <Typography.Title level={5}>{t('trademark.historyTitle')}</Typography.Title>
          {detail.history.length === 0 ? (
            <Typography.Paragraph type="secondary">{t('trademark.historyEmpty')}</Typography.Paragraph>
          ) : (
            <Timeline
              items={detail.history.map((entry) => ({
                color: 'gray',
                content: (
                  <div>
                    <strong>{entry.toStatusTitle}</strong>
                    <div className="tm-history-meta">{formatWhen(entry.occurredAtUtc)}</div>
                    {entry.fromStatusTitle ? (
                      <div className="tm-history-meta">
                        {t('trademark.historyFrom', { title: entry.fromStatusTitle })}
                      </div>
                    ) : null}
                    {entry.note ? <div>{entry.note}</div> : null}
                  </div>
                ),
              }))}
            />
          )}

          {canManage ? (
            <Form
              form={form}
              layout="vertical"
              onFinish={(values) => mutation.mutate(values)}
              style={{ marginTop: 8 }}
            >
              <Typography.Title level={5}>{t('trademark.changeStatus')}</Typography.Title>
              {mutation.isError ? (
                <Alert
                  type="error"
                  showIcon
                  style={{ marginBottom: 12 }}
                  message={
                    mutation.error instanceof ApiError
                      ? mutation.error.message
                      : t('trademark.statusSaveFailed')
                  }
                />
              ) : null}
              {mutation.isSuccess ? (
                <Alert type="success" showIcon style={{ marginBottom: 12 }} message={t('trademark.statusSaved')} />
              ) : null}
              <Form.Item label={t('trademark.processNumber')} name="processNumber">
                <Input maxLength={64} />
              </Form.Item>
              <Form.Item
                label={t('trademark.statusLabel')}
                name="trademarkStatusId"
                rules={[{ required: true, message: t('validation.required') }]}
              >
                <Select
                  showSearch
                  optionFilterProp="label"
                  loading={statusesQuery.isLoading}
                  options={groupStatuses(statusesQuery.data ?? [], (phase) => dict.trademark.phases[phase])}
                />
              </Form.Item>
              <Form.Item label={t('trademark.noteLabel')} name="note">
                <Input.TextArea rows={3} maxLength={1000} placeholder={t('trademark.notePlaceholder')} />
              </Form.Item>
              <Button type="primary" htmlType="submit" loading={mutation.isPending}>
                {t('trademark.saveStatus')}
              </Button>
            </Form>
          ) : null}
        </div>
      ) : null}
    </Drawer>
  )
}
