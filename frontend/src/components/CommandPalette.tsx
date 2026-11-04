import { Kbd } from '@mantine/core'
import { Spotlight, type SpotlightActionData, type SpotlightActionGroupData } from '@mantine/spotlight'
import {
  IconArrowLeft,
  IconArrowRight,
  IconCalendar,
  IconCalendarEvent,
  IconCalendarMonth,
  IconCalendarWeek,
  IconCalendarDot,
  IconCategory,
  IconUsers,
  IconLayoutKanban,
  IconLogout,
  IconPlug,
  IconPlus,
  IconRepeat,
  IconSearch,
  IconShieldLock,
  IconUser,
} from '@tabler/icons-react'
import { useMemo } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { useActivityForm } from './activity-form/context'
import { runPageCommand, type PageCommand } from '../lib/commands'

const ICON = 16

export function CommandPalette() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const { openActivityForm } = useActivityForm()

  const actions = useMemo<SpotlightActionGroupData[]>(() => {
    const pageTarget = pathname === '/calendar' ? '/calendar' : '/'
    const page = (command: PageCommand) => () => runPageCommand(command, pathname, pageTarget, navigate)
    const calendar = (command: PageCommand) => () => runPageCommand(command, pathname, '/calendar', navigate)

    const navigation: SpotlightActionData[] = [
      {
        id: 'new-activity',
        label: 'Yeni aktivite',
        leftSection: <IconPlus size={ICON} />,
        rightSection: <Kbd size="xs">C</Kbd>,
        onClick: () => openActivityForm({ mode: 'create' }),
      },
      {
        id: 'bulk-activity',
        label: 'Toplu aktivite ekle',
        description: 'Tekrarlayan aktivite serisi',
        leftSection: <IconRepeat size={ICON} />,
        onClick: () => openActivityForm({ mode: 'create', repeat: true }),
      },
      { id: 'go-board', label: "Pano'ya git", leftSection: <IconLayoutKanban size={ICON} />, onClick: () => navigate('/') },
      {
        id: 'go-calendar',
        label: 'Takvime git',
        leftSection: <IconCalendarEvent size={ICON} />,
        onClick: () => navigate('/calendar'),
      },
      { id: 'today', label: 'Bugüne dön', leftSection: <IconCalendarDot size={ICON} />, onClick: page('today') },
      { id: 'prev', label: 'Önceki hafta', leftSection: <IconArrowLeft size={ICON} />, onClick: page('prev') },
      { id: 'next', label: 'Sonraki hafta', leftSection: <IconArrowRight size={ICON} />, onClick: page('next') },
    ]

    const view: SpotlightActionData[] = [
      {
        id: 'view-week',
        label: 'Hafta görünümü',
        description: 'Takvim',
        leftSection: <IconCalendarWeek size={ICON} />,
        onClick: calendar('view:timeGridWeek'),
      },
      {
        id: 'view-month',
        label: 'Ay görünümü',
        description: 'Takvim',
        leftSection: <IconCalendarMonth size={ICON} />,
        onClick: calendar('view:dayGridMonth'),
      },
      {
        id: 'view-day',
        label: 'Gün görünümü',
        description: 'Takvim',
        leftSection: <IconCalendar size={ICON} />,
        onClick: calendar('view:timeGridDay'),
      },
    ]

    const account: SpotlightActionData[] = [
      { id: 'profile', label: 'Profilim', leftSection: <IconUser size={ICON} />, onClick: () => navigate('/profile') },
      {
        id: 'logout',
        label: 'Çıkış yap',
        leftSection: <IconLogout size={ICON} />,
        onClick: () => {
          void logout().then(() => navigate('/login', { replace: true }))
        },
      },
    ]

    const admin: SpotlightActionData[] =
      user?.role === 'Admin'
        ? [
            { id: 'admin-users', label: 'Kullanıcılar', description: 'Yönetim', leftSection: <IconUsers size={ICON} />, onClick: () => navigate('/admin/users') },
            { id: 'admin-categories', label: 'Kategoriler', description: 'Yönetim', leftSection: <IconCategory size={ICON} />, onClick: () => navigate('/admin/categories') },
            { id: 'admin-activities', label: 'Moderasyon', description: 'Yönetim · tüm aktiviteler', leftSection: <IconShieldLock size={ICON} />, onClick: () => navigate('/admin/activities') },
            { id: 'admin-integrations', label: 'Entegrasyonlar ve Yol Haritası', description: 'Yönetim · MCP', leftSection: <IconPlug size={ICON} />, onClick: () => navigate('/admin/integrations') },
          ]
        : []

    return [
      { group: 'Gezinme', actions: navigation },
      { group: 'Görünüm', actions: view },
      ...(admin.length > 0 ? [{ group: 'Yönetim', actions: admin }] : []),
      { group: 'Hesap', actions: account },
    ]
  }, [pathname, navigate, user?.role, logout, openActivityForm])

  return (
    <Spotlight
      actions={actions}
      shortcut={['mod + K']}
      // Odak bir radio/input'ta (örn. navbar segmenti) iken de ⌘K açılsın; varsayılan input'ları yok sayardı.
      tagsToIgnore={[]}
      nothingFound="Eşleşen komut yok"
      highlightQuery
      limit={20}
      returnFocus={false}
      scrollable
      maxHeight={380}
      radius="lg"
      searchProps={{
        leftSection: <IconSearch size={ICON} />,
        placeholder: 'Ara veya komut çalıştır…',
        rightSection: <Kbd size="xs">Esc</Kbd>,
      }}
    />
  )
}

