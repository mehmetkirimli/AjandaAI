import { Group, Text } from '@mantine/core'
import { IconGripVertical, IconRepeat } from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryFill, UNCATEGORIZED } from '../../lib/categoryColor'

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) hap hafif büyür ve gölge alır.
  overlay?: boolean
  style?: CSSProperties
}

// Renkli hap: kategori renginde dolu zemin; iptal edilmiş veya kategorisiz aktivite nötr gri.
// Metin: "Başlık - 09:30 [Kategori]" (tüm gün: "Tüm gün"), en fazla 2 satır.
export function ActivityCard({ item, categoryName, overlay = false, style }: Props) {
  const neutral = item.status === 'Cancelled' || categoryName === UNCATEGORIZED
  const background = neutral ? 'var(--mantine-color-gray-7)' : categoryFill(item.categoryId)
  const color = '#ffffff'
  const time = item.isAllDay ? 'Tüm gün' : dayjs(item.start).format('HH:mm')

  return (
    <Group
      className="activity-pill"
      gap={4}
      wrap="nowrap"
      px={6}
      py={6}
      mih={38}
      style={{
        background,
        color,
        borderRadius: 8,
        boxShadow: overlay ? 'var(--mantine-shadow-xl)' : undefined,
        transform: overlay ? 'scale(1.04)' : undefined,
        cursor: overlay ? 'grabbing' : 'grab',
        ...style,
      }}
    >
      <IconGripVertical size={14} style={{ flex: 'none', opacity: 0.6 }} />
      <Text
        fz={12.5}
        fw={500}
        lh={1.3}
        lineClamp={2}
        c="inherit"
        style={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}
        title={`${item.title} - ${time} [${categoryName}]`}
      >
        {item.title} - {time} [{categoryName}]
      </Text>
      {item.seriesId != null && <IconRepeat size={12} aria-label="Seri" style={{ flex: 'none', opacity: 0.6 }} />}
      {item.priority === 'High' && (
        <span
          aria-label="Yüksek öncelik"
          style={{ width: 6, height: 6, borderRadius: '50%', background: 'currentColor', opacity: 0.85, flex: 'none' }}
        />
      )}
    </Group>
  )
}
