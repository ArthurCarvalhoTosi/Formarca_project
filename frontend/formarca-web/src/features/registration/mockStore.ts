import { ApiError } from '@/shared/api/types'
import { MOCK_TENANTS } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import type { Tenant } from '@/features/admin/types'
import type {
  AddressDto,
  RegistrationDto,
  RegistrationPayload,
} from './types'
import { toRegistrationPayload } from './payload'
import { onlyDigits, slugify } from './validation'

export type MockTenantStatus = 'Pending' | 'Active' | 'Inactive' | 'Suspended'

export type MockTenantRecord = {
  id: string
  name: string
  slug: string
  document: string
  status: MockTenantStatus
  email: string
  phone: string
  brandName: string
  brandDescription: string
  address: AddressDto
  isDefault?: boolean
  createdAt: string
}

const demoAddress: AddressDto = {
  zipCode: '01310100',
  street: 'Avenida Paulista',
  number: '1000',
  complement: 'Conj. 101',
  neighborhood: 'Bela Vista',
  city: 'São Paulo',
  state: 'SP',
}

const acmeAddress: AddressDto = {
  zipCode: '20040020',
  street: 'Avenida Rio Branco',
  number: '156',
  complement: null,
  neighborhood: 'Centro',
  city: 'Rio de Janeiro',
  state: 'RJ',
}

const mockTenantRecords: MockTenantRecord[] = [
  {
    id: MOCK_TENANTS[0].id,
    name: MOCK_TENANTS[0].name,
    slug: MOCK_TENANTS[0].slug ?? 'demo',
    document: '39053344705',
    status: 'Active',
    email: 'demo@formarca.com',
    phone: '11988880001',
    brandName: 'Demo Marcas',
    brandDescription: 'Marca de demonstração da fundação ForMarca.',
    address: demoAddress,
    isDefault: true,
    createdAt: '2026-01-01T00:00:00.000Z',
  },
  {
    id: MOCK_TENANTS[1].id,
    name: MOCK_TENANTS[1].name,
    slug: MOCK_TENANTS[1].slug ?? 'acme',
    document: '12345678000195',
    status: 'Active',
    email: 'contato@acmemarcas.com',
    phone: '21977770002',
    brandName: 'Acme',
    brandDescription: 'Produtos e serviços de marca da Acme.',
    address: acmeAddress,
    createdAt: '2026-01-02T00:00:00.000Z',
  },
]

function toRegistrationDto(record: MockTenantRecord): RegistrationDto {
  return {
    id: record.id,
    name: record.name,
    email: record.email,
    phone: record.phone,
    document: record.document,
    address: { ...record.address },
    brandName: record.brandName,
    brandDescription: record.brandDescription,
    slug: record.slug,
    status: record.status,
  }
}

function toAdminTenant(record: MockTenantRecord): Tenant {
  return {
    id: record.id,
    name: record.name,
    slug: record.slug,
    document: record.document,
    email: record.email,
    status: record.status,
    createdAt: record.createdAt,
  }
}

export type MockCustomerTenantRow = {
  id: string
  name: string
  slug: string
  document: string
  email: string
  status: MockTenantStatus
}

function uniqueSlug(base: string): string {
  const root = slugify(base) || 'tenant'
  let slug = root
  let n = 2
  while (mockTenantRecords.some((record) => record.slug === slug)) {
    slug = `${root}-${n}`
    n += 1
  }
  return slug
}

function assertUniqueContact(
  email: string,
  document: string,
  exceptId?: string,
): void {
  const emailNorm = email.trim().toLowerCase()
  const doc = onlyDigits(document)
  const clash = mockTenantRecords.find(
    (record) =>
      record.id !== exceptId &&
      (record.email.toLowerCase() === emailNorm ||
        onlyDigits(record.document) === doc),
  )
  if (!clash) return
  if (clash.email.toLowerCase() === emailNorm) {
    throw new ApiError(409, 'Email already registered')
  }
  throw new ApiError(409, 'Document already registered')
}

export function mockListTenants(): Tenant[] {
  return mockTenantRecords.map(toAdminTenant)
}

export function mockListCustomerTenants(): MockCustomerTenantRow[] {
  return mockTenantRecords.map((record) => ({
    id: record.id,
    name: record.name,
    slug: record.slug,
    document: record.document,
    email: record.email,
    status: record.status,
  }))
}

export function mockGetRegistrationById(id: string): RegistrationDto | null {
  const record = mockTenantRecords.find((item) => item.id === id)
  if (!record) return null
  return toRegistrationDto(record)
}

export function mockUpdateRegistrationById(
  id: string,
  payload: RegistrationPayload,
): RegistrationDto {
  const record = mockTenantRecords.find((item) => item.id === id)
  if (!record) {
    throw new ApiError(404, 'Registration not found')
  }
  const body = toRegistrationPayload(payload)
  assertUniqueContact(body.email, body.document, record.id)
  record.name = body.name
  record.email = body.email
  record.phone = body.phone
  record.document = body.document
  record.brandName = body.brandName
  record.brandDescription = body.brandDescription
  record.address = body.address
  return toRegistrationDto(record)
}

export function mockSubmitRegistration(payload: RegistrationPayload): void {
  const body = toRegistrationPayload(payload)
  assertUniqueContact(body.email, body.document)
  mockTenantRecords.push({
    id: crypto.randomUUID(),
    name: body.name,
    slug: uniqueSlug(body.name),
    document: body.document,
    status: 'Pending',
    email: body.email,
    phone: body.phone,
    brandName: body.brandName,
    brandDescription: body.brandDescription,
    address: body.address,
    createdAt: new Date().toISOString(),
  })
}

export function mockGetCurrentRegistration(): RegistrationDto | null {
  const tenantId = tokenStorage.getActiveTenantId()
  if (!tenantId) return null
  const record = mockTenantRecords.find((item) => item.id === tenantId)
  if (!record || record.status !== 'Active') return null
  return toRegistrationDto(record)
}

export function mockUpdateRegistration(
  payload: RegistrationPayload,
): RegistrationDto {
  const tenantId = tokenStorage.getActiveTenantId()
  if (!tenantId) {
    throw new ApiError(404, 'No active tenant')
  }
  const record = mockTenantRecords.find((item) => item.id === tenantId)
  if (!record || record.status !== 'Active') {
    throw new ApiError(404, 'Registration not found')
  }
  const body = toRegistrationPayload(payload)
  assertUniqueContact(body.email, body.document, record.id)
  record.name = body.name
  record.email = body.email
  record.phone = body.phone
  record.document = body.document
  record.brandName = body.brandName
  record.brandDescription = body.brandDescription
  record.address = body.address
  return toRegistrationDto(record)
}

export function mockApproveTenant(id: string, password: string): void {
  if (password.trim().length < 8) {
    throw new ApiError(400, 'Password must be at least 8 characters')
  }
  const record = mockTenantRecords.find((item) => item.id === id)
  if (!record) {
    throw new ApiError(404, 'Tenant not found')
  }
  if (record.status !== 'Pending') {
    throw new ApiError(409, 'Tenant is not pending')
  }
  record.status = 'Active'
}

export function mockRejectTenant(id: string): void {
  const record = mockTenantRecords.find((item) => item.id === id)
  if (!record) {
    throw new ApiError(404, 'Tenant not found')
  }
  if (record.status !== 'Pending') {
    throw new ApiError(409, 'Tenant is not pending')
  }
  record.status = 'Inactive'
}
