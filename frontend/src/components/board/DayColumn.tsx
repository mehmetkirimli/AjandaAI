import { useDraggable, useDroppable } from '@dnd-kit/core'
import { Skeleton } from '@mantine/core'
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
  // Verilirse kartın yerine bu içerik sürüklenir (mobil liste, hafta sonu satırı).
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
        minWidth: 0,
        opacity: isDragging ? 0.35 : 1,
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
        <Skeleton key={i} h={44} radius={3} />
      ))}
    </>
  )
}

function DayHead({ day, index, isToday }: { day: Dayjs; index: number; isToday: boolean }) {
  if (isToday) {
    return (
      <div className="day-head" style={{ alignItems: 'center' }}>
        <span className="day-today-badge">{day.format('D')}</span>
        <span style={{ display: 'flex', flexDirection: 'column' }}>
          <span className="day-name">{DAY_NAMES[index]}</span>
          <span className="hand muted" style={{ fontSize: 15 }}>
            bugün
          </span>
        </span>
      </div>
    )
  }
  return (
    <div className="day-head">
      <span className="day-num">{day.format('D')}</span>
      <span className="day-name">{DAY_NAMES[index]}</span>
    </div>
  )
}

// Hafta içi gün kartı (taslak: Eskiz-Pano): spiral tel, kalem çerçeve; bugün = daire içinde tarih.
export function DayColumn({ day, index, items, categoryName, loading, isToday, onOpen, onAddActivity }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  const empty = !loading && items.length === 0

  return (
    <section
      ref={setNodeRef}
      className="day-card"
      data-shape={index}
      data-over={isOver}
      aria-label={`${day.format('D')} ${DAY_NAMES[index]}`}
    >
      <Spiral />
      <DayHead day={day} index={index} isToday={isToday} />

      {empty ? (
        <button type="button" className="day-empty" onClick={() => onAddActivity(day)} aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}>
          <EmptyPage width={56} />
          <span className="hand" style={{ fontSize: 17 }}>
            {isOver ? 'buraya bırak' : 'boş sayfa, yeni fırsat'}
          </span>
        </button>
      ) : (
        <div className="day-body">
          {loading ? (
            <LoadingCards count={index % 2 === 0 ? 2 : 1} />
          ) : (
            items.map((item) => (
              <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen} />
            ))
          )}
          {!loading && (
            <>
              <div className="day-rule" aria-hidden />
              <button type="button" className="day-add" onClick={() => onAddActivity(day)} aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}>
                + ekle
              </button>
            </>
          )}
        </div>
      )}
    </section>
  )
}

// Hafta sonu: belirgin yükseklikte kart, içerik alt alta (taslak: 256 px).
export function WeekendCard({ day, index, items, categoryName, loading, isToday, onOpen, onAddActivity }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  const empty = !loading && items.length === 0
  return (
    <section
      ref={setNodeRef}
      className="day-card"
      data-shape={index}
      data-over={isOver}
      aria-label={`${day.format('D')} ${DAY_NAMES[index]}`}
    >
      <Spiral />
      <DayHead day={day} index={index} isToday={isToday} />
      {empty ? (
        <button type="button" className="day-empty" onClick={() => onAddActivity(day)} aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}>
          {index === 6 ? <CoffeeCup width={46} color="#8a8984" /> : <EmptyPage width={56} />}
          <span className="hand" style={{ fontSize: 17 }}>
            {isOver ? 'buraya bırak' : index === 6 ? 'dinlenme günü, keyfine bak' : 'hafta sonu, boş sayfa'}
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
          </div>
          {!loading && (
            <div className="weekend-foot">
              <button type="button" className="day-add" style={{ padding: '0 14px', margin: 0 }} onClick={() => onAddActivity(day)} aria-label={`${DAY_NAMES[index]} gününe aktivite ekle`}>
                + ekle
              </button>
              {index === 6 ? <CoffeeCup width={46} /> : <span className="weekend-note">hafta sonu ☼</span>}
            </div>
          )}
        </>
      )}
    </section>
  )
}
