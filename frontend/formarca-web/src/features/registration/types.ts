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
  brandName: string
  brandDescription: string
  slug?: string
  status?: string
}

export type RegistrationPayload = {
  name: string
  email: string
  phone: string
  document: string
  address: AddressDto
  brandName: string
  brandDescription: string
}

export type ApproveTenantRequest = {
  password: string
}
