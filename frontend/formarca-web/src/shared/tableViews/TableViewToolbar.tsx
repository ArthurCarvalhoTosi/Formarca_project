import { useEffect, useState } from 'react'
import {
  App,
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Popover,
  Space,
  Tooltip,
  Typography,
} from 'antd'
import { Check, LayoutList, Pencil } from 'lucide-react'
import { useI18n } from '@/shared/i18n'
import type { TableViewController } from './useTableViews'
import { columnLabel, nextVisibleKeys } from './visibility'

type TableViewToolbarProps<T> = {
  controller: TableViewController<T>
}

export function TableViewToolbar<T>({ controller }: TableViewToolbarProps<T>) {
  const { t } = useI18n()
  const { modal } = App.useApp()
  const [editorOpen, setEditorOpen] = useState(false)
  const [editorName, setEditorName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [form] = Form.useForm<{ name: string }>()

  const {
    views,
    activeView,
    catalog,
    visibleKeys,
    isDirty,
    loading,
    mutating,
    selectView,
    toggleColumn,
    save,
    saveAs,
    rename,
    remove,
  } = controller

  const hasSavedView = Boolean(activeView)
  const busy = loading || mutating || submitting
  const triggerLabel = activeView?.name ?? t('tableViews.default')

  useEffect(() => {
    if (editorOpen) {
      form.setFieldsValue({ name: editorName })
    } else {
      form.resetFields()
    }
  }, [editorName, editorOpen, form])

  const openEditor = (id: string | null) => {
    selectView(id)
    setEditorName(id ? (views.find((view) => view.id === id)?.name ?? '') : '')
    setEditorOpen(true)
  }

  const confirmDelete = () => {
    modal.confirm({
      title: t('tableViews.deleteConfirmTitle'),
      content: t('tableViews.deleteConfirmContent'),
      okText: t('common.delete'),
      okButtonProps: { danger: true },
      cancelText: t('common.cancel'),
      onOk: async () => {
        await remove()
        setEditorOpen(false)
      },
    })
  }

  const submitSave = async () => {
    if (!activeView) return
    const values = await form.validateFields()
    const name = values.name.trim()
    setSubmitting(true)
    try {
      if (name && name !== activeView.name) {
        await rename(name)
      }
      if (isDirty) {
        await save()
      }
      setEditorOpen(false)
    } catch {
      // Error toast is handled in the hook.
    } finally {
      setSubmitting(false)
    }
  }

  const submitSaveAs = async () => {
    const values = await form.validateFields()
    const name = values.name.trim()
    if (!name) return
    setSubmitting(true)
    try {
      await saveAs(name)
      setEditorOpen(false)
    } catch {
      // Error toast is handled in the hook.
    } finally {
      setSubmitting(false)
    }
  }

  const picker = (
    <div className="table-views-menu">
      <ViewRow
        label={t('tableViews.default')}
        active={!activeView}
        onSelect={() => selectView(null)}
        onEdit={() => openEditor(null)}
        editLabel={t('tableViews.editView')}
      />
      {views.map((view) => (
        <ViewRow
          key={view.id}
          label={view.name}
          active={activeView?.id === view.id}
          onSelect={() => selectView(view.id)}
          onEdit={() => openEditor(view.id)}
          editLabel={t('tableViews.editView')}
        />
      ))}
    </div>
  )

  return (
    <>
      <Popover trigger="click" placement="bottomRight" content={picker}>
        <Tooltip title={t('tableViews.selectView')}>
          <Button
            icon={<LayoutList size={16} />}
            loading={loading}
            aria-label={t('tableViews.selectView')}
          >
            {t('tableViews.title')}
            <Typography.Text type="secondary" style={{ marginLeft: 4 }}>
              {triggerLabel}
            </Typography.Text>
          </Button>
        </Tooltip>
      </Popover>

      <Modal
        open={editorOpen}
        title={t('tableViews.editView')}
        onCancel={() => setEditorOpen(false)}
        destroyOnHidden
        footer={
          <Space wrap style={{ width: '100%', justifyContent: 'flex-end' }}>
            {hasSavedView ? (
              <Button danger disabled={busy} onClick={confirmDelete}>
                {t('common.delete')}
              </Button>
            ) : null}
            <Button
              disabled={busy || (!hasSavedView && !isDirty)}
              onClick={() => {
                void submitSaveAs()
              }}
            >
              {t('tableViews.saveAs')}
            </Button>
            {hasSavedView ? (
              <Button
                type="primary"
                disabled={busy || (!isDirty && editorName.trim() === (activeView?.name ?? ''))}
                loading={submitting}
                onClick={() => {
                  void submitSave()
                }}
              >
                {t('common.save')}
              </Button>
            ) : null}
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label={t('tableViews.nameLabel')}
            rules={
              hasSavedView
                ? [
                    {
                      required: true,
                      whitespace: true,
                      message: t('tableViews.nameRequired'),
                    },
                  ]
                : [
                    {
                      required: true,
                      whitespace: true,
                      message: t('tableViews.nameRequired'),
                    },
                  ]
            }
          >
            <Input
              placeholder={t('tableViews.namePlaceholder')}
              maxLength={80}
              onChange={(event) => setEditorName(event.target.value)}
            />
          </Form.Item>
        </Form>
        <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
          {t('tableViews.visibleColumns')}
        </Typography.Paragraph>
        <div className="table-views-columns">
          {catalog.map((column) => {
            const checked = visibleKeys.includes(column.key)
            const locked =
              column.hideable === false ||
              (checked && nextVisibleKeys(visibleKeys, column.key, false, catalog) == null)

            return (
              <Checkbox
                key={column.key}
                checked={checked}
                disabled={locked}
                onChange={(event) => toggleColumn(column.key, event.target.checked)}
              >
                {columnLabel(column)}
              </Checkbox>
            )
          })}
        </div>
        {!hasSavedView && isDirty ? (
          <Typography.Paragraph type="secondary" style={{ marginTop: 12, marginBottom: 0 }}>
            {t('tableViews.saveAsHint')}
          </Typography.Paragraph>
        ) : null}
      </Modal>
    </>
  )
}

function ViewRow({
  label,
  active,
  onSelect,
  onEdit,
  editLabel,
}: {
  label: string
  active: boolean
  onSelect: () => void
  onEdit: () => void
  editLabel: string
}) {
  return (
    <div className={`table-views-row${active ? ' is-active' : ''}`}>
      <button type="button" className="table-views-row-select" onClick={onSelect}>
        <span className="table-views-row-check" aria-hidden>
          {active ? <Check size={14} /> : null}
        </span>
        <span>{label}</span>
      </button>
      <Tooltip title={editLabel}>
        <Button
          type="text"
          size="small"
          aria-label={editLabel}
          icon={<Pencil size={14} />}
          onClick={onEdit}
        />
      </Tooltip>
    </div>
  )
}
