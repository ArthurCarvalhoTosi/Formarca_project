export const LOCALES = ['pt-BR', 'en'] as const

export type Locale = (typeof LOCALES)[number]

export const LOCALE_STORAGE_KEY = 'formarca.locale'

export const BRAZIL_UFS = [
  'AC',
  'AL',
  'AP',
  'AM',
  'BA',
  'CE',
  'DF',
  'ES',
  'GO',
  'MA',
  'MT',
  'MS',
  'MG',
  'PA',
  'PB',
  'PR',
  'PE',
  'PI',
  'RJ',
  'RN',
  'RS',
  'RO',
  'RR',
  'SC',
  'SP',
  'SE',
  'TO',
] as const

export type BrazilUf = (typeof BRAZIL_UFS)[number]

export type Dictionary = {
  common: {
    save: string
    cancel: string
    loading: string
    yes: string
    no: string
    optional: string
    back: string
    submit: string
    actions: string
    error: string
    success: string
  }
  validation: {
    required: string
    email: string
    phone: string
    taxId: string
    zipCode: string
    uf: string
    passwordMin: string
  }
  locale: {
    label: string
    ptBR: string
    en: string
  }
  nav: {
    home: string
    processes: string
    contracts: string
    registration: string
    customers: string
    information: string
    admin: string
  }
  auth: {
    title: string
    subtitle: string
    email: string
    password: string
    emailPlaceholder: string
    signIn: string
    loginFailed: string
    loginRetry: string
    apiUnreachable: string
    mockBanner: string
    mockHint: string
    accessProblems: string
    back: string
    registerLink: string
    registerPrompt: string
  }
  session: {
    loading: string
  }
  home: {
    title: string
    welcome: string
    welcomeNamed: string
    foundation: string
    activeTenant: string
    noActiveTenant: string
    noActiveTenantAdmin: string
  }
  placeholder: {
    comingSoon: string
    processesDescription: string
    contractsDescription: string
    informationDescription: string
  }
  header: {
    platformAdmin: string
    user: string
    logout: string
    company: string
    selectTenant: string
  }
  admin: {
    tenants: string
    users: string
    groups: string
    tenantsTitle: string
    tenantsDescription: string
    tenantsUnavailable: string
    tenantsLoadError: string
    tenantsLoadHint: string
    tenantsEmpty: string
    tenantsLoading: string
    usersTitle: string
    usersDescription: string
    usersUnavailable: string
    usersLoadError: string
    usersEmpty: string
    usersLoading: string
    groupsTitle: string
    groupsDescription: string
    groupsUnavailable: string
    groupsLoadError: string
    groupsEmpty: string
    groupsLoading: string
    colName: string
    colSlug: string
    colDocument: string
    colStatus: string
    colEmail: string
    colDisplayName: string
    colPlatformRole: string
    colActive: string
    colScope: string
    colDescription: string
    scopeTenant: string
    scopePlatform: string
    restrictedTitle: string
    restrictedSubtitle: string
    approve: string
    reject: string
    approveTitle: string
    approvePassword: string
    approveSuccess: string
    approveFailed: string
    rejectConfirmTitle: string
    rejectConfirmContent: string
    rejectSuccess: string
    rejectFailed: string
  }
  status: {
    Pending: string
    Active: string
    Inactive: string
    Suspended: string
  }
  registration: {
    publicTitle: string
    publicSubtitle: string
    publicSubmit: string
    publicSuccessTitle: string
    publicSuccessDescription: string
    goToLogin: string
    panelTitle: string
    panelDescription: string
    emptyTitle: string
    emptyDescription: string
    save: string
    saveSuccess: string
    sections: {
      personal: string
      address: string
      brand: string
    }
    fields: {
      name: string
      email: string
      phone: string
      taxId: string
      zipCode: string
      street: string
      number: string
      complement: string
      neighborhood: string
      city: string
      state: string
      brandName: string
      brandDescription: string
    }
    placeholders: {
      name: string
      email: string
      phone: string
      taxId: string
      zipCode: string
      street: string
      number: string
      complement: string
      neighborhood: string
      city: string
      brandName: string
      brandDescription: string
    }
    viaCepFailed: string
    errors: {
      emailTaken: string
      documentTaken: string
      submitFailed: string
      saveFailed: string
      loadFailed: string
    }
  }
  customers: {
    title: string
    description: string
    searchPlaceholder: string
    empty: string
    new: string
    loading: string
    unavailable: string
    loadError: string
    colName: string
    colDocument: string
    colEmail: string
    colStatus: string
  }
  contracts: {
    pageTitle: string
    emptyDescription: string
  }
  ufs: Record<BrazilUf, string>
}

type NestedKeys<T, Prefix extends string = ''> = {
  [K in keyof T & string]: T[K] extends string
    ? `${Prefix}${K}`
    : NestedKeys<T[K], `${Prefix}${K}.`>
}[keyof T & string]

export type MessageKey = NestedKeys<Dictionary>
