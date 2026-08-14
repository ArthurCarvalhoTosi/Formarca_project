type BrandLogoProps = {
  height?: number
  compact?: boolean
  className?: string
}

export function BrandLogo({
  height = 32,
  compact = false,
  className,
}: BrandLogoProps) {
  if (compact) {
    return (
      <img
        src="/favicon.svg"
        alt="ForMarca"
        className={className}
        height={height}
        width={height}
        style={{ height, width: height, display: 'block' }}
      />
    )
  }

  return (
    <img
      src="/brand/logo.png"
      alt="ForMarca"
      className={className}
      height={height}
      style={{ height, width: 'auto', display: 'block' }}
    />
  )
}
