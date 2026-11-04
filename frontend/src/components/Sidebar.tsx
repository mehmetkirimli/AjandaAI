import { ActionIcon, Kbd, Tooltip } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import {
  IconCalendar,
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

function NavItem({
  to,
  icon,
  children,
  onNavigate,
  tone = 'teal',
  badge,
}: {
  to: string
  icon: ReactNode
  children: string
  onNavigate?: () => void
  tone?: 'teal' | 'violet' | 'orange' | 'green'
  badge?: string
}) {
  const { pathname } = useLocation()
  const active = to === '/' ? pathname === '/' : pathname.startsWith(to)
  return (
    <Link to={to} className="nav-item" data-active={active} data-tone={tone} onClick={onNavigate} aria-current={active ? 'page' : undefined}>
      {icon}
      <span className="nav-label">{children}</span>
      {badge && <span className="nav-badge">{badge}</span>}
    </Link>
  )
}

// "Bu hafta" kutusu: Pano ile aynı sorgu anahtarı (önbellek paylaşılır). "Tamam" = Completed durumu.
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
  return (
    <div className="week-box">
      <span className="hand" style={{ fontSize: 22 }}>
        Bu hafta
      </span>
      <span style={{ fontSize: 14 }}>
        {query.isPending ? 'Yükleniyor…' : total === 0 ? 'Henüz plan yok' : `${total} plan · ${done} tamamlandı`}
      </span>
      <div
        role="progressbar"
        aria-label="Bu haftanın tamamlanma oranı"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={pct}
        style={{ height: 8, borderRadius: 4, background: 'rgba(255,255,255,0.15)', overflow: 'hidden' }}
      >
        <div style={{ width: `${pct}%`, height: '100%', background: 'linear-gradient(90deg, #4ADE80, #2DD4BF)', transition: 'width 300ms ease' }} />
      </div>
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
      <div style={{ padding: '0 8px 20px' }}>
        <Logo />
      </div>

      <button
        type="button"
        className="btn-rainbow"
        style={{ height: 44, marginBottom: 14, fontSize: 15 }}
        onClick={() => {
          onNavigate?.()
          openActivityForm({ mode: 'create' })
        }}
      >
        <IconPlus size={18} stroke={2.4} aria-hidden />
        Yeni aktivite
      </button>

      <NavItem to="/" icon={<IconLayoutDashboard size={20} color="var(--l-teal)" />} onNavigate={onNavigate} tone="teal">
        Pano
      </NavItem>
      <NavItem to="/calendar" icon={<IconCalendar size={20} color="var(--l-violet)" />} onNavigate={onNavigate} tone="violet">
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
        <IconRepeat size={20} color="var(--l-green)" aria-hidden />
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
        <IconSearch size={20} color="var(--l-orange)" aria-hidden />
        <span className="nav-label">Ara</span>
        <span style={{ marginLeft: 'auto', display: 'flex', gap: 3 }}>
          <Kbd size="xs">{MOD_KEY}</Kbd>
          <Kbd size="xs">K</Kbd>
        </span>
      </button>

      {isAdmin && (
        <>
          <span className="nav-section">YÖNETİM</span>
          <NavItem to="/admin/users" icon={<IconUsers size={20} color="var(--l-green)" />} onNavigate={onNavigate} tone="green">
            Kullanıcılar
          </NavItem>
          <NavItem to="/admin/categories" icon={<IconTag size={20} color="var(--l-orange)" />} onNavigate={onNavigate} tone="orange">
            Kategoriler
          </NavItem>
          <NavItem to="/admin/activities" icon={<IconShield size={20} color="var(--l-violet)" />} onNavigate={onNavigate} tone="violet">
            Moderasyon
          </NavItem>
          <NavItem
            to="/admin/integrations"
            icon={<IconPlug size={20} color="var(--l-violet)" />}
            onNavigate={onNavigate}
            tone="violet"
            badge="MCP"
          >
            Entegrasyonlar
          </NavItem>
        </>
      )}

      <div style={{ flex: 1, minHeight: 16 }} />

      <WeekProgress />

      <div style={{ display: 'flex', alignItems: 'center', gap: 4, marginTop: 8 }}>
        <Link
          to="/profile"
          className="nav-item"
          data-active={pathname === '/profile'}
          data-tone="orange"
          onClick={onNavigate}
          style={{ height: 48, padding: '0 8px', flex: 1, minWidth: 0 }}
        >
          <span className="avatar-grad" style={{ width: 32, height: 32 }}>
            {user?.displayName.charAt(0).toLocaleUpperCase('tr')}
          </span>
          <span style={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
            <span className="nav-label" style={{ fontSize: 14 }}>
              {user?.displayName}
            </span>
            {isAdmin && <span style={{ fontSize: 12 }}>Admin</span>}
          </span>
        </Link>
        <Tooltip label="Çıkış yap">
          <ActionIcon size={44} radius="md" aria-label="Çıkış yap" onClick={handleLogout} color="gray" variant="subtle">
            <IconLogout size={18} color="#fff" />
          </ActionIcon>
        </Tooltip>
      </div>
    </div>
  )
}
