export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100
}

export function formatMoney(value: number, locale: string): string {
  return new Intl.NumberFormat(locale === 'en' ? 'en-US' : 'pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value)
}

export function formatDate(value: string | null | undefined, locale: string): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat(locale === 'en' ? 'en-US' : 'pt-BR', {
    dateStyle: 'short',
  }).format(date)
}

export function formatClasses(classes: number[] | string | null | undefined): string {
  if (classes == null || classes === '') return '—'
  if (Array.isArray(classes)) {
    return classes.length ? classes.join(', ') : '—'
  }
  return classes
}

export function parseClasses(value: number[] | string | null | undefined): number[] {
  if (!value) return []
  if (Array.isArray(value)) {
    return value.map(Number).filter((n) => Number.isInteger(n) && n >= 1 && n <= 45)
  }
  return value
    .split(/[,\s]+/)
    .map((part) => Number(part.trim()))
    .filter((n) => Number.isInteger(n) && n >= 1 && n <= 45)
}
