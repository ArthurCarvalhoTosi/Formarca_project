import { ConfigProvider, App as AntApp } from 'antd'
import enUS from 'antd/locale/en_US'
import ptBR from 'antd/locale/pt_BR'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import dayjs from 'dayjs'
import 'dayjs/locale/en'
import 'dayjs/locale/pt-br'
import { type ReactNode, useEffect, useState } from 'react'
import { AuthProvider } from '@/shared/auth/AuthContext'
import { I18nProvider, useI18n } from '@/shared/i18n'
import { antdComponentTokens, antdThemeToken } from '@/shared/theme/tokens'

type AppProvidersProps = {
  children: ReactNode
}

function AntdWithLocale({ children }: { children: ReactNode }) {
  const { locale } = useI18n()
  const antdLocale = locale === 'en' ? enUS : ptBR

  useEffect(() => {
    dayjs.locale(locale === 'en' ? 'en' : 'pt-br')
  }, [locale])

  return (
    <ConfigProvider
      locale={antdLocale}
      theme={{
        token: antdThemeToken,
        components: antdComponentTokens,
      }}
    >
      <AntApp>
        <AuthProvider>{children}</AuthProvider>
      </AntApp>
    </ConfigProvider>
  )
}

export function AppProviders({ children }: AppProvidersProps) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            refetchOnWindowFocus: false,
          },
        },
      }),
  )

  return (
    <QueryClientProvider client={queryClient}>
      <I18nProvider>
        <AntdWithLocale>{children}</AntdWithLocale>
      </I18nProvider>
    </QueryClientProvider>
  )
}
