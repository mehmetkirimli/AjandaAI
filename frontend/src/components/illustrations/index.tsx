// Satır içi SVG illüstrasyonlar (fotoğraf/emoji yok). Hepsi dekoratif: aria-hidden.
import type { CSSProperties } from 'react'

interface P {
  width?: number | string
  style?: CSSProperties
}

// Giriş ekranı: gradyan kapaklı spiralli defter, yapışkan notlar, kalem, kupa.
export function NotebookHero({ width = 520, style }: P) {
  return (
    <svg width={width} viewBox="0 0 520 440" aria-hidden style={{ maxWidth: '100%', height: 'auto', ...style }}>
      <defs>
        <linearGradient id="nh-kapak" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#15803D" />
          <stop offset="0.35" stopColor="#0F766E" />
          <stop offset="0.7" stopColor="#7C3AED" />
          <stop offset="1" stopColor="#C2410C" />
        </linearGradient>
        <linearGradient id="nh-kalem" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#F97316" />
          <stop offset="1" stopColor="#FB923C" />
        </linearGradient>
      </defs>
      <ellipse cx="260" cy="410" rx="200" ry="16" fill="#000000" opacity="0.35" />
      <rect x="120" y="40" width="300" height="360" rx="22" fill="#0b0816" transform="rotate(-6 270 220)" />
      <g transform="rotate(-6 270 220)">
        <rect x="110" y="30" width="300" height="360" rx="22" fill="url(#nh-kapak)" />
        <rect x="110" y="30" width="300" height="360" rx="22" fill="none" stroke="#ffffff" strokeOpacity="0.25" strokeWidth="2" />
        <rect x="150" y="80" width="230" height="250" rx="14" fill="#ffffff" fillOpacity="0.12" />
        <line x1="170" y1="130" x2="360" y2="130" stroke="#ffffff" strokeOpacity="0.35" strokeWidth="2" />
        <line x1="170" y1="170" x2="360" y2="170" stroke="#ffffff" strokeOpacity="0.35" strokeWidth="2" />
        <line x1="170" y1="210" x2="330" y2="210" stroke="#ffffff" strokeOpacity="0.35" strokeWidth="2" />
        <line x1="170" y1="250" x2="300" y2="250" stroke="#ffffff" strokeOpacity="0.35" strokeWidth="2" />
        <circle cx="180" cy="290" r="10" fill="none" stroke="#86EFAC" strokeWidth="3" />
        <path d="M175 290 l4 4 l8 -9" fill="none" stroke="#86EFAC" strokeWidth="3" strokeLinecap="round" />
        <line x1="200" y1="290" x2="320" y2="290" stroke="#ffffff" strokeOpacity="0.5" strokeWidth="3" strokeLinecap="round" />
        <g fill="#e9e5f5" stroke="#5b4e86" strokeWidth="2">
          {[60, 100, 140, 180, 220, 260, 300, 340].map((y) => (
            <rect key={y} x="96" y={y} width="30" height="12" rx="6" />
          ))}
        </g>
        <rect x="380" y="70" width="18" height="70" rx="3" fill="#F97316" />
      </g>
      <g transform="rotate(12 430 120)">
        <rect x="400" y="60" width="96" height="96" rx="8" fill="#A78BFA" />
        <path d="M400 140 l0 16 l16 0 z" fill="#7C3AED" />
        <line x1="414" y1="88" x2="480" y2="88" stroke="#ffffff" strokeWidth="3" strokeLinecap="round" />
        <line x1="414" y1="106" x2="466" y2="106" stroke="#ffffff" strokeWidth="3" strokeLinecap="round" />
      </g>
      <g transform="rotate(-10 60 300)">
        <rect x="20" y="260" width="84" height="84" rx="8" fill="#2DD4BF" />
        <line x1="34" y1="286" x2="90" y2="286" stroke="#0b3b36" strokeWidth="3" strokeLinecap="round" />
        <line x1="34" y1="304" x2="76" y2="304" stroke="#0b3b36" strokeWidth="3" strokeLinecap="round" />
      </g>
      <g transform="rotate(38 380 330)">
        <rect x="300" y="322" width="170" height="18" rx="9" fill="url(#nh-kalem)" />
        <path d="M470 322 l26 9 l-26 9 z" fill="#fde7d4" />
        <rect x="300" y="322" width="22" height="18" rx="4" fill="#4ADE80" />
      </g>
      <g>
        <path d="M40 120 q0 -6 6 -6 h52 q6 0 6 6 v34 q0 22 -32 22 q-32 0 -32 -22 z" fill="#C2410C" />
        <path d="M104 128 q18 0 18 14 q0 14 -18 14" fill="none" stroke="#C2410C" strokeWidth="6" />
        <path d="M58 100 q6 -10 0 -20 M74 100 q6 -10 0 -20" fill="none" stroke="#ffffff" strokeOpacity="0.6" strokeWidth="3" strokeLinecap="round" />
      </g>
      <path d="M470 220 l4 10 l10 4 l-10 4 l-4 10 l-4 -10 l-10 -4 l10 -4 z" fill="#4ADE80" />
      <path d="M70 40 l3 7 l7 3 l-7 3 l-3 7 l-3 -7 l-7 -3 l7 -3 z" fill="#FDBA74" />
    </svg>
  )
}

// Boş gün: eğik yapışkan not + turuncu nokta.
export function EmptyPage({ width = 84, style }: P) {
  return (
    <svg width={width} viewBox="0 0 84 70" aria-hidden style={{ height: 'auto', ...style }}>
      <rect x="14" y="8" width="56" height="54" rx="6" fill="#2DD4BF" transform="rotate(-6 42 35)" />
      <rect x="20" y="22" width="34" height="3" rx="1.5" fill="#0b3b36" transform="rotate(-6 42 35)" />
      <rect x="20" y="32" width="24" height="3" rx="1.5" fill="#0b3b36" transform="rotate(-6 42 35)" />
      <circle cx="62" cy="12" r="6" fill="#FB923C" />
    </svg>
  )
}

// Hafta sonu boş gün: buharı tüten kupa.
export function CoffeeCup({ width = 56, style }: P) {
  return (
    <svg width={width} viewBox="0 0 56 44" aria-hidden style={{ height: 'auto', flex: 'none', ...style }}>
      <path d="M10 14q0-4 4-4h24q4 0 4 4v12q0 12-16 12q-16 0-16-12z" fill="#C2410C" />
      <path d="M42 18q8 0 8 6q0 6-8 6" fill="none" stroke="#C2410C" strokeWidth="4" />
      <path d="M20 6q3-3 0-6M28 6q3-3 0-6" fill="none" stroke="#ffffff" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

// Profil kapağı: not kağıdı + kalem.
export function NoteAndPencil({ width = 150, style }: P) {
  return (
    <svg width={width} viewBox="0 0 150 120" aria-hidden style={{ height: 'auto', flex: 'none', ...style }}>
      <g transform="rotate(10 80 50)">
        <rect x="40" y="10" width="80" height="80" rx="8" fill="#ffffff" fillOpacity="0.9" />
        <line x1="54" y1="36" x2="106" y2="36" stroke="#7C3AED" strokeWidth="4" strokeLinecap="round" />
        <line x1="54" y1="54" x2="92" y2="54" stroke="#7C3AED" strokeWidth="4" strokeLinecap="round" />
      </g>
      <g transform="rotate(-30 60 100)">
        <rect x="10" y="94" width="110" height="14" rx="7" fill="#FB923C" />
        <path d="M120 94 l18 7 l-18 7 z" fill="#fde7d4" />
      </g>
    </svg>
  )
}

// 404: yırtık sayfa + soru işareti.
export function LostPage({ width = 160, style }: P) {
  return (
    <svg width={width} viewBox="0 0 160 130" aria-hidden style={{ height: 'auto', ...style }}>
      <ellipse cx="80" cy="120" rx="56" ry="6" fill="#000" opacity="0.35" />
      <g transform="rotate(-8 80 60)">
        <path d="M36 10 h88 v84 l-10 -6 l-10 8 l-10 -8 l-10 8 l-10 -8 l-10 8 l-10 -8 l-10 8 l-8 -6 z" fill="#A78BFA" />
        <line x1="50" y1="34" x2="108" y2="34" stroke="#ffffff" strokeOpacity="0.7" strokeWidth="3" strokeLinecap="round" />
        <line x1="50" y1="50" x2="96" y2="50" stroke="#ffffff" strokeOpacity="0.7" strokeWidth="3" strokeLinecap="round" />
      </g>
      <circle cx="128" cy="26" r="16" fill="#FB923C" />
      <path d="M123 22 q0 -6 5 -6 q5 0 5 5 q0 4 -5 5 v3" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" />
      <circle cx="128" cy="35" r="1.8" fill="#fff" />
      <path d="M22 70 l3 7 l7 3 l-7 3 l-3 7 l-3 -7 l-7 -3 l7 -3 z" fill="#4ADE80" />
    </svg>
  )
}

// E-posta kontrolü: zarf + onay.
export function MailCheck({ width = 140, style }: P) {
  return (
    <svg width={width} viewBox="0 0 140 110" aria-hidden style={{ height: 'auto', ...style }}>
      <ellipse cx="70" cy="102" rx="50" ry="6" fill="#000" opacity="0.35" />
      <g transform="rotate(-6 70 55)">
        <rect x="18" y="22" width="104" height="70" rx="10" fill="#0F766E" />
        <path d="M18 30 l52 34 l52 -34" fill="none" stroke="#5EEAD4" strokeWidth="4" strokeLinejoin="round" />
      </g>
      <circle cx="112" cy="26" r="18" fill="#15803D" stroke="#120d22" strokeWidth="4" />
      <path d="M104 26 l6 6 l10 -11" fill="none" stroke="#fff" strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M20 8 l3 7 l7 3 l-7 3 l-3 7 l-3 -7 l-7 -3 l7 -3 z" fill="#FDBA74" />
    </svg>
  )
}

// Spiral halkalar (beyaz tel).
export function Spiral({ count = 5, style }: { count?: number; style?: CSSProperties }) {
  return (
    <div className="spiral" aria-hidden style={style}>
      {Array.from({ length: count }, (_, i) => (
        <span key={i} />
      ))}
    </div>
  )
}
