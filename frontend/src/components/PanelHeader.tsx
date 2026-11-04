import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import type { ReactNode } from 'react'

export type ViewType = 'timeGridWeek' | 'dayGridMonth' | 'timeGridDay'

const VIEWS: { value: ViewType; label: string }[] = [
  { value: 'timeGridWeek', label: 'Hafta' },
  { value: 'dayGridMonth', label: 'Ay' },
  { value: 'timeGridDay', label: 'Gün' },
]

interface HeaderProps {
  // Sayfa adı; `meta` verilirse büyük başlık meta olur, title el yazısı üst satıra iner.
  title: string
  meta?: string
  // El yazısı üst satır (Caveat); verilmezse meta varken title kullanılır.
  kicker?: string
  onToday?: () => void
  onPrev?: () => void
  onNext?: () => void
  prevLabel?: string
  nextLabel?: string
  view?: ViewType
  onViewChange?: (view: ViewType) => void
  // Sağdaki kontrollerin soluna ek öğe (örn. yükleniyor göstergesi, buton).
  children?: ReactNode
}

// İçerik başlığı (taslak: Pano/Takvim): el yazısı üst satır + büyük dönem başlığı; sağda görünüm ve ‹ Bugün ›.
export function PanelHeader({
  title,
  meta,
  kicker,
  onToday,
  onPrev,
  onNext,
  prevLabel = 'Önceki',
  nextLabel = 'Sonraki',
  view,
  onViewChange,
  children,
}: HeaderProps) {
  const small = kicker ?? (meta ? title : undefined)
  return (
    <header className="page-header">
      <div style={{ display: 'flex', flexDirection: 'column', minWidth: 0, flex: '1 1 220px' }}>
        {small && (
          <span className="hand" style={{ fontSize: 24 }}>
            {small}
          </span>
        )}
        <h1>{meta ?? title}</h1>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
        {children}
        {view && onViewChange && (
          <div role="group" aria-label="Görünüm" className="view-switch">
            {VIEWS.map((o) => (
              <button key={o.value} type="button" aria-pressed={o.value === view} onClick={() => onViewChange(o.value)}>
                {o.label}
              </button>
            ))}
          </div>
        )}
        {onPrev && (
          <button type="button" className="icon-btn" aria-label={prevLabel} onClick={onPrev}>
            <IconChevronLeft size={18} stroke={2.2} aria-hidden />
          </button>
        )}
        {onToday && (
          <button type="button" className="text-btn" onClick={onToday}>
            Bugün
          </button>
        )}
        {onNext && (
          <button type="button" className="icon-btn" aria-label={nextLabel} onClick={onNext}>
            <IconChevronRight size={18} stroke={2.2} aria-hidden />
          </button>
        )}
      </div>
    </header>
  )
}

// Başlığın altındaki kaydırılabilir içerik alanı.
export function PageBody({ children, padded = true }: { children: ReactNode; padded?: boolean }) {
  return (
    <div className="page-body" style={padded ? undefined : { padding: 0 }}>
      {children}
    </div>
  )
}
