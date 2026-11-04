// Spiralli defter + onay işareti logosu (taslak: docs/design/ozel-defter).
export function LogoMark({ size = 38, glow = false, plain = false }: { size?: number; glow?: boolean; plain?: boolean }) {
  return (
    <span
      aria-hidden
      style={{
        width: size,
        height: size,
        flex: 'none',
        borderRadius: Math.round(size * 0.29),
        background: plain ? 'rgba(255,255,255,0.2)' : 'var(--grad-rainbow-diag)',
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        boxShadow: glow ? '0 8px 24px rgba(124,58,237,0.45)' : undefined,
      }}
    >
      <svg
        width={Math.round(size * 0.53)}
        height={Math.round(size * 0.53)}
        viewBox="0 0 24 24"
        fill="none"
        stroke="#ffffff"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="5" y="3" width="15" height="18" rx="2" />
        <path d="M3 7h3M3 12h3M3 17h3" />
        <path d="M10 12l2 2l4 -4" />
      </svg>
    </span>
  )
}

interface Props {
  // compact: mobil başlık; large: giriş kartı (alt yazılı).
  size?: 'compact' | 'default' | 'large'
  plain?: boolean
}

export function Logo({ size = 'default', plain = false }: Props) {
  const mark = size === 'large' ? 48 : size === 'compact' ? 36 : 38
  const fz = size === 'large' ? 24 : size === 'compact' ? 18 : 20
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: size === 'large' ? 12 : 10 }}>
      <LogoMark size={mark} glow={size === 'large'} plain={plain} />
      <span style={{ display: 'flex', flexDirection: 'column' }}>
        <span style={{ fontSize: fz, fontWeight: 800, letterSpacing: '-0.02em', lineHeight: 1.1 }}>AjandaAI</span>
        {size === 'large' && (
          <span className="hand" style={{ fontSize: 20, opacity: 0.85 }}>
            dijital defterin
          </span>
        )}
      </span>
    </span>
  )
}
