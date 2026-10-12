import { Group, Text, ThemeIcon } from '@mantine/core'
import { IconCalendarEvent } from '@tabler/icons-react'

// compact: sidebar üst satırı (küçük ikon + 13 px ad). Varsayılan: auth sayfaları.
export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Group gap={compact ? 8 : 10} wrap="nowrap">
      <ThemeIcon
        size={compact ? 20 : 32}
        radius={compact ? 'sm' : 'md'}
        variant="gradient"
        gradient={{ from: 'accent.5', to: 'accent.7', deg: 135 }}
      >
        <IconCalendarEvent size={compact ? 13 : 18} />
      </ThemeIcon>
      <Text fw={600} fz={compact ? 13 : 17} style={{ letterSpacing: '-0.01em' }}>
        AjandaAI
      </Text>
    </Group>
  )
}
