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
  radius: 10,
  fontFamily: '"Inter", "Segoe UI", sans-serif',
  fontFamilyDisplay: '"Plus Jakarta Sans", "Inter", sans-serif',
} as const

export const antdThemeToken = {
  colorPrimary: brand.colors.primary,
  colorPrimaryHover: brand.colors.primaryDark,
  colorPrimaryActive: brand.colors.primaryDark,
  colorSuccess: brand.colors.success,
  colorBgLayout: brand.colors.cream,
  colorText: brand.colors.ink,
  borderRadius: brand.radius,
  fontFamily: brand.fontFamily,
} as const
