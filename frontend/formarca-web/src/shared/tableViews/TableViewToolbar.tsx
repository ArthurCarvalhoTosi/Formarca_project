import { useEffect, useState } from 'react'
import {
  App,
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Popover,
  Select,
  Space,
  Tooltip,
} from 'antd'
import { Columns3 } from 'lucide-react'
import { useI18n } from '@/shared/i18n'
import { DEFAULT_VIEW_VALUE } from './types'
import type { TableViewController } from './useTableViews'
import { columnLabel, nextVisibleKeys } from './visibility'

type TableViewToolbarProps<T> = {
  controller: TableViewController<T>
}

type NameModalState = {
  mode: 'saveAs' | 'rename'
  name: string
}

export function TableViewToolbar<T>({ controller }: TableViewToolbarProps<T>) {
  const { t } = useI18n()
  const { modal } = App.useApp()
  const [nameModal, setNameModal] = useState<NameModalState | null>(null)
  const [form] = Form.useForm<{ name: string }>()
  const [submitting, setSubmitting] = useState(false)

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

  useEffect(() => {
    if (nameModal) {
      form.setFieldsValue({ name: nameModal.name })
    } else {
      form.resetFields()
    }
  }, [form, nameModal])

  const hasSavedView = Boolean(activeView)
  const busy = loading || mutating || submitting

  const openSaveAs = () => {
    setNameModal({ mode: 'saveAs', name: '' })
  }

  const openRename = () => {
    if (!activeView) return
    setNameModal({ mode: 'rename', name: activeView.name })
  }

  const confirmDelete = () => {
    modal.confirm({
      title: t('tableViews.deleteConfirmTitle'),
      content: t('tableViews.deleteConfirmContent'),
      okText: t('common.delete'),
      okButtonProps: { danger: true },
      cancelText: t('common.cancel'),
      onOk: () => remove(),
    })
  }

  const submitName = async () => {
    const values = await form.validateFields()
    const name = values.name.trim()
    if (!name) return
    setSubmitting(true)
    try {
      if (nameModal?.mode === 'rename') {
        await rename(name)
      } else {
        await saveAs(name)
      }
      setNameModal(null)
    } catch {
      // Error toast is handled in the hook.
    } finally {
      setSubmitting(false)
    }
  }

  const picker = (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
        maxHeight: 360,
        overflow: 'auto',
        minWidth: 220,
      }}
    >
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
  )

  return (
    <>
      <Space wrap size="small">
        <Select
          aria-label={t('tableViews.selectView')}
          value={activeView?.id ?? DEFAULT_VIEW_VALUE}
          loading={loading}
          disabled={busy && !loading}
          style={{ minWidth: 200 }}
          onChange={(value) => {
            selectView(value === DEFAULT_VIEW_VALUE ? null : value)
          }}
          options={[
            { value: DEFAULT_VIEW_VALUE, label: t('tableViews.default') },
            ...views.map((view) => ({ value: view.id, label: view.name })),
          ]}
        />
        <Popover trigger="click" placement="bottomRight" content={picker}>
          <Button icon={<Columns3 size={16} />}>{t('tableViews.columns')}</Button>
        </Popover>
        <Tooltip title={hasSavedView && isDirty ? t('tableViews.unsaved') : undefined}>
          <Button
            type={hasSavedView && isDirty ? 'primary' : 'default'}
            disabled={!hasSavedView || !isDirty || busy}
            loading={mutating && hasSavedView && isDirty}
            onClick={() => {
              void save()
            }}
          >
            {t('common.save')}
          </Button>
        </Tooltip>
        <Button disabled={busy} onClick={openSaveAs}>
          {t('tableViews.saveAs')}
        </Button>
        <Button disabled={!hasSavedView || busy} onClick={openRename}>
          {t('tableViews.rename')}
        </Button>
        <Button danger disabled={!hasSavedView || busy} onClick={confirmDelete}>
          {t('common.delete')}
        </Button>
      </Space>

      <Modal
        open={Boolean(nameModal)}
        title={
          nameModal?.mode === 'rename'
            ? t('tableViews.rename')
            : t('tableViews.saveAs')
        }
        okText={
          nameModal?.mode === 'rename' ? t('tableViews.rename') : t('tableViews.saveAs')
        }
        cancelText={t('common.cancel')}
        confirmLoading={submitting}
        onCancel={() => setNameModal(null)}
        onOk={() => {
          void submitName()
        }}
        destroyOnHidden
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label={t('tableViews.nameLabel')}
            rules={[
              { required: true, whitespace: true, message: t('tableViews.nameRequired') },
            ]}
          >
            <Input
              autoFocus
              placeholder={t('tableViews.namePlaceholder')}
              maxLength={80}
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
