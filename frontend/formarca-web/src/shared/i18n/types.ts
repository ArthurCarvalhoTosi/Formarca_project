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
    search: string
    filters: string
    all: string
    download: string
    create: string
    edit: string
    delete: string
    view: string
    confirmDelete: string
    clear: string
    close: string
  }
  validation: {
    required: string
    email: string
    phone: string
    taxId: string
    cpf: string
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
    brands: string
    invoices: string
    information: string
    admin: string
    collapseMenu: string
    expandMenu: string
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
    mockClientHint: string
    accessProblems: string
    back: string
    registerLink: string
    registerPrompt: string
    forbiddenTitle: string
    forbiddenSubtitle: string
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
    searchPlaceholder: string
    filterStatus: string
    filterEmail: string
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
    invoiceSettings: string
    trademarkStatuses: string
    invoiceSettingsTitle: string
    invoiceSettingsDescription: string
    invoiceSettingsSaved: string
    invoiceSettingsLoadError: string
    pixDiscount: string
    generalDiscount: string
    cardCashInterest: string
    interestN: string
  }
  onboarding: {
    bannerTitle: string
    bannerDescription: string
  }
  status: {
    Pending: string
    Active: string
    Inactive: string
    Suspended: string
    Onboarding: string
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
      commercial: string
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
      legalRepresentativeName: string
      legalRepresentativeDocument: string
      quotedAmount: string
      budgetNotes: string
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
      legalRepresentativeName: string
      legalRepresentativeDocument: string
      quotedAmount: string
      budgetNotes: string
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
    mockHint: string
    filterName: string
    filterDocument: string
    filterEmail: string
    filterStatus: string
  }
  brands: {
    title: string
    description: string
    new: string
    empty: string
    loading: string
    loadError: string
    unavailable: string
    saveSuccess: string
    saveFailed: string
    deleteSuccess: string
    deleteFailed: string
    deleteConfirmTitle: string
    deleteConfirmContent: string
    colName: string
    colTenant: string
    colClasses: string
    colDescription: string
    searchPlaceholder: string
    filterClass: string
    filterTenant: string
    formCreateTitle: string
    formEditTitle: string
    preIssue: string
    preIssueSuccess: string
    preIssueFailed: string
    preIssueCompanyRequired: string
    preIssueRepresentativeRequired: string
    fields: {
      name: string
      servicesDescription: string
      inpiClasses: string
      tenant: string
    }
    placeholders: {
      name: string
      servicesDescription: string
    }
  }
  contracts: {
    pageTitle: string
    description: string
    emptyDescription: string
    loading: string
    loadError: string
    unavailable: string
    detailTitle: string
    issue: string
    issueSuccess: string
    issueFailed: string
    signSuccess: string
    signFailed: string
    signCta: string
    contractorSignLabel: string
    autentiqueEmbedHint: string
    autentiqueAdminWaiting: string
    autentiqueAdminWaitingHint: string
    autentiqueOpenInNewTab: string
    autentiqueWaiting: string
    autentiqueIframeFallback: string
    autentiqueSimulateSign: string
    autentiqueSignLinkFailed: string
    autentiqueIframeTitle: string
    autentiqueRejected: string
    autentiqueLegacyUnavailable: string
    previewTitle: string
    previewFailed: string
    legalTextHint: string
    downloadPdf: string
    downloadWord: string
    downloadFailed: string
    saveSnapshot: string
    saveSuccess: string
    updateFailed: string
    signed: string
    notSigned: string
    colCode: string
    colClient: string
    colCnpj: string
    colCpf: string
    colBrand: string
    colStatus: string
    colClientSign: string
    colFormarcaSign: string
    colGeneratedAt: string
    colSignedAt: string
    colAmount: string
    colPaid: string
    colClasses: string
    colPaymentMethod: string
    filters: {
      clientName: string
      code: string
      dates: string
      brandName: string
      document: string
      status: string
    }
    fields: {
      code: string
      companyName: string
      companyDocument: string
      representativeDocument: string
      brandName: string
      classes: string
      amount: string
      status: string
      street: string
      number: string
      city: string
      state: string
      zipCode: string
      minimumTerm: string
      bodyMarkdown: string
    }
    status: {
      PreIssued: string
      Issued: string
      Signed: string
      Cancelled: string
    }
  }
  invoices: {
    title: string
    description: string
    empty: string
    loading: string
    loadError: string
    unavailable: string
    colContract: string
    colTenant: string
    colMethod: string
    colInstallments: string
    colAmount: string
    colExpected: string
    colReceived: string
    colStatus: string
    payTitle: string
    payDescription: string
    method: string
    pix: string
    card: string
    installments: string
    expectedAmount: string
    submitPayment: string
    submitSuccess: string
    submitFailed: string
    awaitingApproval: string
    paidMessage: string
    rejectedMessage: string
    approve: string
    reject: string
    approveSuccess: string
    rejectSuccess: string
    approveFailed: string
    rejectFailed: string
    approveConfirmTitle: string
    rejectConfirmTitle: string
    rejectConfirmContent: string
    noInvoice: string
    filterStatus: string
    filterTenant: string
    filterContract: string
    status: {
      PendingPayment: string
      AwaitingApproval: string
      Paid: string
      Rejected: string
    }
  }
  processes: {
    title: string
    description: string
    dashboardDescription: string
    empty: string
    loading: string
    loadError: string
    unavailable: string
    colBrand: string
    colStatus: string
    colNumber: string
    colContract: string
    colTenant: string
    statusGenerated: string
    viewCards: string
    viewTable: string
    kpiBrands: string
    kpiContracts: string
    kpiInvoices: string
    kpiProcesses: string
    funnelBrand: string
    funnelContract: string
    funnelInvoice: string
    funnelProcess: string
    waitingContract: string
    waitingInvoice: string
    waitingPayment: string
    waitingSignature: string
    processGenerated: string
    filterBrand: string
    filterStage: string
    filterTenant: string
    stageBrand: string
    stageContract: string
    stageInvoice: string
    stageProcess: string
  }
  trademark: {
    dashboardTitle: string
    dashboardDescription: string
    viewStatus: string
    viewFunnel: string
    kpiTotal: string
    kpiWithoutStatus: string
    kpiAction: string
    kpiPaused: string
    kpiActive: string
    kpiClosed: string
    filterPhase: string
    filterStatus: string
    filterBrand: string
    filterTenant: string
    emptyTitle: string
    emptyDescription: string
    noResultsTitle: string
    noResultsDescription: string
    noStatus: string
    noStatusHint: string
    updatedAt: string
    createdAt: string
    openDetails: string
    processNumber: string
    contract: string
    changeStatus: string
    statusLabel: string
    noteLabel: string
    notePlaceholder: string
    saveStatus: string
    statusSaved: string
    statusSaveFailed: string
    historyTitle: string
    historyEmpty: string
    historyFrom: string
    loadError: string
    unavailable: string
    phases: {
      Filing: string
      Opposition: string
      Merit: string
      Decision: string
      Appeal: string
      Grant: string
      Renewal: string
      Extinction: string
    }
    tones: {
      Neutral: string
      Progress: string
      Attention: string
      Success: string
      Danger: string
      Paused: string
    }
    adminTitle: string
    adminDescription: string
    colOrder: string
    colTitle: string
    colPhase: string
    colTone: string
    colActive: string
    add: string
    editTitle: string
    createTitle: string
    fieldTitle: string
    fieldDescription: string
    fieldPhase: string
    fieldTone: string
    fieldActive: string
    fieldOrder: string
    saved: string
    saveFailed: string
    deleted: string
    deleteFailed: string
    moveUp: string
    moveDown: string
    reorderFailed: string
    active: string
    inactive: string
    loadCatalogError: string
  }
  listSearch: {
    title: string
    apply: string
    clear: string
    collapse: string
    openFilters: string
    emptyTitle: string
    emptyDescription: string
    emptyHint: string
    noResultsTitle: string
    noResultsDescription: string
  }
  tableViews: {
    default: string
    title: string
    selectView: string
    columns: string
    visibleColumns: string
    editView: string
    saveAs: string
    saveAsHint: string
    rename: string
    nameLabel: string
    namePlaceholder: string
    nameRequired: string
    nameTaken: string
    minColumns: string
    saveSuccess: string
    created: string
    renamed: string
    deleted: string
    deleteConfirmTitle: string
    deleteConfirmContent: string
    saveFailed: string
    deleteFailed: string
    unsaved: string
  }
  ufs: Record<BrazilUf, string>
}

type NestedKeys<T, Prefix extends string = ''> = {
  [K in keyof T & string]: T[K] extends string
    ? `${Prefix}${K}`
    : NestedKeys<T[K], `${Prefix}${K}.`>
}[keyof T & string]

export type MessageKey = NestedKeys<Dictionary>
