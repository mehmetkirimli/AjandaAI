import { Title } from '@mantine/core'
import type { ReactNode } from 'react'

// Kart başlığı: kalem daire içinde ikon + Kalam başlık (profil kartları, Claude bağlantısı).
export function CardTitle({ icon, children }: { icon: ReactNode; children: string }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
      <span className="avatar" style={{ width: 34, height: 34 }} aria-hidden>
        {icon}
      </span>
      <Title order={2} fz={22}>
        {children}
      </Title>
    </div>
  )
}
