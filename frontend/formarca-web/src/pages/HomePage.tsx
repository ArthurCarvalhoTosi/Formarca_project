import { Collapse, Typography } from 'antd'
import {
  Briefcase,
  ClipboardList,
  FileText,
  Info,
  ListChecks,
  Mail,
  MapPin,
  Phone,
  Receipt,
  Settings,
  ShieldCheck,
  Tag,
  type LucideIcon,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/shared/auth/AuthContext'
import { useI18n, type MessageKey } from '@/shared/i18n'
import { AdminHomeDashboard } from './AdminHomeDashboard'

const SITE_URL = 'https://formarca.com.br/'
const EMAIL = 'contato@formarca.com.br'
const PHONE_DISPLAY = '(11) 97631-7092'
const PHONE_TEL = '+5511976317092'
const WHATSAPP_URL = 'https://wa.me/5511976317092'

type Shortcut = {
  to: string
  icon: LucideIcon
  title: MessageKey
  hint: MessageKey
}

export function HomePage() {
  const { user, tenants, activeTenantId, isPlatformAdmin, isOnboardingLimited, hasPermission } =
    useAuth()
  const { t } = useI18n()
  const activeTenant = tenants.find((tenant) => tenant.id === activeTenantId)

  const welcome = user?.displayName
    ? t('home.welcomeNamed', { name: user.displayName })
    : t('home.welcome')

  const shortcuts: Shortcut[] = [
    !isOnboardingLimited && hasPermission('processes.read')
      ? {
          to: '/processos',
          icon: ClipboardList,
          title: 'home.shortcutProcesses',
          hint: 'home.shortcutProcessesHint',
        }
      : null,
    hasPermission('contracts.read')
      ? {
          to: '/contratos',
          icon: FileText,
          title: 'home.shortcutContracts',
          hint: 'home.shortcutContractsHint',
        }
      : null,
    hasPermission('invoices.read')
      ? {
          to: '/faturas',
          icon: Receipt,
          title: 'home.shortcutInvoices',
          hint: 'home.shortcutInvoicesHint',
        }
      : null,
    hasPermission('registrations.read')
      ? {
          to: '/clientes',
          icon: Briefcase,
          title: 'home.shortcutCustomers',
          hint: 'home.shortcutCustomersHint',
        }
      : null,
    !isOnboardingLimited && hasPermission('brands.read')
      ? {
          to: '/marcas',
          icon: Tag,
          title: 'home.shortcutBrands',
          hint: 'home.shortcutBrandsHint',
        }
      : null,
    !isOnboardingLimited
      ? {
          to: '/informacoes',
          icon: Info,
          title: 'home.shortcutInformation',
          hint: 'home.shortcutInformationHint',
        }
      : null,
    isPlatformAdmin
      ? {
          to: '/admin/trademark-statuses',
          icon: ListChecks,
          title: 'home.shortcutStatuses',
          hint: 'home.shortcutStatusesHint',
        }
      : null,
    isPlatformAdmin
      ? {
          to: '/admin/tenants',
          icon: Settings,
          title: 'home.shortcutAdmin',
          hint: 'home.shortcutAdminHint',
        }
      : null,
  ].filter((item): item is Shortcut => item !== null)

  const highlights = [
    t('home.highlightProtocol'),
    t('home.highlightSupport'),
    t('home.highlightCopies'),
  ]

  const reasons: { title: MessageKey; body: MessageKey }[] = [
    { title: 'home.whyLegalTitle', body: 'home.whyLegalBody' },
    { title: 'home.whyTrustTitle', body: 'home.whyTrustBody' },
    { title: 'home.whyValueTitle', body: 'home.whyValueBody' },
  ]

  const steps: { title: MessageKey; body: MessageKey }[] = [
    { title: 'home.stepViabilityTitle', body: 'home.stepViabilityBody' },
    { title: 'home.stepContractTitle', body: 'home.stepContractBody' },
    { title: 'home.stepFilingTitle', body: 'home.stepFilingBody' },
    { title: 'home.stepCertificateTitle', body: 'home.stepCertificateBody' },
  ]

  const faq: { key: string; question: MessageKey; answer: MessageKey }[] = [
    { key: 'duration', question: 'home.faqDurationQ', answer: 'home.faqDurationA' },
    { key: 'knowledge', question: 'home.faqKnowledgeQ', answer: 'home.faqKnowledgeA' },
    { key: 'refusal', question: 'home.faqRefusalQ', answer: 'home.faqRefusalA' },
    { key: 'followUp', question: 'home.faqFollowUpQ', answer: 'home.faqFollowUpA' },
    { key: 'inUse', question: 'home.faqInUseQ', answer: 'home.faqInUseA' },
    { key: 'firstStep', question: 'home.faqFirstStepQ', answer: 'home.faqFirstStepA' },
    { key: 'brazil', question: 'home.faqBrazilQ', answer: 'home.faqBrazilA' },
    { key: 'individual', question: 'home.faqIndividualQ', answer: 'home.faqIndividualA' },
    { key: 'classes', question: 'home.faqClassesQ', answer: 'home.faqClassesA' },
  ]

  return (
    <div className="home-page">
      <header className="page-header">
        <div className="page-header-copy">
          <Typography.Title level={3} className="page-header-title">
            {t('home.title')}
          </Typography.Title>
          <Typography.Paragraph type="secondary" className="page-header-description">
            {welcome} {isPlatformAdmin ? t('home.adminLead') : t('home.foundation')}
          </Typography.Paragraph>
        </div>
      </header>

      {activeTenant ? (
        <Typography.Paragraph className="home-tenant">
          {t('home.activeTenant')}{' '}
          <Typography.Text strong>{activeTenant.name}</Typography.Text>
        </Typography.Paragraph>
      ) : (
        <Typography.Paragraph type="secondary" className="home-tenant">
          {isPlatformAdmin ? t('home.noActiveTenantAdmin') : t('home.noActiveTenant')}
        </Typography.Paragraph>
      )}

      {isPlatformAdmin ? <AdminHomeDashboard /> : null}

      {shortcuts.length > 0 ? (
        <section className="home-section" aria-labelledby="home-shortcuts-title">
          <Typography.Title level={4} id="home-shortcuts-title" className="home-section-title">
            {t('home.shortcutsTitle')}
          </Typography.Title>
          <div className="home-shortcuts">
            {shortcuts.map((shortcut) => {
              const Icon = shortcut.icon
              return (
                <Link key={shortcut.to} to={shortcut.to} className="home-shortcut">
                  <span className="home-shortcut-icon" aria-hidden>
                    <Icon size={18} strokeWidth={1.75} />
                  </span>
                  <span className="home-shortcut-title">{t(shortcut.title)}</span>
                  <span className="home-shortcut-hint">{t(shortcut.hint)}</span>
                </Link>
              )
            })}
          </div>
        </section>
      ) : null}

      {!isPlatformAdmin ? (
      <section className="home-about" aria-labelledby="home-about-title">
        <div className="home-about-hero">
          <p className="home-eyebrow">{t('home.aboutEyebrow')}</p>
          <Typography.Title level={3} id="home-about-title" className="home-about-title">
            {t('home.aboutTitle')}
          </Typography.Title>
          <Typography.Paragraph className="home-about-lead">
            {t('home.aboutLead')}
          </Typography.Paragraph>
          <ul className="home-highlights">
            {highlights.map((label) => (
              <li key={label}>
                <ShieldCheck size={16} strokeWidth={1.75} aria-hidden />
                {label}
              </li>
            ))}
          </ul>
        </div>

        <div className="home-about-grid">
          <article className="home-card">
            <Typography.Title level={5} className="home-card-title">
              {t('home.stepsTitle')}
            </Typography.Title>
            <ol className="home-steps">
              {steps.map((step, index) => (
                <li key={step.title}>
                  <span className="home-step-index">{index + 1}</span>
                  <div>
                    <strong>{t(step.title)}</strong>
                    <p>{t(step.body)}</p>
                  </div>
                </li>
              ))}
            </ol>
          </article>

          <article className="home-card">
            <Typography.Title level={5} className="home-card-title">
              {t('home.whyTitle')}
            </Typography.Title>
            <ul className="home-reasons">
              {reasons.map((reason) => (
                <li key={reason.title}>
                  <strong>{t(reason.title)}</strong>
                  <p>{t(reason.body)}</p>
                </li>
              ))}
            </ul>
          </article>
        </div>

        <article className="home-card">
          <Typography.Title level={5} className="home-card-title">
            {t('home.contactTitle')}
          </Typography.Title>
          <ul className="home-contact">
            <li>
              <Mail size={16} strokeWidth={1.75} aria-hidden />
              <a href={`mailto:${EMAIL}`}>{EMAIL}</a>
            </li>
            <li>
              <Phone size={16} strokeWidth={1.75} aria-hidden />
              <a href={`tel:${PHONE_TEL}`}>{PHONE_DISPLAY}</a>
            </li>
            <li>
              <MapPin size={16} strokeWidth={1.75} aria-hidden />
              <span>
                {t('home.contactCities')}
                <br />
                {t('home.contactCnpj')}
              </span>
            </li>
          </ul>
          <div className="home-contact-actions">
            <a className="home-link" href={WHATSAPP_URL} target="_blank" rel="noreferrer">
              {t('home.whatsapp')}
            </a>
            <a className="home-link home-link-ghost" href={SITE_URL} target="_blank" rel="noreferrer">
              {t('home.visitSite')}
            </a>
          </div>
        </article>

        <article className="home-card">
          <Typography.Title level={5} className="home-card-title">
            {t('home.faqTitle')}
          </Typography.Title>
          <Collapse
            ghost
            items={faq.map((item) => ({
              key: item.key,
              label: t(item.question),
              children: <p className="home-faq-answer">{t(item.answer)}</p>,
            }))}
          />
        </article>

        <article className="home-card">
          <Typography.Title level={5} className="home-card-title">
            {t('home.teamTitle')}
          </Typography.Title>
          <Typography.Paragraph type="secondary">{t('home.teamLead')}</Typography.Paragraph>
          <div className="home-team">
            <div>
              <strong>{t('home.gabrielName')}</strong>
              <span>{t('home.gabrielRole')}</span>
              <p>{t('home.gabrielBio')}</p>
            </div>
            <div>
              <strong>{t('home.lucasName')}</strong>
              <span>{t('home.lucasRole')}</span>
              <p>{t('home.lucasBio')}</p>
            </div>
          </div>
        </article>
      </section>
      ) : null}
    </div>
  )
}
