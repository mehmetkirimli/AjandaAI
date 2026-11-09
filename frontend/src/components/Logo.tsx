// Kalemle çizilmiş spiralli defter + onay işareti logosu (taslak: docs/design/eskiz).
export function LogoMark({ size = 34 }: { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 34 34"
      fill="none"
      stroke="var(--graphite)"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
      style={{ flex: 'none' }}
    >
      <path d="M9 4.5c5-.4 13-.3 18.5.2c.6 8-.2 17 .1 25c-6 .5-13 .4-18.4-.2c-.4-8-.6-17-.2-25z" />
      <path d="M5 9h5M5 15h5M5 21h5M5 27h5" />
      <path d="M14 17.5l3 3l6-7" />
    </svg>
  )
}

interface Props {
  // compact: mobil başlık; large: giriş kartı (alt yazılı).
  size?: 'compact' | 'default' | 'large'
}

export function Logo({ size = 'default' }: Props) {
  const mark = size === 'large' ? 40 : size === 'compact' ? 28 : 34
  const fz = size === 'large' ? 30 : size === 'compact' ? 22 : 26
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 10 }}>
      <LogoMark size={mark} />
      <span style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontFamily: 'var(--font-hand)', fontSize: fz, fontWeight: 700, lineHeight: 1 }}>AjandaAI</span>
        {size === 'large' && (
          <span className="hand muted" style={{ fontSize: 17 }}>
            eskiz defterin
          </span>
        )}
      </span>
    </span>
  )
}
