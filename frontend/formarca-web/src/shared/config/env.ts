export const env = {
  apiUrl: (import.meta.env.VITE_API_URL ?? 'http://localhost:5000').replace(
    /\/$/,
    '',
  ),
  /** Login local sem API. Desligue com VITE_MOCK_AUTH=false. */
  mockAuth: import.meta.env.VITE_MOCK_AUTH !== 'false',
} as const
