import { Group, Paper, Stack, Text } from '@mantine/core'
import { IconClock, IconSun } from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryStrip } from '../../lib/categoryColor'
import { PriorityBars } from './PriorityBars'

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) kart hafif büyür ve geniş, yumuşak gölge alır.
  overlay?: boolean
  style?: CSSProperties
}

// Sakin, Linear liste satırı gibi: nötr yüzey, hover'da aydınlanma; kategori = renkli nokta + gri etiket.
export function ActivityCard({ item, categoryName, overlay = false, style }: Props) {
  const start = dayjs(item.start)
  const end = dayjs(item.end)

  return (
    <Paper
      className="activity-card"
      px={10}
      py={8}
      radius="md"
      withBorder
      shadow={overlay ? 'xl' : undefined}
      style={{
        background: overlay ? 'var(--app-surface-2)' : undefined,
        borderColor: overlay ? 'var(--app-line-strong)' : undefined,
        borderStyle: item.isAllDay ? 'dashed' : 'solid',
        transform: overlay ? 'scale(1.03)' : undefined,
        cursor: overlay ? 'grabbing' : 'grab',
        ...style,
      }}
    >
      <Stack gap={4}>
        <Text fw={500} size="sm" lh={1.3} lineClamp={2} style={{ overflowWrap: 'anywhere' }}>
          {item.title}
        </Text>
        <Group gap={4} wrap="nowrap" c="dimmed">
          {item.isAllDay ? <IconSun size={12} /> : <IconClock size={12} />}
          <Text size="xs" c="dimmed">
            {item.isAllDay ? 'Tüm gün' : `${start.format('HH:mm')} – ${end.format('HH:mm')}`}
          </Text>
        </Group>
        <Group gap={6} wrap="nowrap" justify="space-between">
          <Group gap={6} wrap="nowrap" style={{ minWidth: 0 }}>
            <span
              style={{ width: 7, height: 7, borderRadius: '50%', background: categoryStrip(item.categoryId), flex: 'none' }}
            />
            <Text size="xs" c="dimmed" truncate>
              {categoryName}
            </Text>
          </Group>
          <Text c="dimmed" lh={0} style={{ flex: 'none' }}>
            <PriorityBars priority={item.priority} />
          </Text>
        </Group>
      </Stack>
    </Paper>
  )
}
