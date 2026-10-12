import { Text } from '@mantine/core'
import { IconGripVertical, IconRepeat } from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryStrip, UNCATEGORIZED } from '../../lib/categoryColor'

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) kart hafif büyür ve gölge alır.
  overlay?: boolean
  style?: CSSProperties
}

export const MONO = 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace'

// Linear tarzı kart: kategori renginin %10 tonu (hover %14), solda 4 px tam renk çizgi, üstte mikro ışık.
// Başlık 13/500, saat monospace 12 px; kategori solunda renkli noktalı mini pill.
export function ActivityCard({ item, categoryName, overlay = false, style }: Props) {
  const neutral = item.status === 'Cancelled' || categoryName === UNCATEGORIZED
  const color = neutral ? 'var(--mantine-color-gray-6)' : categoryStrip(item.categoryId)
  const time = item.isAllDay
    ? 'Tüm gün'
    : `${dayjs(item.start).format('HH:mm')} – ${dayjs(item.end).format('HH:mm')}`

  return (
    <div
      className="activity-pill"
      style={
        {
          '--cat': color,
          boxShadow: `inset 4px 0 0 0 ${color}, inset 0 1px 0 0 rgba(255,255,255,0.08)${overlay ? ', var(--mantine-shadow-xl)' : ''}`,
          transform: overlay ? 'scale(1.04)' : undefined,
          cursor: overlay ? 'grabbing' : 'grab',
          ...style,
        } as CSSProperties
      }
    >
      <IconGripVertical size={14} style={{ flex: 'none', color: 'var(--app-icon-faint)' }} />
      <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 3 }}>
        <Text
          fz={13}
          fw={500}
          lh={1.3}
          lineClamp={2}
          style={{ overflowWrap: 'anywhere' }}
          title={`${item.title} - ${time} [${categoryName}]`}
        >
          {item.title}
        </Text>
        <Text fz={12} lh={1.2} style={{ fontFamily: MONO, color: 'var(--app-text-muted)' }}>
          {time}
        </Text>
        <span className="cat-pill">
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: color, flex: 'none' }} />
          <span className="cat-pill-label">{categoryName}</span>
        </span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, flex: 'none' }}>
        {item.seriesId != null && (
          <IconRepeat size={12} aria-label="Seri" style={{ color: 'var(--app-text-faint)' }} />
        )}
        {item.priority === 'High' && (
          <span
            aria-label="Yüksek öncelik"
            style={{ width: 6, height: 6, borderRadius: '50%', background: 'var(--mantine-color-red-5)' }}
          />
        )}
      </div>
    </div>
  )
}
