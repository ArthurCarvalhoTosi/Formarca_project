export function unwrapPngBase64(value: string): string {
  const trimmed = value.trim()
  const marker = 'base64,'
  const index = trimmed.indexOf(marker)
  return index >= 0 ? trimmed.slice(index + marker.length) : trimmed
}
