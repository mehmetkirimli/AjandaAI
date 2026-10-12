import { ActionIcon, Box, Group, Text } from '@mantine/core'
import { IconLayoutSidebar, IconMenu2 } from '@tabler/icons-react'
import type { ReactNode } from 'react'
import { useLayout } from '../layout/context'

interface HeaderProps {
  title: string
  meta?: string
  // Sağdaki küçük, nötr kontroller.
  children?: ReactNode
}

// Panelin kendi üst barı (44 px) ve altında ince ayırıcı.
export function PanelHeader({ title, meta, children }: HeaderProps) {
  const { collapsed, isMobile, toggleSidebar, openMobileNav } = useLayout()
  return (
    <Group
      h={44}
      px="md"
      justify="space-between"
      wrap="nowrap"
      style={{ flex: 'none', borderBottom: '1px solid var(--app-line)' }}
    >
      <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
        {isMobile ? (
          <ActionIcon size="sm" aria-label="Menüyü aç" onClick={openMobileNav}>
            <IconMenu2 size={16} />
          </ActionIcon>
        ) : (
          collapsed && (
            <ActionIcon size="sm" aria-label="Kenar çubuğunu aç" onClick={toggleSidebar}>
              <IconLayoutSidebar size={16} />
            </ActionIcon>
          )
        )}
        <Text fz={13} fw={500} truncate>
          {title}
        </Text>
        {meta && (
          <Text fz={12} c="dimmed" truncate style={{ textTransform: 'capitalize' }}>
            {meta}
          </Text>
        )}
      </Group>
      <Group gap={6} wrap="nowrap">
        {children}
      </Group>
    </Group>
  )
}

// Üst barın altındaki kaydırılabilir içerik alanı.
export function PageBody({ children, padded = true }: { children: ReactNode; padded?: boolean }) {
  return (
    <Box style={{ flex: 1, minHeight: 0, overflow: 'auto', padding: padded ? 'var(--mantine-spacing-md)' : 0 }}>
      {children}
    </Box>
  )
}
