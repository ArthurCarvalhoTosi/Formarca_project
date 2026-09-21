import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { en } from './en'
import { ptBR } from './pt-BR'
import {
  LOCALE_STORAGE_KEY,
  type Dictionary,
  type Locale,
  type MessageKey,
} from './types'

type TranslateVars = Record<string, string | number>

export type TranslateFn = (key: MessageKey, vars?: TranslateVars) => string

type I18nContextValue = {
  locale: Locale
  setLocale: (locale: Locale) => void
  t: TranslateFn
  dict: Dictionary
}

const I18nContext = createContext<I18nContextValue | null>(null)

function isLocale(value: string | null): value is Locale {
  return value === 'pt-BR' || value === 'en'
}

function readStoredLocale(): Locale {
  try {
    const stored = localStorage.getItem(LOCALE_STORAGE_KEY)
    if (isLocale(stored)) return stored
  } catch {
    // Ignore storage errors (private mode, etc.).
  }
  return 'pt-BR'
}

function getByPath(dict: Dictionary, path: MessageKey): string {
  const parts = path.split('.')
  let current: unknown = dict
  for (const part of parts) {
    if (current && typeof current === 'object' && part in current) {
      current = (current as Record<string, unknown>)[part]
    } else {
      return path
    }
  }
  return typeof current === 'string' ? current : path
}

function interpolate(template: string, vars?: TranslateVars): string {
  if (!vars) return template
  return template.replace(/\{(\w+)\}/g, (_, name: string) =>
    name in vars ? String(vars[name]) : `{${name}}`,
  )
}

type I18nProviderProps = {
  children: ReactNode
}

export function I18nProvider({ children }: I18nProviderProps) {
  const [locale, setLocaleState] = useState<Locale>(readStoredLocale)

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next)
    try {
      localStorage.setItem(LOCALE_STORAGE_KEY, next)
    } catch {
      // Ignore storage errors.
    }
  }, [])

  const dict = locale === 'en' ? en : ptBR

  const t = useCallback<TranslateFn>(
    (key, vars) => interpolate(getByPath(dict, key), vars),
    [dict],
  )

  useEffect(() => {
    document.documentElement.lang = locale
  }, [locale])

  const value = useMemo<I18nContextValue>(
    () => ({ locale, setLocale, t, dict }),
    [locale, setLocale, t, dict],
  )

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n(): I18nContextValue {
  const ctx = useContext(I18nContext)
  if (!ctx) {
    throw new Error('useI18n must be used within I18nProvider')
  }
  return ctx
}
