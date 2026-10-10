import { Badge, Group, Paper, Stack, Text } from '@mantine/core'
import { IconClock, IconSun } from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { CSSProperties } from 'react'
import type { ActivityListItem, Priority } from '../../api/types'
import { categoryColor } from '../../lib/categoryColor'

const PRIORITY: Record<Priority, { label: string; color: string }> = {
  High: { label: 'Yüksek', color: 'red' },
  Medium: { label: 'Orta', color: 'yellow' },
  Low: { label: 'Düşük', color: 'gray' },
}

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) kart büyür ve gölgelenir.
  overlay?: boolean
  style?: CSSProperties
}

export function ActivityCard({ item, categoryName, overlay = false, style }: Props) {
  const color = categoryColor(item.categoryId)
  const priority = PRIORITY[item.priority] ?? PRIORITY.Low
  const start = dayjs(item.start)
  const end = dayjs(item.end)

  return (
    <Paper
      p="sm"
      radius="md"
      shadow={overlay ? 'xl' : 'xs'}
      style={{
        background: item.isAllDay
          ? `linear-gradient(135deg, var(--mantine-color-${color}-light) 0%, var(--mantine-color-${color}-light-hover) 100%)`
          : 'var(--mantine-color-body)',
        border: `1px ${item.isAllDay ? 'dashed' : 'solid'} var(--mantine-color-${color}-${item.isAllDay ? 'filled' : 'light-color'})`,
        borderLeft: `4px solid var(--mantine-color-${color}-filled)`,
        transform: overlay ? 'scale(1.05) rotate(1.5deg)' : undefined,
        cursor: overlay ? 'grabbing' : 'grab',
        transition: 'box-shadow 120ms ease, transform 120ms ease',
        ...style,
      }}
    >
      <Stack gap={6}>
        <Text fw={600} size="sm" lh={1.3} lineClamp={2} style={{ overflowWrap: "anywhere" }}>
          {item.title}
        </Text>
        <Group gap={4} wrap="nowrap" c="dimmed">
          {item.isAllDay ? <IconSun size={14} /> : <IconClock size={14} />}
          <Text size="xs">{item.isAllDay ? 'Tüm gün' : `${start.format('HH:mm')} – ${end.format('HH:mm')}`}</Text>
        </Group>
        <Group gap={6}>
          <Badge size="sm" variant="light" color={color} radius="sm" maw="100%">
            {categoryName}
          </Badge>
          <Badge size="sm" variant="dot" color={priority.color} radius="sm">
            {priority.label}
          </Badge>
        </Group>
      </Stack>
    </Paper>
  )
}
