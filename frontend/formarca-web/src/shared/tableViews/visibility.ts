import type { TableViewColumn } from './types'

export function catalogForUser<T>(
  columns: TableViewColumn<T>[],
  isPlatformAdmin: boolean,
): TableViewColumn<T>[] {
  return columns.filter((column) => !column.adminOnly || isPlatformAdmin)
}

export function requiredColumnKeys<T>(catalog: TableViewColumn<T>[]): string[] {
  return catalog.filter((column) => column.hideable === false).map((column) => column.key)
}

export function defaultColumnKeys<T>(catalog: TableViewColumn<T>[]): string[] {
  return catalog.map((column) => column.key)
}

export function resolveVisibleKeys<T>(
  stored: string[] | null | undefined,
  catalog: TableViewColumn<T>[],
): string[] {
  if (stored == null) return defaultColumnKeys(catalog)

  const known = new Set(catalog.map((column) => column.key))
  const required = requiredColumnKeys(catalog)
  const picked = stored.filter((key) => known.has(key))
  if (picked.length === 0 && required.length === 0) {
    return defaultColumnKeys(catalog)
  }

  const merged = new Set([...required, ...picked])
  return catalog.map((column) => column.key).filter((key) => merged.has(key))
}

export function nextVisibleKeys<T>(
  current: string[],
  key: string,
  visible: boolean,
  catalog: TableViewColumn<T>[],
): string[] | null {
  const column = catalog.find((item) => item.key === key)
  if (!column || column.hideable === false) return current

  const next = new Set(current)
  if (visible) next.add(key)
  else next.delete(key)

  for (const required of requiredColumnKeys(catalog)) {
    next.add(required)
  }

  const ordered = catalog.map((item) => item.key).filter((item) => next.has(item))
  return ordered.length === 0 ? null : ordered
}

export function sameKeys(left: string[], right: string[]): boolean {
  if (left.length !== right.length) return false
  const other = new Set(right)
  return left.every((key) => other.has(key))
}

export function columnLabel<T>(column: TableViewColumn<T>): string {
  return typeof column.title === 'string' ? column.title : column.key
}
