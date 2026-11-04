import type { ReactNode } from 'react'
import { NotebookHero } from './illustrations'
import { Logo } from './Logo'
import './auth.css'

interface Props {
  title: string
  subtitle?: string
  // Kartın üstünde (başlığın yerine/öncesinde) gösterilecek illüstrasyon.
  art?: ReactNode
  children: ReactNode
}

// Giriş, kayıt, doğrulama ekranları (taslak: Main.dc.html): solda büyük defter illüstrasyonu, sağda cam kart.
export function AuthLayout({ title, subtitle, art, children }: Props) {
  return (
    <div className="auth-shell">
      <div className="auth-hero" aria-hidden>
        <NotebookHero width={520} />
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, textAlign: 'center' }}>
          <span className="hand" style={{ fontSize: 34 }}>
            Hayatının en düzenli sayfası.
          </span>
          <span style={{ fontSize: 15, opacity: 0.8 }}>Planla, sürükle, tamamla. Senin defterin, senin düzenin.</span>
        </div>
      </div>

      <div className="auth-side">
        <main className="auth-card page-enter">
          <Logo size="large" />
          {art}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <h1 style={{ margin: 0, fontSize: 30, fontWeight: 700, letterSpacing: '-0.02em', lineHeight: 1.15 }}>{title}</h1>
            {subtitle && <p style={{ margin: 0, fontSize: 15, opacity: 0.75 }}>{subtitle}</p>}
          </div>
          {children}
        </main>
      </div>
    </div>
  )
}
