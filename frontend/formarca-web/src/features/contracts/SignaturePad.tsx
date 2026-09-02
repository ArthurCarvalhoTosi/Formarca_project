import { Button, Typography } from 'antd'
import { useEffect, useRef, type PointerEvent as ReactPointerEvent } from 'react'
import { useI18n } from '@/shared/i18n'
import { unwrapPngBase64 } from './signature'

type SignaturePadProps = {
  onChange: (pngBase64: string | null) => void
}

export function SignaturePad({ onChange }: SignaturePadProps) {
  const { t } = useI18n()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawingRef = useRef(false)
  const dirtyRef = useRef(false)
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  const resetCanvas = () => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ratio = window.devicePixelRatio || 1
    const width = canvas.clientWidth || 600
    const height = canvas.clientHeight || 180
    canvas.width = Math.floor(width * ratio)
    canvas.height = Math.floor(height * ratio)
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.setTransform(1, 0, 0, 1, 0, 0)
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(0, 0, canvas.width, canvas.height)
    ctx.setTransform(ratio, 0, 0, ratio, 0, 0)
    ctx.lineWidth = 2
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.strokeStyle = '#111111'
    dirtyRef.current = false
    onChangeRef.current(null)
  }

  useEffect(() => {
    resetCanvas()
  }, [])

  const pointFromEvent = (event: ReactPointerEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current
    if (!canvas) return { x: 0, y: 0 }
    const rect = canvas.getBoundingClientRect()
    return { x: event.clientX - rect.left, y: event.clientY - rect.top }
  }

  const emit = () => {
    const canvas = canvasRef.current
    if (!canvas || !dirtyRef.current) {
      onChangeRef.current(null)
      return
    }
    onChangeRef.current(unwrapPngBase64(canvas.toDataURL('image/png')))
  }

  return (
    <div>
      <Typography.Text strong>{t('contracts.signHere')}</Typography.Text>
      <Typography.Paragraph type="secondary" style={{ marginTop: 4 }}>
        {t('contracts.signHint')}
      </Typography.Paragraph>
      <canvas
        ref={canvasRef}
        style={{
          width: '100%',
          height: 180,
          touchAction: 'none',
          cursor: 'crosshair',
          background: '#fff',
          border: '1px dashed rgba(36, 36, 36, 0.25)',
          borderRadius: 8,
          display: 'block',
        }}
        onPointerDown={(event) => {
          event.currentTarget.setPointerCapture(event.pointerId)
          drawingRef.current = true
          const ctx = canvasRef.current?.getContext('2d')
          const point = pointFromEvent(event)
          ctx?.beginPath()
          ctx?.moveTo(point.x, point.y)
        }}
        onPointerMove={(event) => {
          if (!drawingRef.current) return
          const ctx = canvasRef.current?.getContext('2d')
          const point = pointFromEvent(event)
          ctx?.lineTo(point.x, point.y)
          ctx?.stroke()
          dirtyRef.current = true
        }}
        onPointerUp={() => {
          drawingRef.current = false
          emit()
        }}
        onPointerCancel={() => {
          drawingRef.current = false
          emit()
        }}
      />
      <Button style={{ marginTop: 8 }} onClick={resetCanvas}>
        {t('contracts.clearSignature')}
      </Button>
    </div>
  )
}
