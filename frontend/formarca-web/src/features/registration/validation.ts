import { BRAZIL_UFS, type BrazilUf } from '@/shared/i18n'

export function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

export function isBrazilUf(value: string): value is BrazilUf {
  return (BRAZIL_UFS as readonly string[]).includes(value.toUpperCase())
}

export function isValidCpf(value: string): boolean {
  const cpf = onlyDigits(value)
  if (cpf.length !== 11) return false
  if (/^(\d)\1{10}$/.test(cpf)) return false

  let sum = 0
  for (let i = 0; i < 9; i += 1) {
    sum += Number(cpf[i]) * (10 - i)
  }
  let rest = (sum * 10) % 11
  if (rest === 10) rest = 0
  if (rest !== Number(cpf[9])) return false

  sum = 0
  for (let i = 0; i < 10; i += 1) {
    sum += Number(cpf[i]) * (11 - i)
  }
  rest = (sum * 10) % 11
  if (rest === 10) rest = 0
  return rest === Number(cpf[10])
}

export function isValidCnpj(value: string): boolean {
  const cnpj = onlyDigits(value)
  if (cnpj.length !== 14) return false
  if (/^(\d)\1{13}$/.test(cnpj)) return false

  const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]

  let sum = 0
  for (let i = 0; i < 12; i += 1) {
    sum += Number(cnpj[i]) * weights1[i]
  }
  let rest = sum % 11
  const digit1 = rest < 2 ? 0 : 11 - rest
  if (digit1 !== Number(cnpj[12])) return false

  sum = 0
  for (let i = 0; i < 13; i += 1) {
    sum += Number(cnpj[i]) * weights2[i]
  }
  rest = sum % 11
  const digit2 = rest < 2 ? 0 : 11 - rest
  return digit2 === Number(cnpj[13])
}

export function isValidTaxId(value: string): boolean {
  const digits = onlyDigits(value)
  if (digits.length === 11) return isValidCpf(digits)
  if (digits.length === 14) return isValidCnpj(digits)
  return false
}

export function isValidBrPhone(value: string): boolean {
  const digits = onlyDigits(value)
  return digits.length === 10 || digits.length === 11
}

export function isValidZipCode(value: string): boolean {
  return onlyDigits(value).length === 8
}

export function slugify(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80)
}
