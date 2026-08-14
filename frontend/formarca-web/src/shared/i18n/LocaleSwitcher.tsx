import { Segmented } from 'antd'
import { useI18n } from './I18nProvider'
import type { Locale } from './types'

export function LocaleSwitcher() {
  const { locale, setLocale, t } = useI18n()

  return (
    <Segmented<Locale>
      size="small"
      value={locale}
      aria-label={t('locale.label')}
      onChange={setLocale}
      options={[
        { label: t('locale.ptBR'), value: 'pt-BR' },
        { label: t('locale.en'), value: 'en' },
      ]}
    />
  )
}
