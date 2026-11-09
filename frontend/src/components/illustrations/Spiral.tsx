import { useId, type CSSProperties } from 'react'

// Spiral tel (taslak: docs/design/eskiz, <pattern id="spiral">): delikten geçip kartın üst kenarından kıvrılan
// koyu metal halkalar. Ebeveyn `position: relative` olmalı; tel üst kenardan ~14 px taşar, kart üstte ~22 px boşluk bırakır.
// Her örnek useId ile kendi desen kimliğini alır; aynı sayfadaki SVG'ler çakışmaz.
export function Spiral({ inset = 14, style }: { inset?: number; style?: CSSProperties }) {
  const id = `spiral-${useId().replace(/:/g, '')}`
  return (
    <svg
      aria-hidden
      height="26"
      style={{ position: 'absolute', top: -14, left: inset, width: `calc(100% - ${inset * 2}px)`, overflow: 'visible', pointerEvents: 'none', ...style }}
    >
      <defs>
        <pattern id={id} width="17" height="26" patternUnits="userSpaceOnUse">
          <ellipse cx="8.5" cy="19.5" rx="2.7" ry="2.5" fill="#3d3c38" />
          <path d="M6.8 19.5 C5.4 7.5 12.2 2.4 11.8 13.5" fill="none" stroke="#3a3935" strokeWidth="2" strokeLinecap="round" />
          <path d="M7.4 17 C6.6 8.6 10.9 5.2 11 10.5" fill="none" stroke="#d4d3cd" strokeWidth="0.7" strokeLinecap="round" />
        </pattern>
      </defs>
      <rect width="100%" height="26" fill={`url(#${id})`} />
    </svg>
  )
}

// El çizimi alt çizgi (başlıkların altında).
export function Scribble({ width = 250, style }: { width?: number; style?: CSSProperties }) {
  return (
    <svg className="scribble" width={width} height="10" viewBox="0 0 250 10" preserveAspectRatio="none" aria-hidden style={{ maxWidth: '100%', ...style }}>
      <path d="M2 6c40-4 90-5 140-2s80 3 106-1" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" vectorEffect="non-scaling-stroke" />
    </svg>
  )
}
