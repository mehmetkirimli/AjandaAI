import { useDraggable, useDroppable } from '@dnd-kit/core'
import { Skeleton } from '@mantine/core'
import { IconPlus } from '@tabler/icons-react'
import type { Dayjs } from 'dayjs'
import type { ReactNode } from 'react'
import type { ActivityListItem } from '../../api/types'
import { DAY_NAMES, dayKey } from '../../lib/week'
import { CoffeeCup, EmptyPage, Spiral } from '../illustrations'
import { ActivityCard } from './ActivityCard'

interface Props {
  day: Dayjs
  index: number
  items: ActivityListItem[]
  categoryName: (id: number) => string
  loading: boolean
  isToday: boolean
  onOpen: (id: number) => void
  onAddActivity: (day: Dayjs) => void
}

export function DraggableCard({
  item,
  categoryName,
  onOpen,
  children,
}: {
  item: ActivityListItem
  categoryName: string
  onOpen: (id: number) => void
  // Verilirse kartın yerine bu içerik sürüklenir (mobil liste).
  children?: ReactNode
}) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: item.id })
  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      onClick={() => onOpen(item.id)}
      className="card-enter"
      aria-label={`${item.title}. Taşımak için boşluk tuşuna basın, sağ ve sol ok tuşlarıyla gün değiştirin.`}
      style={{
        touchAction: 'manipulation',
        WebkitTouchCallout: 'none',
        userSelect: 'none',
        outlineOffset: 2,
        borderRadius: 12,
        minWidth: 0,
        opacity: isDragging ? 0.35 : 1,
        filter: isDragging ? 'grayscale(0.6)' : undefined,
      }}
    >
      {children ?? <ActivityCard item={item} categoryName={categoryName} />}
    </div>
  )
}

function LoadingCards({ count }: { count: number }) {
  return (
    <>
      {Array.from({ length: count }, (_, i) => (
        <Skeleton key={i} h={48} radius={12} style={{ opacity: 0.35 }} />
      ))}
    </>
  )
}

// Hafta içi gün kartı: beyaz spiral halkalar + çizgili sayfa; bugün gradyan çerçeve + parlama.
export function DayColumn({ day, index, items, categoryName, loading, isToday, onOpen, onAddActivity }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  const empty = !loading && items.length === 0

  return (
    <section ref={setNodeRef} className="day-card" data-today={isToday} data-over={isOver} aria-label={`${day.format('D')} ${DAY_NAMES[index]}`}>
      <Spiral />
      {isToday ? (
        <div className="day-head" style={{ alignItems: 'center' }}>
          <span className="day-today-badge">{day.format('D')}</span>
          <span style={{ display: 'flex', flexDirection: 'column' }}>
            <span style={{ fontSize: 14, fontWeight: 700 }}>{DAY_NAMES[index]}</span>
            <span className="hand" style={{ fontSize: 18 }}>
              bugün
            </span>
          </span>
        </div>
      ) : (
        <div className="day-head">
          <span className="day-num">{day.format('D')}</span>
          <span style={{ fontSize: 14 }}>{DAY_NAMES[index]}</span>
        </div>
      )}

      {empty ? (
        <button type="button" className="day-empty" onClick={() => onAddActivity(day)} aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}>
          <EmptyPage width={84} />
          <span className="hand" style={{ fontSize: 20 }}>
            {isOver ? 'Buraya bırak' : 'Boş sayfa, yeni fırsat'}
          </span>
        </button>
      ) : (
        <div className="day-body ruled">
          {loading ? (
            <LoadingCards count={index % 2 === 0 ? 2 : 1} />
          ) : (
            items.map((item) => (
              <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen} />
            ))
          )}
          {!loading && (
            <button type="button" className="day-add" onClick={() => onAddActivity(day)}>
              <IconPlus size={14} stroke={2.4} aria-hidden />
              Ekle
            </button>
          )}
        </div>
      )}
    </section>
  )
}

// Hafta sonu: iki geniş yatay kart.
export function WeekendCard({ day, index, items, categoryName, loading, isToday, onOpen, onAddActivity }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  const empty = !loading && items.length === 0
  return (
    <section
      ref={setNodeRef}
      className="weekend-card"
      data-idx={index - 5}
      data-today={isToday}
      data-over={isOver}
      aria-label={`${day.format('D')} ${DAY_NAMES[index]}`}
    >
      <div style={{ display: 'flex', flexDirection: 'column', flex: 'none' }}>
        <span className="day-num">{day.format('D')}</span>
        <span style={{ fontSize: 14 }}>{DAY_NAMES[index]}</span>
        {isToday && (
          <span className="hand" style={{ fontSize: 18 }}>
            bugün
          </span>
        )}
      </div>
      {empty ? (
        <button
          type="button"
          className="day-empty"
          style={{ flexDirection: 'row', justifyContent: 'flex-start', minHeight: 64 }}
          onClick={() => onAddActivity(day)}
          aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}
        >
          {index === 6 ? <CoffeeCup width={56} /> : <EmptyPage width={56} />}
          <span className="hand" style={{ fontSize: 22, textAlign: 'left' }}>
            {isOver ? 'Buraya bırak' : index === 6 ? 'Dinlenme günü, keyfine bak' : 'Hafta sonu, boş sayfa'}
          </span>
        </button>
      ) : (
        <>
          <div className="weekend-items">
            {loading ? (
              <LoadingCards count={1} />
            ) : (
              items.map((item) => (
                <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen}>
                  <ActivityCard item={item} categoryName={categoryName(item.categoryId)} inline />
                </DraggableCard>
              ))
            )}
            {!loading && (
              <button type="button" className="day-add" style={{ padding: '0 14px', height: 44 }} onClick={() => onAddActivity(day)}>
                <IconPlus size={14} stroke={2.4} aria-hidden />
                Ekle
              </button>
            )}
          </div>
          <span className="hand" style={{ fontSize: 22, flex: 'none' }}>
            hafta sonu
          </span>
        </>
      )}
    </section>
  )
}
