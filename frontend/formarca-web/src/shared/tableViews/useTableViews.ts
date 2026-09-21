import { App } from 'antd'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo, useState } from 'react'
import { ApiError } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n } from '@/shared/i18n'
import { tableViewsApi } from './api'
import type { TableKey, TableViewColumn, TableViewDto } from './types'
import {
  catalogForUser,
  nextVisibleKeys,
  resolveVisibleKeys,
  sameKeys,
} from './visibility'

export type UseTableViewsArgs<T> = {
  tableKey: TableKey
  columns: TableViewColumn<T>[]
}

export type TableViewController<T> = {
  tableKey: TableKey
  views: TableViewDto[]
  activeView: TableViewDto | null
  catalog: TableViewColumn<T>[]
  visibleKeys: string[]
  visibleColumns: TableViewColumn<T>[]
  isDirty: boolean
  loading: boolean
  mutating: boolean
  selectView: (id: string | null) => void
  toggleColumn: (key: string, visible: boolean) => void
  save: () => Promise<void>
  saveAs: (name: string) => Promise<void>
  rename: (name: string) => Promise<void>
  remove: () => Promise<void>
}

function errorMessage(error: unknown, fallback: string, nameTaken: string): string {
  if (error instanceof ApiError && error.status === 409) return nameTaken
  if (error instanceof ApiError && error.message) return error.message
  return fallback
}

export function useTableViews<T>({
  tableKey,
  columns,
}: UseTableViewsArgs<T>): TableViewController<T> {
  const { t } = useI18n()
  const { message } = App.useApp()
  const { isPlatformAdmin, user } = useAuth()
  const queryClient = useQueryClient()

  const catalog = useMemo(
    () => catalogForUser(columns, isPlatformAdmin),
    [columns, isPlatformAdmin],
  )

  const queryKey = ['me', 'table-views', tableKey, user?.id] as const

  const query = useQuery({
    queryKey,
    queryFn: () => tableViewsApi.list(tableKey),
    enabled: Boolean(user?.id),
    retry: 1,
  })

  const views = useMemo(() => query.data ?? [], [query.data])
  const serverActive = useMemo(
    () => views.find((view) => view.isActive) ?? null,
    [views],
  )

  const [selectedId, setSelectedId] = useState<string | null | undefined>(undefined)
  const [draftKeys, setDraftKeys] = useState<string[] | null>(null)

  const activeView =
    selectedId === undefined
      ? serverActive
      : selectedId === null
        ? null
        : (views.find((view) => view.id === selectedId) ?? null)

  const baselineKeys = useMemo(
    () =>
      resolveVisibleKeys(
        activeView ? activeView.columnKeys : null,
        catalog,
      ),
    [activeView, catalog],
  )

  const visibleKeys = draftKeys ?? baselineKeys
  const isDirty = !sameKeys(visibleKeys, baselineKeys)

  const visibleColumns = useMemo(
    () => catalog.filter((column) => visibleKeys.includes(column.key)),
    [catalog, visibleKeys],
  )

  const invalidate = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: ['me', 'table-views', tableKey] })
  }, [queryClient, tableKey])

  const activateMutation = useMutation({
    mutationFn: (id: string) => tableViewsApi.activate(id),
    onSuccess: async (view) => {
      setSelectedId(view.id)
      setDraftKeys(null)
      await invalidate()
    },
    onError: (error) => {
      setSelectedId(undefined)
      message.error(errorMessage(error, t('tableViews.saveFailed'), t('tableViews.nameTaken')))
    },
  })

  const saveMutation = useMutation({
    mutationFn: (payload: { id: string; columnKeys: string[] }) =>
      tableViewsApi.update(payload.id, { columnKeys: payload.columnKeys }),
    onSuccess: async () => {
      setDraftKeys(null)
      await invalidate()
      message.success(t('tableViews.saveSuccess'))
    },
  })

  const createMutation = useMutation({
    mutationFn: (payload: { name: string; columnKeys: string[] }) =>
      tableViewsApi.create({
        tableKey,
        name: payload.name,
        columnKeys: payload.columnKeys,
        activate: true,
      }),
    onSuccess: async (view) => {
      setSelectedId(view.id)
      setDraftKeys(null)
      await invalidate()
      message.success(t('tableViews.created'))
    },
  })

  const renameMutation = useMutation({
    mutationFn: (payload: { id: string; name: string }) =>
      tableViewsApi.update(payload.id, { name: payload.name }),
    onSuccess: async () => {
      await invalidate()
      message.success(t('tableViews.renamed'))
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => tableViewsApi.remove(id),
    onSuccess: async () => {
      setSelectedId(undefined)
      setDraftKeys(null)
      await invalidate()
      message.success(t('tableViews.deleted'))
    },
  })

  const selectView = useCallback(
    (id: string | null) => {
      setDraftKeys(null)
      setSelectedId(id)
      if (id && id !== serverActive?.id) {
        activateMutation.mutate(id)
      }
    },
    [activateMutation, serverActive?.id],
  )

  const toggleColumn = useCallback(
    (key: string, visible: boolean) => {
      const next = nextVisibleKeys(visibleKeys, key, visible, catalog)
      if (next == null) {
        message.warning(t('tableViews.minColumns'))
        return
      }
      setDraftKeys(next)
    },
    [catalog, message, t, visibleKeys],
  )

  const save = useCallback(async () => {
    if (!activeView) return
    try {
      await saveMutation.mutateAsync({ id: activeView.id, columnKeys: visibleKeys })
    } catch (error) {
      message.error(errorMessage(error, t('tableViews.saveFailed'), t('tableViews.nameTaken')))
      throw error
    }
  }, [activeView, message, saveMutation, t, visibleKeys])

  const saveAs = useCallback(
    async (name: string) => {
      try {
        await createMutation.mutateAsync({ name, columnKeys: visibleKeys })
      } catch (error) {
        message.error(errorMessage(error, t('tableViews.saveFailed'), t('tableViews.nameTaken')))
        throw error
      }
    },
    [createMutation, message, t, visibleKeys],
  )

  const rename = useCallback(
    async (name: string) => {
      if (!activeView) return
      try {
        await renameMutation.mutateAsync({ id: activeView.id, name })
      } catch (error) {
        message.error(errorMessage(error, t('tableViews.saveFailed'), t('tableViews.nameTaken')))
        throw error
      }
    },
    [activeView, message, renameMutation, t],
  )

  const remove = useCallback(async () => {
    if (!activeView) return
    try {
      await deleteMutation.mutateAsync(activeView.id)
    } catch (error) {
      message.error(errorMessage(error, t('tableViews.deleteFailed'), t('tableViews.nameTaken')))
      throw error
    }
  }, [activeView, deleteMutation, message, t])

  const mutating =
    activateMutation.isPending ||
    saveMutation.isPending ||
    createMutation.isPending ||
    renameMutation.isPending ||
    deleteMutation.isPending

  return {
    tableKey,
    views,
    activeView,
    catalog,
    visibleKeys,
    visibleColumns,
    isDirty,
    loading: query.isLoading,
    mutating,
    selectView,
    toggleColumn,
    save,
    saveAs,
    rename,
    remove,
  }
}
