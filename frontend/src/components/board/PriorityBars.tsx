import type { Priority } from '../../api/types'

const LEVEL: Record<Priority, number> = { Low: 1, Medium: 2, High: 3 }
const LABEL: Record<Priority, string> = { Low: 'Düşük', Medium: 'Orta', High: 'Yüksek' }

// Linear'daki gibi küçük çubuk ikon: dolu çubuk sayısı = öncelik.
export function PriorityBars({ priority }: { priority: Priority }) {
  const level = LEVEL[priority] ?? 1
  return (
    <svg width="12" height="12" viewBox="0 0 12 12" role="img" aria-label={`Öncelik: ${LABEL[priority]}`}>
      <title>{`Öncelik: ${LABEL[priority]}`}</title>
      {[0, 1, 2].map((i) => (
        <rect
          key={i}
          x={i * 4 + 0.5}
          y={8 - i * 3}
          width="3"
          height={4 + i * 3}
          rx="0.8"
          fill="currentColor"
          opacity={i < level ? 1 : 0.22}
        />
      ))}
    </svg>
  )
}
