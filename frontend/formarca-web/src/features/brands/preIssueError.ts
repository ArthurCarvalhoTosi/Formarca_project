import { getApiErrorMessage } from '@/shared/api/types'
import type { TranslateFn } from '@/shared/i18n/I18nProvider'

export const PRE_ISSUE_COMPANY_DOCUMENT_REQUIRED =
  'Company document is required to pre-issue a contract.'
export const PRE_ISSUE_REPRESENTATIVE_DOCUMENT_REQUIRED =
  'Legal representative document is required to pre-issue a contract.'

export function preIssueErrorMessage(error: unknown, t: TranslateFn): string {
  return getApiErrorMessage(error, t('brands.preIssueFailed'), {
    [PRE_ISSUE_COMPANY_DOCUMENT_REQUIRED]: t('brands.preIssueCompanyRequired'),
    [PRE_ISSUE_REPRESENTATIVE_DOCUMENT_REQUIRED]: t(
      'brands.preIssueRepresentativeRequired',
    ),
  })
}
