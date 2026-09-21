import type { FilterChip } from './useListSearch'

export function splitCsv(value: string | undefined | null): string[] {
  if (!value?.trim()) return []
  return value
    .split(',')
    .map((part) => part.trim())
    .filter(Boolean)
}

export function joinCsv(values: string[] | undefined | null): string {
  return (values ?? [])
    .map((value) => value.trim())
    .filter(Boolean)
    .join(',')
}

export function csvChips(
  csv: string,
  key: string,
  prefix: string,
  labelFor: (value: string) => string,
  onRemove: (value: string) => void,
): FilterChip[] {
  return splitCsv(csv).map((value) => ({
    key: `${key}:${value}`,
    label: `${prefix}: ${labelFor(value)}`,
    onClose: () => onRemove(value),
  }))
}
