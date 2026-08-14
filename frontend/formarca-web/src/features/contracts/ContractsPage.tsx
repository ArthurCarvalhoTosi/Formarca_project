import { Typography } from 'antd'
import { useI18n } from '@/shared/i18n'

export function ContractsPage() {
  const { t } = useI18n()

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('contracts.pageTitle')}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {t('contracts.emptyDescription')}
      </Typography.Paragraph>
    </div>
  )
}
