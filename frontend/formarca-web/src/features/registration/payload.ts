import { maskPhone, maskTaxId, maskZipCode } from './masks'
import type { RegistrationDto, RegistrationPayload } from './types'
import { onlyDigits } from './validation'

export function toRegistrationPayload(
  values: RegistrationPayload,
): RegistrationPayload {
  return {
    name: values.name.trim(),
    email: values.email.trim(),
    phone: onlyDigits(values.phone),
    document: onlyDigits(values.document),
    legalRepresentativeName: values.legalRepresentativeName.trim(),
    legalRepresentativeDocument: onlyDigits(values.legalRepresentativeDocument),
    quotedAmount:
      values.quotedAmount == null || Number.isNaN(Number(values.quotedAmount))
        ? null
        : Number(values.quotedAmount),
    budgetNotes: values.budgetNotes?.trim() ? values.budgetNotes.trim() : null,
    address: {
      zipCode: onlyDigits(values.address.zipCode),
      street: values.address.street.trim(),
      number: values.address.number.trim(),
      complement: values.address.complement?.trim()
        ? values.address.complement.trim()
        : null,
      neighborhood: values.address.neighborhood.trim(),
      city: values.address.city.trim(),
      state: values.address.state.trim().toUpperCase(),
    },
  }
}

export function toFormValues(dto: RegistrationDto): RegistrationPayload {
  return {
    name: dto.name,
    email: dto.email,
    phone: maskPhone(dto.phone),
    document: maskTaxId(dto.document),
    legalRepresentativeName: dto.legalRepresentativeName ?? '',
    legalRepresentativeDocument: dto.legalRepresentativeDocument
      ? maskTaxId(dto.legalRepresentativeDocument)
      : '',
    quotedAmount: dto.quotedAmount ?? null,
    budgetNotes: dto.budgetNotes ?? '',
    address: {
      zipCode: maskZipCode(dto.address.zipCode),
      street: dto.address.street,
      number: dto.address.number,
      complement: dto.address.complement ?? '',
      neighborhood: dto.address.neighborhood,
      city: dto.address.city,
      state: dto.address.state,
    },
  }
}
