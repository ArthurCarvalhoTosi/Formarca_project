import { onlyDigits } from './validation'

export type ViaCepResult = {
  street: string
  neighborhood: string
  city: string
  state: string
}

type ViaCepResponse = {
  erro?: boolean
  logradouro?: string
  bairro?: string
  localidade?: string
  uf?: string
}

export async function lookupZipCode(
  zipCode: string,
): Promise<ViaCepResult | null> {
  const cep = onlyDigits(zipCode)
  if (cep.length !== 8) return null

  const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`)
  if (!response.ok) return null

  const data = (await response.json()) as ViaCepResponse
  if (data.erro) return null

  return {
    street: data.logradouro ?? '',
    neighborhood: data.bairro ?? '',
    city: data.localidade ?? '',
    state: data.uf ?? '',
  }
}
