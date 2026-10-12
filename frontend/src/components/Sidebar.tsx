import { ActionIcon, Avatar, Box, Group, Kbd, Menu, Stack, Text, Tooltip, UnstyledButton, useComputedColorScheme, useMantineColorScheme } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import {
  IconCalendarEvent,
  IconCategory,
  IconChevronDown,
  IconLayoutKanban,
  IconLogout,
  IconMoon,
  IconPencilPlus,
  IconSearch,
  IconSun,
  IconUser,
  IconUsers,
} from '@tabler/icons-react'
import { useState, type ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { useActivityForm } from './activity-form/context'
import { MOD_KEY } from '../lib/commands'
import '../layout.css'
import { Logo } from './Logo'

function NavItem({ to, icon, children, onNavigate }: { to: string; icon: ReactNode; children: string; onNavigate?: () => void }) {
  const { pathname } = useLocation()
  return (
    <Link to={to} className="nav-item" data-active={pathname === to} onClick={onNavigate}>
      {icon}
      <span>{children}</span>
    </Link>
  )
}

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { user, logout } = useAuth()
  const { openActivityForm } = useActivityForm()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const { setColorScheme } = useMantineColorScheme()
  const scheme = useComputedColorScheme('dark')
  const [adminOpen, setAdminOpen] = useState(true)

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <Stack h="100%" gap={0} px={8} py={10} style={{ minHeight: 0 }}>
      {/* Üst satır: çalışma alanı menüsü + arama + yeni */}
      <Group justify="space-between" wrap="nowrap" mb={10}>
        <Menu position="bottom-start" width={210}>
          <Menu.Target>
            <UnstyledButton aria-label="Hesap menüsü" px={6} py={4} className="nav-item" style={{ width: 'auto', height: 30 }}>
              <Logo compact />
              <IconChevronDown size={12} />
            </UnstyledButton>
          </Menu.Target>
          <Menu.Dropdown>
            <Menu.Label style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={user?.email}>{user?.email}</Menu.Label>
            <Menu.Item
              leftSection={<IconUser size={14} />}
              component={Link}
              to="/profile"
              onClick={onNavigate}
            >
              Profilim
            </Menu.Item>
            <Menu.Item
              leftSection={scheme === 'dark' ? <IconSun size={14} /> : <IconMoon size={14} />}
              onClick={() => setColorScheme(scheme === 'dark' ? 'light' : 'dark')}
            >
              Temayı değiştir
            </Menu.Item>
            <Menu.Divider />
            <Menu.Item leftSection={<IconLogout size={14} />} onClick={handleLogout}>
              Çıkış yap
            </Menu.Item>
          </Menu.Dropdown>
        </Menu>
        <Group gap={2} wrap="nowrap">
          <Tooltip
            label={
              <Group gap={4}>
                Ara <Kbd size="xs">{MOD_KEY}</Kbd>
                <Kbd size="xs">K</Kbd>
              </Group>
            }
          >
            <ActionIcon size="md" aria-label="Komut paletini aç" onClick={() => spotlight.open()}>
              <IconSearch size={15} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Yeni aktivite">
            <ActionIcon
              size="md"
              aria-label="Yeni"
              onClick={() => openActivityForm({ mode: 'create' })}
            >
              <IconPencilPlus size={15} />
            </ActionIcon>
          </Tooltip>
        </Group>
      </Group>

      <Stack gap={1}>
        <NavItem to="/" icon={<IconLayoutKanban size={16} />} onNavigate={onNavigate}>
          Pano
        </NavItem>
        <NavItem to="/calendar" icon={<IconCalendarEvent size={16} />} onNavigate={onNavigate}>
          Takvim
        </NavItem>
      </Stack>

      {user?.role === 'Admin' && (
        <>
          <button
            type="button"
            className="nav-section"
            data-collapsed={!adminOpen}
            onClick={() => setAdminOpen((o) => !o)}
            aria-expanded={adminOpen}
          >
            Yönetim
            <IconChevronDown size={11} className="chev" />
          </button>
          {adminOpen && (
            <Stack gap={1} mt={2}>
              <NavItem to="/admin/users" icon={<IconUsers size={16} />} onNavigate={onNavigate}>
                Kullanıcılar
              </NavItem>
              <NavItem to="/admin/categories" icon={<IconCategory size={16} />} onNavigate={onNavigate}>
                Kategoriler
              </NavItem>
            </Stack>
          )}
        </>
      )}

      <Box style={{ flex: 1 }} />

      {/* Alt: avatar + ad → profil */}
      <Link to="/profile" className="nav-item" data-active={pathname === '/profile'} onClick={onNavigate} style={{ height: 34 }}>
        <Avatar color="accent" radius="xl" size={20}>
          {user?.displayName.charAt(0).toLocaleUpperCase('tr')}
        </Avatar>
        <Text span fz={13} fw={500} truncate>
          {user?.displayName}
        </Text>
      </Link>
    </Stack>
  )
}
