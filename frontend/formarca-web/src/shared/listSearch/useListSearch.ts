import { useCallback, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { splitCsv } from './csv'
import { DEFAULT_PAGE, DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS } from './pagination'

export const SEARCHED_PARAM = 'searched'
export const PAGE_PARAM = 'page'
export const PAGE_SIZE_PARAM = 'pageSize'

export type SearchFilters = Record<string, string>

export type FilterChip = {
  key: string
  label: string
  onClose?: () => void
}

function isFilled(value: string | undefined | null): boolean {
  return Boolean(value && value.trim())
}

function countFilled(filters: SearchFilters): number {
  return Object.values(filters).reduce((sum, value) => {
    const parts = splitCsv(value)
    return sum + (parts.length > 0 ? parts.length : 0)
  }, 0)
}

function clampPageSize(value: number): number {
  if (PAGE_SIZE_OPTIONS.includes(value as (typeof PAGE_SIZE_OPTIONS)[number])) {
    return value
  }
  if (value < 1) return DEFAULT_PAGE_SIZE
  return Math.min(100, value)
}

export type UseListSearchResult<T extends SearchFilters> = {
  draft: T
  applied: T
  hasSearched: boolean
  sidebarOpen: boolean
  page: number
  pageSize: number
  appliedCount: number
  updateDraft: (partial: Partial<T>) => void
  setDraftValue: (key: keyof T, value: string) => void
  search: () => void
  resetDraft: () => void
  removeFilter: (key: keyof T) => void
  removeFilters: (keys: (keyof T)[]) => void
  removeCsvValue: (key: keyof T, value: string) => void
  openSidebar: () => void
  collapseSidebar: () => void
  setPage: (page: number, pageSize?: number) => void
}

export function useListSearch<T extends SearchFilters>(
  defaultFilters: T,
): UseListSearchResult<T> {
  const defaultsRef = useRef(defaultFilters)
  const filterKeys = useMemo(
    () => Object.keys(defaultsRef.current) as (keyof T & string)[],
    [],
  )
  const [params, setParams] = useSearchParams()
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [draft, setDraft] = useState<T>(() => readFilters(params, defaultsRef.current, filterKeys))

  const hasSearched = params.get(SEARCHED_PARAM) === '1'
  const applied = useMemo(
    () => (hasSearched ? readFilters(params, defaultsRef.current, filterKeys) : defaultsRef.current),
    [filterKeys, hasSearched, params],
  )
  const page = Math.max(1, Number(params.get(PAGE_PARAM) || DEFAULT_PAGE) || DEFAULT_PAGE)
  const pageSize = clampPageSize(
    Number(params.get(PAGE_SIZE_PARAM) || DEFAULT_PAGE_SIZE) || DEFAULT_PAGE_SIZE,
  )
  const appliedCount = useMemo(() => countFilled(applied), [applied])

  const write = useCallback(
    (nextFilters: T, nextPage: number, nextPageSize: number, searched: boolean) => {
      const next = new URLSearchParams(params)
      for (const key of filterKeys) next.delete(key)
      next.delete(SEARCHED_PARAM)
      next.delete(PAGE_PARAM)
      next.delete(PAGE_SIZE_PARAM)

      if (searched) {
        next.set(SEARCHED_PARAM, '1')
        for (const key of filterKeys) {
          const value = nextFilters[key]
          if (isFilled(value)) next.set(key, value.trim())
        }
        if (nextPage > DEFAULT_PAGE) next.set(PAGE_PARAM, String(nextPage))
        if (nextPageSize !== DEFAULT_PAGE_SIZE) {
          next.set(PAGE_SIZE_PARAM, String(nextPageSize))
        }
      }

      setParams(next, { replace: true })
    },
    [filterKeys, params, setParams],
  )

  const updateDraft = useCallback((partial: Partial<T>) => {
    setDraft((current) => ({ ...current, ...partial }))
  }, [])

  const setDraftValue = useCallback((key: keyof T, value: string) => {
    setDraft((current) => ({ ...current, [key]: value }))
  }, [])

  const search = useCallback(() => {
    write(draft, DEFAULT_PAGE, pageSize, true)
    setSidebarOpen(false)
  }, [draft, pageSize, write])

  const resetDraft = useCallback(() => {
    setDraft(defaultsRef.current)
  }, [])

  const removeFilters = useCallback(
    (keys: (keyof T)[]) => {
      const next = { ...applied }
      for (const key of keys) {
        next[key] = '' as T[keyof T]
      }
      setDraft(next)
      write(next, DEFAULT_PAGE, pageSize, true)
    },
    [applied, pageSize, write],
  )

  const removeFilter = useCallback(
    (key: keyof T) => {
      removeFilters([key])
    },
    [removeFilters],
  )

  const removeCsvValue = useCallback(
    (key: keyof T, value: string) => {
      const next = {
        ...applied,
        [key]: splitCsv(String(applied[key] ?? ''))
          .filter((part) => part !== value)
          .join(','),
      } as T
      setDraft(next)
      write(next, DEFAULT_PAGE, pageSize, true)
    },
    [applied, pageSize, write],
  )

  const setPage = useCallback(
    (nextPage: number, nextPageSize = pageSize) => {
      const pageToWrite = nextPageSize !== pageSize ? DEFAULT_PAGE : Math.max(1, nextPage)
      write(applied, pageToWrite, nextPageSize, true)
    },
    [applied, pageSize, write],
  )

  return {
    draft,
    applied,
    hasSearched,
    sidebarOpen,
    page,
    pageSize,
    appliedCount,
    updateDraft,
    setDraftValue,
    search,
    resetDraft,
    removeFilter,
    removeFilters,
    removeCsvValue,
    openSidebar: () => setSidebarOpen(true),
    collapseSidebar: () => setSidebarOpen(false),
    setPage,
  }
}

function readFilters<T extends SearchFilters>(
  params: URLSearchParams,
  defaults: T,
  keys: (keyof T & string)[],
): T {
  const next = { ...defaults }
  for (const key of keys) {
    const value = params.get(key)
    next[key] = (value ?? '') as T[keyof T & string]
  }
  return next
}
