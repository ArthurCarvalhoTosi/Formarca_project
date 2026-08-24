import { roundMoney } from '@/shared/money'
import type { InstallmentNumber, InvoicePaymentMethod, InvoiceSettings } from './types'

export function calculateExpectedAmount(
  contractAmount: number,
  invoiceDiscount: number | null | undefined,
  settings: InvoiceSettings,
  method: InvoicePaymentMethod,
  installments: number,
): number {
  let amount = contractAmount
  const discount =
    invoiceDiscount != null && invoiceDiscount > 0
      ? invoiceDiscount
      : settings.generalDiscount
  amount = amount * (1 - discount / 100)

  if (method === 'Pix') {
    amount = amount * (1 - settings.pixDiscount / 100)
  } else {
    const clamped = Math.min(12, Math.max(1, Math.round(installments))) as InstallmentNumber
    const rate =
      clamped <= 1
        ? settings.interestRates.cardCash
        : settings.interestRates[`${clamped}`]
    amount = amount * (1 + rate / 100)
  }

  return roundMoney(amount)
}
