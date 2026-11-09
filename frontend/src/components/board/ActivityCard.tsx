import type { CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryLook, UNCATEGORIZED } from '../../lib/categoryColor'
import { activityTime } from '../../lib/week'

interface Props {
  item: ActivityListItem
  categoryName: string
  // Sürükleme katmanında (DragOverlay) kart kâğıt parçası gibi hafifçe eğilir.
  overlay?: boolean
  // Hafta sonu kartında tek satır: çizgi + başlık + saat.
  inline?: boolean
  style?: CSSProperties
}

// Eskiz kartı (taslak: Eskiz-Pano): 3 px solgun kategori çizgisi, grafit ikon ve başlık, mono saat.
// Seri ↻, tamamlanan ✓ + üstü çizili, yüksek öncelik "!".
export function ActivityCard({ item, categoryName, overlay = false, inline = false, style }: Props) {
  const cancelled = item.status === 'Cancelled'
  const done = item.status === 'Completed'
  const neutral = cancelled || categoryName === UNCATEGORIZED
  const look = categoryLook(item.categoryId, categoryName, neutral)
  const Icon = look.icon
  const marks = `${item.seriesId != null ? ' ↻' : ''}${done ? ' ✓' : ''}`

  return (
    <div
      className="act-card"
      data-overlay={overlay}
      data-inline={inline}
      data-done={done}
      data-cancelled={cancelled}
      title={`${item.title} · ${activityTime(item)} · ${categoryName}`}
      style={{ '--cat': look.line, ...style } as CSSProperties}
    >
      {!inline && <Icon size={16} stroke={1.7} color="var(--graphite)" aria-hidden style={{ flex: 'none', marginTop: 2 }} />}
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
        <span className="act-title">{item.title}</span>
        <span className="act-time">
          {activityTime(item, inline)}
          {marks}
        </span>
      </span>
      {item.priority === 'High' && (
        <span aria-label="Yüksek öncelik" title="Yüksek öncelik" className="mono" style={{ fontSize: 13, fontWeight: 700, flex: 'none' }}>
          !
        </span>
      )}
    </div>
  )
}
