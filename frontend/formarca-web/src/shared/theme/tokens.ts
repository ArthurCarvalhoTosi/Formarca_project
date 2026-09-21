export const brand = {
  colors: {
    primary: '#F0762A',
    primaryDark: '#D54C0C',
    primaryLight: '#FA9850',
    primaryLighter: '#FAA56A',
    amber: '#E38B24',
    cream: '#F7F3ED',
    ink: '#242424',
    textOnAmber: '#4A2C09',
    success: '#2D9B59',
    white: '#FFFFFF',
  },
  radius: 4,
  fontFamily: '"Inter", "Segoe UI", sans-serif',
  fontFamilyDisplay: '"Plus Jakarta Sans", "Inter", sans-serif',
} as const

export const antdThemeToken = {
  colorPrimary: brand.colors.primary,
  colorPrimaryHover: brand.colors.primaryDark,
  colorPrimaryActive: brand.colors.primaryDark,
  colorSuccess: brand.colors.success,
  colorBgLayout: brand.colors.cream,
  colorBgContainer: brand.colors.white,
  colorText: brand.colors.ink,
  colorTextSecondary: 'rgba(36, 36, 36, 0.65)',
  borderRadius: brand.radius,
  fontFamily: brand.fontFamily,
} as const

export const antdComponentTokens = {
  Table: {
    headerBg: brand.colors.cream,
    headerColor: brand.colors.ink,
    headerBorderRadius: 4,
    rowHoverBg: 'rgba(240, 118, 42, 0.06)',
  },
  Button: {
    primaryShadow: 'none',
    borderRadius: brand.radius,
    fontWeight: 600,
  },
  Card: {
    borderRadiusLG: 4,
  },
  Drawer: {
    paddingLG: 16,
  },
  Layout: {
    bodyBg: brand.colors.cream,
    headerBg: brand.colors.white,
    siderBg: brand.colors.white,
    lightSiderBg: brand.colors.white,
  },
  Tag: {
    borderRadiusSM: 4,
  },
} as const
