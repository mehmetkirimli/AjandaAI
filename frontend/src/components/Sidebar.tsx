import { ActionIcon, Tooltip } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import {
  IconCalendar,
  IconCheck,
  IconLayoutDashboard,
  IconLogout,
  IconPlug,
  IconPlus,
  IconRepeat,
  IconSearch,
  IconShield,
  IconTag,
  IconUsers,
} from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import type { ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { fetchActivities } from '../api/activities'
import { useAuth } from '../auth/context'
import { MOD_KEY } from '../lib/commands'
import { dayKey, startOfWeek } from '../lib/week'
import { useActivityForm } from './activity-form/context'
import { Logo } from './Logo'

const ICON = { size: 18, stroke: 1.7, color: 'var(--graphite)' } as const

function NavItem({
  to,
  icon,
  children,
  onNavigate,
  badge,
}: {
  to: string
  icon: ReactNode
  children: string
  onNavigate?: () => void
  badge?: string
}) {
  const { pathname } = useLocation()
  const active = to === '/' ? pathname === '/' : pathname.startsWith(to)
  return (
    <Link to={to} className="nav-item" data-active={active} onClick={onNavigate} aria-current={active ? 'page' : undefined}>
      {icon}
      <span className="nav-label">{children}</span>
      {badge && <span className="nav-badge">{badge}</span>}
    </Link>
  )
}

// "Bu hafta" kutusu: Pano ile aynı sorgu anahtarı (önbellek paylaşılır). "Tamam" = Completed durumu.
// Her plan bir kutucuk; tamamlananlar işaretli (en fazla 14 kutucuk).
function WeekProgress() {
  const weekStart = startOfWeek(dayjs())
  const query = useQuery({
    queryKey: ['activities', dayKey(weekStart)],
    queryFn: () => fetchActivities(weekStart.toISOString(), weekStart.add(7, 'day').subtract(1, 'millisecond').toISOString()),
  })
  const items = query.data ?? []
  const total = items.length
  const done = items.filter((a) => a.status === 'Completed').length
  const pct = total === 0 ? 0 : Math.round((done / total) * 100)
  const boxes = Math.min(total, 14)
  const doneBoxes = total === 0 ? 0 : Math.round((done / total) * boxes)
  return (
    <div className="week-box">
      <span className="hand" style={{ fontSize: 19 }}>
        bu hafta
      </span>
      <span style={{ fontSize: 13 }}>
        {query.isPending ? 'Yükleniyor…' : total === 0 ? 'Henüz plan yok' : `${total} plan · ${done} tamamlandı`}
      </span>
      {boxes > 0 && (
        <div
          className="week-ticks"
          role="progressbar"
          aria-label="Bu haftanın tamamlanma oranı"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={pct}
        >
          {Array.from({ length: boxes }, (_, i) => (
            <span key={i}>{i < doneBoxes && <IconCheck size={10} stroke={3} aria-hidden />}</span>
          ))}
        </div>
      )}
    </div>
  )
}

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { user, logout } = useAuth()
  const { openActivityForm } = useActivityForm()
  const navigate = useNavigate()
  const { pathname } = useLocation()

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  const isAdmin = user?.role === 'Admin'

  return (
    <div className="sidebar">
      <div style={{ padding: '0 6px 22px' }}>
        <Logo />
      </div>

      <button
        type="button"
        className="btn-ink"
        style={{ height: 42, marginBottom: 16, fontSize: 14 }}
        onClick={() => {
          onNavigate?.()
          openActivityForm({ mode: 'create' })
        }}
      >
        <IconPlus size={16} stroke={2.2} aria-hidden />
        Yeni aktivite
      </button>

      <NavItem to="/" icon={<IconLayoutDashboard {...ICON} />} onNavigate={onNavigate}>
        Pano
      </NavItem>
      <NavItem to="/calendar" icon={<IconCalendar {...ICON} />} onNavigate={onNavigate}>
        Takvim
      </NavItem>
      <button
        type="button"
        className="nav-item"
        onClick={() => {
          onNavigate?.()
          openActivityForm({ mode: 'create', repeat: true })
        }}
      >
        <IconRepeat {...ICON} aria-hidden />
        <span className="nav-label">Tekrarlayan ekle</span>
      </button>
      <button
        type="button"
        className="nav-item"
        onClick={() => {
          onNavigate?.()
          spotlight.open()
        }}
      >
        <IconSearch {...ICON} aria-hidden />
        <span className="nav-label">Ara</span>
        <span className="nav-kbd">{MOD_KEY} K</span>
      </button>

      {isAdmin && (
        <>
          <span className="nav-section">yönetim</span>
          <NavItem to="/admin/users" icon={<IconUsers {...ICON} />} onNavigate={onNavigate}>
            Kullanıcılar
          </NavItem>
          <NavItem to="/admin/categories" icon={<IconTag {...ICON} />} onNavigate={onNavigate}>
            Kategoriler
          </NavItem>
          <NavItem to="/admin/activities" icon={<IconShield {...ICON} />} onNavigate={onNavigate}>
            Moderasyon
          </NavItem>
          <NavItem to="/admin/integrations" icon={<IconPlug {...ICON} />} onNavigate={onNavigate} badge="MCP">
            Entegrasyonlar
          </NavItem>
        </>
      )}

      <div style={{ flex: 1, minHeight: 16 }} />

      <WeekProgress />

      <div style={{ display: 'flex', alignItems: 'center', gap: 4, marginTop: 10 }}>
        <Link
          to="/profile"
          className="nav-item"
          data-active={pathname === '/profile'}
          onClick={onNavigate}
          style={{ height: 46, padding: '0 6px', flex: 1, minWidth: 0, gap: 10 }}
        >
          <span className="avatar" style={{ width: 30, height: 30, fontSize: 16 }}>
            {user?.displayName.charAt(0).toLocaleUpperCase('tr')}
          </span>
          <span style={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
            <span className="nav-label" style={{ fontSize: 13 }}>
              {user?.displayName}
            </span>
            {isAdmin && (
              <span className="muted" style={{ fontSize: 12 }}>
                Admin
              </span>
            )}
          </span>
        </Link>
        <Tooltip label="Çıkış yap">
          <ActionIcon size={44} aria-label="Çıkış yap" onClick={handleLogout} variant="subtle">
            <IconLogout size={18} stroke={1.7} color="var(--graphite)" />
          </ActionIcon>
        </Tooltip>
      </div>
    </div>
  )
}
