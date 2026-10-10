import {
  ActionIcon,
  Anchor,
  AppShell,
  Avatar,
  Button,
  Container,
  Group,
  Menu,
  Text,
  useComputedColorScheme,
  useMantineColorScheme,
} from '@mantine/core'
import { IconLogout, IconMoon, IconShieldLock, IconSun } from '@tabler/icons-react'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Logo } from './Logo'

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const { setColorScheme } = useMantineColorScheme()
  const scheme = useComputedColorScheme('light')

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <AppShell header={{ height: 64 }} padding="md">
      <AppShell.Header>
        <Container fluid h="100%" px="lg">
          <Group h="100%" justify="space-between" wrap="nowrap">
            <Anchor component={Link} to="/" underline="never" c="inherit">
              <Logo />
            </Anchor>
            <Group gap="sm" wrap="nowrap">
              {user?.role === 'Admin' && (
                <Button component={Link} to="/admin" variant="subtle" leftSection={<IconShieldLock size={18} />}>
                  Admin
                </Button>
              )}
              <ActionIcon
                variant="subtle"
                color="gray"
                size="lg"
                aria-label="Temayı değiştir"
                onClick={() => setColorScheme(scheme === 'dark' ? 'light' : 'dark')}
              >
                {scheme === 'dark' ? <IconSun size={20} /> : <IconMoon size={20} />}
              </ActionIcon>
              <Menu position="bottom-end" width={220} shadow="md">
                <Menu.Target>
                  <Button variant="subtle" color="gray" px="xs" aria-label="Kullanıcı menüsü">
                    <Group gap="xs" wrap="nowrap">
                      <Avatar color="indigo" radius="xl" size={32}>
                        {user?.displayName.charAt(0).toLocaleUpperCase('tr')}
                      </Avatar>
                      <Text fw={600} size="sm" visibleFrom="xs">
                        {user?.displayName}
                      </Text>
                    </Group>
                  </Button>
                </Menu.Target>
                <Menu.Dropdown>
                  <Menu.Label>{user?.email}</Menu.Label>
                  <Menu.Item color="red" leftSection={<IconLogout size={16} />} onClick={handleLogout}>
                    Çıkış
                  </Menu.Item>
                </Menu.Dropdown>
              </Menu>
            </Group>
          </Group>
        </Container>
      </AppShell.Header>
      <AppShell.Main bg="var(--mantine-color-body)">
        <Outlet />
      </AppShell.Main>
    </AppShell>
  )
}
