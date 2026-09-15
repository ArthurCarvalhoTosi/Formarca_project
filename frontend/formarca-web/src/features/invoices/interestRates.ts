import { INSTALLMENT_NUMBERS, type InterestRates, type InvoiceSettingsDto } from './types'

export function defaultInterestRates(value = 0): InterestRates {
  const rates = { cardCash: value } as InterestRates
  for (const n of INSTALLMENT_NUMBERS) {
    rates[`${n}`] = value
  }
  return rates
}

export function parseInterestRates(value: unknown): InterestRates {
  const raw =
    typeof value === 'string'
      ? (JSON.parse(value) as Record<string, unknown>)
      : value && typeof value === 'object'
        ? (value as Record<string, unknown>)
        : {}

  const rates = defaultInterestRates(0)
  for (const n of INSTALLMENT_NUMBERS) {
    const parsed = Number(raw[`${n}`] ?? raw[n])
    rates[`${n}`] = Number.isFinite(parsed) ? parsed : 0
  }
  const cardCash = Number(raw.cardCash)
  rates.cardCash = Number.isFinite(cardCash) ? cardCash : 0
  return rates
}

export function fromSettingsDto(dto: InvoiceSettingsDto | null | undefined) {
  const source =
    dto?.interestRates ??
    dto?.interestRatesJson ??
    (dto?.installmentInterestRates
      ? { ...dto.installmentInterestRates, cardCash: dto.cardCashInterest ?? 0 }
      : {})
  return {
    id: dto?.id,
    interestRates: parseInterestRates(source),
    pixDiscount: Number(dto?.pixDiscount ?? 0),
    generalDiscount: Number(dto?.generalDiscount ?? 0),
  }
}
