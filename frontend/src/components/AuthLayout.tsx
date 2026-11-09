import type { ReactNode } from 'react'
import { NotebookHero, Scribble, Spiral } from './illustrations'
import { Logo } from './Logo'
import './auth.css'

interface Props {
  title: string
  subtitle?: string
  // Kartın üstünde (başlığın yerine/öncesinde) gösterilecek illüstrasyon.
  art?: ReactNode
  children: ReactNode
}

// Giriş, kayıt, doğrulama ekranları: noktalı kâğıt zemin, solda kalem çizimi defter, sağda spiral telli kâğıt kart.
export function AuthLayout({ title, subtitle, art, children }: Props) {
  return (
    <div className="auth-shell paper-bg">
      <div className="auth-hero" aria-hidden>
        <NotebookHero width={460} />
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, textAlign: 'center' }}>
          <span className="hand" style={{ fontSize: 30, fontWeight: 700 }}>
            hayatının en düzenli sayfası.
          </span>
          <span className="muted" style={{ fontSize: 14 }}>
            Planla, sürükle, tamamla. Senin defterin, senin düzenin.
          </span>
        </div>
      </div>

      <div className="auth-side">
        <main className="auth-card paper page-enter">
          <Spiral />
          <Logo size="large" />
          {art}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <h1 style={{ margin: 0, fontFamily: 'var(--font-hand)', fontSize: 32, fontWeight: 700, lineHeight: 1.1 }}>{title}</h1>
            <Scribble width={180} />
            {subtitle && (
              <p className="muted" style={{ margin: '4px 0 0', fontSize: 14 }}>
                {subtitle}
              </p>
            )}
          </div>
          {children}
        </main>
      </div>
    </div>
  )
}
