export type AddressDto = {
  zipCode: string
  street: string
  number: string
  complement?: string | null
  neighborhood: string
  city: string
  state: string
}

export type RegistrationDto = {
  id: string
  name: string
  email: string
  phone: string
  document: string
  address: AddressDto
  legalRepresentativeName?: string | null
  legalRepresentativeDocument?: string | null
  quotedAmount?: number | null
  budgetNotes?: string | null
  brandName?: string | null
  brandDescription?: string | null
  slug?: string
  status?: string
}

export type RegistrationPayload = {
  name: string
  email: string
  phone: string
  document: string
  address: AddressDto
  legalRepresentativeName: string
  legalRepresentativeDocument: string
  quotedAmount?: number | null
  budgetNotes?: string | null
}

export type ApproveTenantRequest = {
  password: string
}
