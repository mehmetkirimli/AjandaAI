// Satır içi SVG illüstrasyonlar: tek renk grafit kalem çizimi (ADR 0021 Karar 5f). Hepsi dekoratif: aria-hidden.
import type { CSSProperties, ReactNode } from 'react'

export { Scribble, Spiral } from './Spiral'

interface P {
  width?: number | string
  style?: CSSProperties
  // Çizgi rengi; varsayılan grafit. Boş durumlarda silik gri (#8a8984).
  color?: string
}

function Pencil({ viewBox, width, style, color = '#2b2a27', strokeWidth = 1.5, children }: P & { viewBox: string; strokeWidth?: number; children: ReactNode }) {
  return (
    <svg
      width={width}
      viewBox={viewBox}
      fill="none"
      stroke={color}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
      style={{ maxWidth: '100%', height: 'auto', flex: 'none', ...style }}
    >
      {children}
    </svg>
  )
}

// Giriş ekranı: açık spiralli defter, kalem, kupa, yapışkan not.
export function NotebookHero({ width = 480, style }: P) {
  return (
    <Pencil viewBox="0 0 480 400" width={width} style={style} strokeWidth={1.8}>
      {/* gölge çizgisi */}
      <path d="M70 372c90 6 250 6 340-2" strokeWidth="1.2" strokeDasharray="4 6" />
      {/* defter */}
      <g transform="rotate(-4 240 200)">
        <path d="M110 52c60-3 170-2 240 1c3 100 1 200 2 296c-70 3-180 3-242-1c-3-98-4-198 0-296z" fill="#fcfcfa" />
        <path d="M114 352l6 6c70 3 170 3 238 0l-4-8" />
        {[80, 112, 144, 176, 208, 240, 272, 304, 336].map((y) => (
          <path key={y} d={`M98 ${y}c-6-8 4-16 18-12`} strokeWidth="2.2" />
        ))}
        <path d="M150 104c40-2 110-2 170 0" />
        <path d="M150 140c40-1 100-1 150 1" strokeWidth="1.2" />
        <path d="M150 176c40-1 90-1 130 0" strokeWidth="1.2" />
        <path d="M152 222l7 7l13-15" strokeWidth="2" />
        <path d="M186 224c30-1 70-1 104 0" strokeWidth="1.2" />
        <path d="M152 262l7 7l13-15" strokeWidth="2" />
        <path d="M186 264c26-1 56-1 84 1" strokeWidth="1.2" />
        <rect x="150" y="298" width="16" height="16" rx="2" strokeWidth="1.4" />
        <path d="M186 306c22-1 44-1 66 0" strokeWidth="1.2" />
      </g>
      {/* kalem */}
      <g transform="rotate(36 380 300)">
        <path d="M296 292h150l22 8l-22 8h-150z" fill="#fcfcfa" />
        <path d="M446 292v16M318 292v16" strokeWidth="1.2" />
        <path d="M462 298l6 2l-6 2" fill="#2b2a27" />
      </g>
      {/* yapışkan not */}
      <g transform="rotate(9 410 110)">
        <path d="M368 66c28-1 56-1 84 1c1 28 0 56 1 82c-28 1-56 1-84-1c-1-26-2-54-1-82z" fill="#fcfcfa" />
        <path d="M382 94c18-1 38-1 56 0M382 112c14-1 28-1 40 0" strokeWidth="1.2" />
      </g>
      {/* kupa */}
      <g>
        <path d="M30 120c18-1 44-1 60 0c1 16 0 32-4 42c-8 12-44 12-52 0c-4-10-5-26-4-42z" fill="#fcfcfa" />
        <path d="M90 128c16 0 16 22 0 22" />
        <path d="M48 104c4-6-2-10 2-16M64 104c4-6-2-10 2-16" strokeWidth="1.3" />
      </g>
      {/* yıldız işaretleri */}
      <path d="M440 220l4 10M437 226l10-2" strokeWidth="1.3" />
      <path d="M58 40l4 10M55 46l10-2" strokeWidth="1.3" />
    </Pencil>
  )
}

// Boş gün: sayfa + satırlar + kalem çizgisi (taslaktaki ikon).
export function EmptyPage({ width = 56, style, color = '#8a8984' }: P) {
  return (
    <Pencil viewBox="0 0 56 48" width={width} style={style} color={color} strokeWidth={1.4}>
      <path d="M10 10c10-1 26-1 36 0c1 10 0 20 .5 28c-10 1-26 1-36 0c-1-9-1-19-.5-28z" />
      <path d="M16 20h22M16 27h14" />
      <path d="M38 34l8-8" />
    </Pencil>
  )
}

// Hafta sonu: buharı tüten kupa.
export function CoffeeCup({ width = 46, style, color = '#6b6a65' }: P) {
  return (
    <Pencil viewBox="0 0 46 38" width={width} style={style} color={color} strokeWidth={1.4}>
      <path d="M8 12c8-.5 18-.5 24 0c.5 6 0 12-12 15c-12-3-12.5-9-12-15z" />
      <path d="M32 15c6 0 6 7 0 7" />
      <path d="M16 7c2-2 0-4 2-6M23 7c2-2 0-4 2-6" />
    </Pencil>
  )
}

// Profil kapağı: not kâğıdı + kalem.
export function NoteAndPencil({ width = 140, style, color }: P) {
  return (
    <Pencil viewBox="0 0 150 120" width={width} style={style} color={color} strokeWidth={1.6}>
      <g transform="rotate(8 80 50)">
        <path d="M40 12c26-1 54-1 80 1c1 26 0 52 1 76c-26 1-54 1-80-1c-1-24-2-50-1-76z" fill="#fcfcfa" />
        <path d="M54 36c18-1 34-1 52 0M54 54c12-1 26-1 38 0M54 72c8-1 16-1 24 0" strokeWidth="1.2" />
      </g>
      <g transform="rotate(-28 60 100)">
        <path d="M12 94h104l18 7l-18 7h-104z" fill="#fcfcfa" />
        <path d="M116 94v14M30 94v14" strokeWidth="1.1" />
      </g>
    </Pencil>
  )
}

// 404: yırtık sayfa + soru işareti.
export function LostPage({ width = 150, style, color }: P) {
  return (
    <Pencil viewBox="0 0 160 130" width={width} style={style} color={color} strokeWidth={1.6}>
      <path d="M40 122c30 3 60 3 84-1" strokeWidth="1.1" strokeDasharray="3 5" />
      <g transform="rotate(-7 80 60)">
        <path d="M36 12c30-1 60-1 88 1c1 26 0 54 0 80l-10-6l-10 8l-10-8l-10 8l-10-8l-10 8l-10-8l-10 8l-8-6c-1-26-1-52 0-77z" fill="#fcfcfa" />
        <path d="M50 36c20-1 38-1 58 0M50 52c16-1 30-1 46 0" strokeWidth="1.2" />
      </g>
      <path d="M121 20c0-7 14-8 14 0c0 6-7 6-7 12" strokeWidth="2" />
      <path d="M128 40v1" strokeWidth="2.6" />
    </Pencil>
  )
}

// E-posta kontrolü: zarf + onay.
export function MailCheck({ width = 130, style, color }: P) {
  return (
    <Pencil viewBox="0 0 140 110" width={width} style={style} color={color} strokeWidth={1.6}>
      <path d="M30 102c26 3 54 3 80-1" strokeWidth="1.1" strokeDasharray="3 5" />
      <g transform="rotate(-5 70 55)">
        <path d="M18 24c34-1 70-1 104 1c1 22 0 46 1 66c-34 1-70 1-104-1c-1-22-2-44-1-66z" fill="#fcfcfa" />
        <path d="M19 30l51 34l52-34" />
      </g>
      <circle cx="112" cy="24" r="15" fill="#fcfcfa" />
      <path d="M105 24l5 5l9-10" strokeWidth="2" />
    </Pencil>
  )
}
