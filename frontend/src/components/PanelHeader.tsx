import { ActionIcon, Box, Button, Group, Kbd, SegmentedControl, Text, UnstyledButton } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import { IconChevronLeft, IconChevronRight, IconLayoutSidebar, IconMenu2, IconSearch } from '@tabler/icons-react'
import type { ReactNode } from 'react'
import { MOD_KEY } from '../lib/commands'
import { useLayout } from '../layout/context'

export type ViewType = 'timeGridWeek' | 'dayGridMonth' | 'timeGridDay'

const VIEWS: { value: ViewType; label: string }[] = [
  { value: 'timeGridWeek', label: 'Hafta' },
  { value: 'dayGridMonth', label: 'Ay' },
  { value: 'timeGridDay', label: 'Gün' },
]

interface HeaderProps {
  title: string
  meta?: string
  // Soldaki mini "Bugün" butonu; verilmezse görünmez.
  onToday?: () => void
  onPrev?: () => void
  onNext?: () => void
  // Verilirse sağda Hafta | Ay | Gün değiştirici görünür.
  view?: ViewType
  onViewChange?: (view: ViewType) => void
  // Sağdaki kontrollerin soluna ek öğe (örn. yükleniyor göstergesi).
  children?: ReactNode
}

// İnce üst navbar (48 px, layer-1 + blur): solda hiyerarşi ve Bugün, ortada command bar, sağda görünüm ve gezinme.
export function PanelHeader({ title, meta, onToday, onPrev, onNext, view, onViewChange, children }: HeaderProps) {
  const { collapsed, isMobile, toggleSidebar, openMobileNav } = useLayout()
  return (
    <Box
      component="header"
      style={{
        flex: 'none',
        height: 48,
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 12,
        padding: '0 16px',
        background: 'var(--app-layer-1)',
        backdropFilter: 'blur(12px)',
        WebkitBackdropFilter: 'blur(12px)',
        borderBottom: '1px solid var(--app-line)',
      }}
    >
      <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
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
          <>
            <Text fz={13} style={{ color: 'var(--app-text-faint)', opacity: 0.6 }} aria-hidden>
              /
            </Text>
            <Text fz={13} c="dimmed" truncate style={{ textTransform: 'capitalize' }}>
              {meta}
            </Text>
          </>
        )}
        {onToday && (
          <Button size="compact-xs" variant="default" onClick={onToday} ml={4}>
            Bugün
          </Button>
        )}
      </Group>

      {/* Ortada command bar: tıklayınca ⌘K paleti */}
      <UnstyledButton
        onClick={() => spotlight.open()}
        visibleFrom="md"
        aria-label="Komut paletini aç"
        style={{
          position: 'absolute',
          left: '50%',
          top: '50%',
          transform: 'translate(-50%, -50%)',
          width: 340,
          padding: '5px 8px 5px 10px',
          borderRadius: 8,
          border: '1px solid var(--app-line)',
          background: 'rgba(255,255,255,0.03)',
        }}
      >
        <Group justify="space-between" wrap="nowrap" gap="xs">
          <Group gap={8} wrap="nowrap">
            <IconSearch size={14} style={{ color: 'var(--app-text-faint)' }} />
            <Text size="xs" style={{ color: 'var(--app-text-faint)' }}>
              Ara veya komut çalıştır…
            </Text>
          </Group>
          <Group gap={3} wrap="nowrap">
            <Kbd size="xs">{MOD_KEY}</Kbd>
            <Kbd size="xs">K</Kbd>
          </Group>
        </Group>
      </UnstyledButton>

      <Group gap={6} wrap="nowrap">
        {children}
        {view && onViewChange && (
          <SegmentedControl
            size="xs"
            value={view}
            data={VIEWS}
            onChange={(v) => onViewChange(v as ViewType)}
            visibleFrom="sm"
          />
        )}
        {onPrev && (
          <ActionIcon size="md" aria-label="Önceki" onClick={onPrev}>
            <IconChevronLeft size={16} />
          </ActionIcon>
        )}
        {onNext && (
          <ActionIcon size="md" aria-label="Sonraki" onClick={onNext}>
            <IconChevronRight size={16} />
          </ActionIcon>
        )}
      </Group>
    </Box>
  )
}

// Navbar'ın altındaki kaydırılabilir içerik alanı.
export function PageBody({ children, padded = true }: { children: ReactNode; padded?: boolean }) {
  return (
    <Box style={{ flex: 1, minHeight: 0, overflow: 'auto', padding: padded ? 'var(--mantine-spacing-md)' : 0 }}>
      {children}
    </Box>
  )
}
