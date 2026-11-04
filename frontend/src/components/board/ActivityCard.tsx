import { IconRepeat } from '@tabler/icons-react'
import type { CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryLook, UNCATEGORIZED } from '../../lib/categoryColor'
import { activityTime } from '../../lib/week'

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) kart hafifçe eğilir ve gölge alır.
  overlay?: boolean
  // Hafta sonu kartında yatay, tek satır hap.
  inline?: boolean
  style?: CSSProperties
}

export const MONO = "'JetBrains Mono Variable', 'JetBrains Mono', ui-monospace, monospace"

// Özel Defter kartı: kategori gradyanı, ikon kutusu, beyaz başlık, monospace saat.
export function ActivityCard({ item, categoryName, overlay = false, inline = false, style }: Props) {
  const neutral = item.status === 'Cancelled' || categoryName === UNCATEGORIZED
  const look = categoryLook(item.categoryId, categoryName, neutral)
  const Icon = look.icon
  const time = activityTime(item, inline)
  const cancelled = item.status === 'Cancelled'

  return (
    <div
      className="act-card"
      data-overlay={overlay}
      title={`${item.title} · ${activityTime(item)} · ${categoryName}`}
      style={{
        background: look.gradient,
        boxShadow: look.shadow,
        padding: inline ? '10px 12px' : undefined,
        ...style,
      }}
    >
      {inline ? (
        <Icon size={16} color="#fff" aria-hidden style={{ flex: 'none' }} />
      ) : (
        <span className="act-icon" aria-hidden>
          <Icon size={16} color="#fff" />
        </span>
      )}
      <span
        style={{
          display: 'flex',
          flexDirection: inline ? 'row' : 'column',
          alignItems: inline ? 'baseline' : undefined,
          gap: inline ? 8 : 1,
          minWidth: 0,
          flex: 1,
        }}
      >
        <span className="act-title" style={{ textDecoration: cancelled ? 'line-through' : undefined }}>
          {item.title}
        </span>
        <span className="act-time">{time}</span>
      </span>
      {(item.seriesId != null || item.priority === 'High') && (
        <span style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, flex: 'none' }}>
          {item.seriesId != null && <IconRepeat size={13} aria-label="Seri" color="#fff" />}
          {item.priority === 'High' && (
            <span
              aria-label="Yüksek öncelik"
              title="Yüksek öncelik"
              style={{ width: 8, height: 8, borderRadius: '50%', background: '#fff', boxShadow: '0 0 0 2px rgba(255,255,255,0.35)' }}
            />
          )}
        </span>
      )}
    </div>
  )
}
