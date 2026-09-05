import { parseClasses } from '@/shared/money'
import type { Contract } from './types'

function pdfEscape(text: string): string {
  return text
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replace(/[^\x20-\x7E]/g, '?')
    .replaceAll('\\', '\\\\')
    .replaceAll('(', '\\(')
    .replaceAll(')', '\\)')
}

function renderPlainPdf(lines: string[]): Blob {
  const wrapped: string[] = []
  const width = 92
  for (const raw of lines) {
    const line = raw.length === 0 ? ' ' : raw
    for (let i = 0; i < line.length; i += width) {
      wrapped.push(line.slice(i, i + width))
    }
  }
  const maxLines = 50
  const visible = wrapped.slice(0, maxLines)
  if (wrapped.length > maxLines) visible.push('...')

  const commands = visible.map((line, index) => {
    const y = 770 - index * 14
    return `BT /F1 10 Tf 40 ${y} Td (${pdfEscape(line)}) Tj ET`
  })
  const stream = commands.join('\n')

  const objects = [
    '1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n',
    '2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n',
    '3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n',
    `4 0 obj\n<< /Length ${stream.length} >>\nstream\n${stream}\nendstream\nendobj\n`,
    '5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n',
  ]

  let pdf = '%PDF-1.4\n'
  const offsets: number[] = []
  for (const object of objects) {
    offsets.push(pdf.length)
    pdf += object
  }
  const xrefStart = pdf.length
  pdf += `xref\n0 ${objects.length + 1}\n`
  pdf += '0000000000 65535 f \n'
  for (const offset of offsets) {
    pdf += `${offset.toString().padStart(10, '0')} 00000 n \n`
  }
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xrefStart}\n%%EOF\n`
  return new Blob([pdf], { type: 'application/pdf' })
}

export function buildContractPdf(contract: Contract): Blob {
  const classes = parseClasses(contract.classes).join(', ')
  const clientSigned = Boolean(contract.clientSignaturePng || contract.clientSignedAt)
  const formarcaSigned =
    contract.status === 'Issued' || contract.status === 'Signed'
  return renderPlainPdf([
    `Contrato ${contract.code}`,
    `Status: ${contract.status}`,
    `Contratante: ${contract.companyName ?? ''}`,
    `CNPJ: ${contract.companyDocument}`,
    `Endereco: ${contract.street ?? ''}, ${contract.number ?? ''} - ${contract.city ?? ''}/${contract.state ?? ''} CEP ${contract.zipCode ?? ''}`,
    `Marca: ${contract.brandName}`,
    `Classes: ${classes}`,
    `Valor: ${contract.amount}`,
    `Prazo: ${contract.minimumTerm ?? ''}`,
    '',
    ...(contract.bodyMarkdown ?? '').split('\n'),
    '',
    formarcaSigned
      ? 'Assinatura da contratada (FOR MARCA LTDA): registrada'
      : 'Assinatura da contratada (FOR MARCA LTDA): pendente',
    formarcaSigned && contract.formarcaSignedAt
      ? `Assinado eletronicamente em: ${contract.formarcaSignedAt}`
      : '',
    clientSigned
      ? 'Assinatura do contratante: registrada'
      : 'Assinatura do contratante: pendente',
    contract.clientSignedAt ? `Assinado em: ${contract.clientSignedAt}` : '',
  ])
}
