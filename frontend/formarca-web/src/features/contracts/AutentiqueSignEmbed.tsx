import { Alert, Button, Space, Spin, Typography } from 'antd'
import { useI18n } from '@/shared/i18n'

type AutentiqueSignEmbedProps = {
  signUrl: string | null | undefined
  isLoadingLink: boolean
  linkErrorMessage: string | null
  isRejected: boolean
  showSimulate: boolean
  simulatePending: boolean
  onSimulate: () => void
}

export function AutentiqueSignEmbed({
  signUrl,
  isLoadingLink,
  linkErrorMessage,
  isRejected,
  showSimulate,
  simulatePending,
  onSimulate,
}: AutentiqueSignEmbedProps) {
  const { t } = useI18n()

  return (
    <div>
      <Typography.Paragraph type="secondary">
        {t('contracts.autentiqueEmbedHint')}
      </Typography.Paragraph>
      {isLoadingLink && !signUrl ? <Spin tip={t('common.loading')} /> : null}
      {linkErrorMessage && !signUrl ? (
        <Alert type="error" showIcon message={linkErrorMessage} style={{ marginBottom: 16 }} />
      ) : null}
      {signUrl ? (
        <iframe
          src={signUrl}
          title={t('contracts.autentiqueIframeTitle')}
          style={{
            width: '100%',
            height: 720,
            border: '1px solid rgba(36, 36, 36, 0.15)',
            borderRadius: 8,
            background: '#fff',
            display: 'block',
          }}
        />
      ) : null}
      {isRejected ? (
        <Alert
          type="error"
          showIcon
          message={t('contracts.autentiqueRejected')}
          style={{ marginTop: signUrl ? 16 : 0 }}
        />
      ) : (
        <Alert
          type="info"
          showIcon
          message={t('contracts.autentiqueWaiting')}
          description={t('contracts.autentiqueIframeFallback')}
          style={{ marginTop: signUrl ? 16 : 0 }}
        />
      )}
      <Space wrap style={{ marginTop: 16 }}>
        <Button
          disabled={!signUrl}
          onClick={() => {
            if (!signUrl) return
            window.open(signUrl, '_blank', 'noopener,noreferrer')
          }}
        >
          {t('contracts.autentiqueOpenInNewTab')}
        </Button>
        {showSimulate ? (
          <Button type="primary" loading={simulatePending} onClick={onSimulate}>
            {t('contracts.autentiqueSimulateSign')}
          </Button>
        ) : null}
      </Space>
    </div>
  )
}
