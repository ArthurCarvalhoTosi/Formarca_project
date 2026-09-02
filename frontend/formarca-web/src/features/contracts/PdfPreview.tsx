import { Alert, Spin } from 'antd'
import { useEffect, useMemo } from 'react'

type PdfPreviewProps = {
  blob: Blob | undefined
  isLoading: boolean
  isError: boolean
  errorMessage: string
  title: string
}

export function PdfPreview({
  blob,
  isLoading,
  isError,
  errorMessage,
  title,
}: PdfPreviewProps) {
  const url = useMemo(() => (blob ? URL.createObjectURL(blob) : null), [blob])

  useEffect(() => {
    return () => {
      if (url) URL.revokeObjectURL(url)
    }
  }, [url])

  if (isLoading) {
    return <Spin />
  }

  if (isError || !url) {
    return <Alert type="warning" showIcon message={errorMessage} />
  }

  return (
    <iframe
      title={title}
      src={url}
      style={{
        width: '100%',
        minHeight: 640,
        border: '1px solid var(--fm-border)',
        borderRadius: 8,
        background: '#fff',
      }}
    />
  )
}
