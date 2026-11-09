import { IconCalendar, IconLayoutDashboard, IconMenu2, IconPlus } from '@tabler/icons-react'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { useActivityForm } from './activity-form/context'

// Mobil alt bar (taslak: Eskiz-Mobil.dc.html): Pano, Takvim, ortada grafit "+", Menü, Profil.
export function MobileNav({ onOpenMenu }: { onOpenMenu: () => void }) {
  const { pathname } = useLocation()
  const { user } = useAuth()
  const { openActivityForm } = useActivityForm()

  return (
    <nav aria-label="Alt menü" className="mobile-nav">
      <Link to="/" aria-label="Pano" className="mobile-nav-item" data-active={pathname === '/'}>
        <IconLayoutDashboard size={22} stroke={1.8} aria-hidden />
      </Link>
      <Link to="/calendar" aria-label="Takvim" className="mobile-nav-item" data-active={pathname.startsWith('/calendar')}>
        <IconCalendar size={22} stroke={1.8} aria-hidden />
      </Link>
      <button type="button" aria-label="Yeni aktivite" className="mobile-fab" onClick={() => openActivityForm({ mode: 'create' })}>
        <IconPlus size={24} stroke={2.2} color="var(--paper-bg)" aria-hidden />
      </button>
      <button type="button" aria-label="Menüyü aç" className="mobile-nav-item" onClick={onOpenMenu}>
        <IconMenu2 size={22} stroke={1.8} aria-hidden />
      </button>
      <Link to="/profile" aria-label="Profil" className="mobile-nav-item" data-active={pathname === '/profile'}>
        <span className="avatar" style={{ width: 30, height: 30, fontSize: 16 }}>
          {user?.displayName.charAt(0).toLocaleUpperCase('tr')}
        </span>
      </Link>
    </nav>
  )
}
