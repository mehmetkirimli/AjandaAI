import { Group, Text, ThemeIcon } from '@mantine/core'
import { IconCalendarEvent } from '@tabler/icons-react'

export function Logo({ light = false }: { light?: boolean }) {
  return (
    <Group gap="xs" wrap="nowrap">
      <ThemeIcon size={36} radius="md" variant="gradient" gradient={{ from: 'indigo', to: 'grape', deg: 135 }}>
        <IconCalendarEvent size={22} />
      </ThemeIcon>
      <Text fw={800} fz={22} c={light ? 'white' : undefined} style={{ letterSpacing: '-0.5px' }}>
        Ajanda<Text span inherit c={light ? 'indigo.1' : 'indigo'}>AI</Text>
      </Text>
    </Group>
  )
}
