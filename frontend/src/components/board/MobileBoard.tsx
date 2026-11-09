import { useDroppable } from '@dnd-kit/core'
import { Skeleton } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import { IconChevronLeft, IconChevronRight, IconSearch } from '@tabler/icons-react'
import type { Dayjs } from 'dayjs'
import { useState, type CSSProperties } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryLook, UNCATEGORIZED } from '../../lib/categoryColor'
import { activityTime, DAY_NAMES, dayKey } from '../../lib/week'
import { EmptyPage, Spiral } from '../illustrations'
import { Logo } from '../Logo'
import { DraggableCard } from './DayColumn'

const SHORT = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

interface Props {
  greeting: string
  days: Dayjs[]
  byDay: Map<string, ActivityListItem[]>
  todayKey: string
  loading: boolean
  categoryName: (id: number) => string
  onOpen: (id: number) => void
  onAddActivity: (day: Dayjs) => void
  onPrev: () => void
  onNext: () => void
  onToday: () => void
}

// Şeritteki gün: dokununca seçilir; sürüklenen kart üstüne bırakılınca o güne taşınır.
function StripDay({ day, index, today, selected, hasItems, onSelect }: { day: Dayjs; index: number; today: boolean; selected: boolean; hasItems: boolean; onSelect: () => void }) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  return (
    <button
      ref={setNodeRef}
      type="button"
      className="m-strip-day"
      data-today={today}
      data-over={isOver}
      aria-pressed={selected}
      aria-label={`${day.format('D')} ${DAY_NAMES[index]}`}
      onClick={onSelect}
    >
      {SHORT[index]}
      <b>{day.format('D')}</b>
      {hasItems && <span className="m-strip-dot" />}
    </button>
  )
}

// Liste satırı (taslak: Eskiz-Mobil "yaklaşanlar"): gün, 3 px kategori çizgisi, başlık + kategori, saat.
function Row({ item, categoryName, day, index }: { item: ActivityListItem; categoryName: string; day?: Dayjs; index?: number }) {
  const cancelled = item.status === 'Cancelled'
  const done = item.status === 'Completed'
  const look = categoryLook(item.categoryId, categoryName, cancelled || categoryName === UNCATEGORIZED)
  const Icon = look.icon
  const struck: CSSProperties | undefined = done || cancelled ? { textDecoration: 'line-through', color: 'var(--muted)' } : undefined
  return (
    <div className="m-row">
      {day && index !== undefined ? (
        <span className="m-row-day">
          {SHORT[index]}
          <b>{day.format('D')}</b>
        </span>
      ) : (
        <Icon size={18} stroke={1.7} aria-hidden style={{ flex: 'none' }} />
      )}
      <span className="m-row-line" style={{ background: look.line }} />
      <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span className="act-title" style={struck}>
          {item.title}
        </span>
        <span style={{ fontSize: 12, color: 'var(--graphite-2)' }}>
          {categoryName}
          {item.seriesId != null ? ' · ↻' : ''}
        </span>
      </span>
      <span className="mono" style={{ fontSize: 12, flex: 'none' }}>
        {activityTime(item, true)}
        {done ? ' ✓' : ''}
      </span>
    </div>
  )
}

// Mobil Pano (taslak: Eskiz-Mobil.dc.html): gri başlık şeridi + 7 günlük daire şerit, seçili günün spiral kartı,
// yaklaşanlar, boş gün çipleri.
export function MobileBoard({ greeting, days, byDay, todayKey, loading, categoryName, onOpen, onAddActivity, onPrev, onNext, onToday }: Props) {
  const todayIndex = days.findIndex((d) => dayKey(d) === todayKey)
  const [picked, setPicked] = useState<{ week: string; index: number } | null>(null)
  const weekId = dayKey(days[0])
  // Hafta değişince seçim sıfırlanır: bu haftaysa bugün, değilse Pazartesi.
  const selected = picked?.week === weekId ? picked.index : Math.max(0, todayIndex)
  const selectedDay = days[selected]
  const selectedItems = byDay.get(dayKey(selectedDay)) ?? []
  const isTodaySelected = dayKey(selectedDay) === todayKey

  const upcoming = days
    .map((d, i) => ({ d, i }))
    .filter(({ i }) => i > selected)
    .flatMap(({ d, i }) => (byDay.get(dayKey(d)) ?? []).map((item) => ({ item, d, i })))
    .slice(0, 6)
  const emptyDays = days.map((d, i) => ({ d, i })).filter(({ d, i }) => i !== selected && (byDay.get(dayKey(d)) ?? []).length === 0)

  return (
    <>
      <header className="m-hero">
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <Logo size="compact" />
          <div style={{ flex: 1 }} />
          <button type="button" className="icon-btn" aria-label="Önceki hafta" onClick={onPrev}>
            <IconChevronLeft size={18} stroke={1.8} aria-hidden />
          </button>
          <button type="button" className="icon-btn" aria-label="Sonraki hafta" onClick={onNext}>
            <IconChevronRight size={18} stroke={1.8} aria-hidden />
          </button>
          <button type="button" className="icon-btn" aria-label="Ara" onClick={() => spotlight.open()}>
            <IconSearch size={18} stroke={1.8} aria-hidden />
          </button>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <span className="hand muted" style={{ fontSize: 17 }}>
            {greeting}
          </span>
          <h1 style={{ margin: 0, fontFamily: 'var(--font-hand)', fontSize: 30, fontWeight: 700, lineHeight: 1.05 }}>
            {selectedDay.format('D MMMM')}, {DAY_NAMES[selected]}
          </h1>
          <span style={{ fontSize: 13, display: 'flex', alignItems: 'center', gap: 10, marginTop: 4, color: 'var(--graphite-2)' }}>
            {loading ? 'Yükleniyor…' : `${isTodaySelected ? 'Bugün' : 'Bu gün'} ${selectedItems.length} planın var`}
            {todayIndex < 0 && (
              <button type="button" className="text-btn" onClick={onToday}>
                Bugüne dön
              </button>
            )}
          </span>
        </div>
        <div className="m-strip">
          {days.map((d, i) => (
            <StripDay
              key={dayKey(d)}
              day={d}
              index={i}
              today={dayKey(d) === todayKey}
              selected={i === selected}
              hasItems={(byDay.get(dayKey(d)) ?? []).length > 0}
              onSelect={() => setPicked({ week: weekId, index: i })}
            />
          ))}
        </div>
      </header>

      <div className="page-body" style={{ display: 'flex', flexDirection: 'column', gap: 12, paddingTop: 16 }}>
        <span className="m-section">{isTodaySelected ? 'bugün' : DAY_NAMES[selected].toLocaleLowerCase('tr')}</span>

        {loading ? (
          <div className="paper m-list" style={{ padding: 22 }}>
            <Spiral />
            <Skeleton h={40} radius={3} />
          </div>
        ) : selectedItems.length === 0 ? (
          <button
            type="button"
            className="paper day-empty"
            style={{ marginTop: 10, padding: '26px 18px 18px', minHeight: 0, flex: 'none' }}
            onClick={() => onAddActivity(selectedDay)}
          >
            <Spiral />
            <EmptyPage width={52} />
            <span className="hand" style={{ fontSize: 17 }}>
              boş sayfa, yeni fırsat · eklemek için dokun
            </span>
          </button>
        ) : (
          <div className="paper m-list">
            <Spiral />
            {selectedItems.map((item) => (
              <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen}>
                <Row item={item} categoryName={categoryName(item.categoryId)} />
              </DraggableCard>
            ))}
          </div>
        )}

        {!loading && upcoming.length > 0 && (
          <>
            <span className="m-section" style={{ marginTop: 4 }}>
              yaklaşanlar
            </span>
            <div className="paper m-list">
              <Spiral />
              {upcoming.map(({ item, d, i }) => (
                <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen}>
                  <Row item={item} day={d} index={i} categoryName={categoryName(item.categoryId)} />
                </DraggableCard>
              ))}
            </div>
          </>
        )}

        {!loading && emptyDays.length > 0 && (
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap', marginTop: 4 }}>
            <span className="hand muted" style={{ fontSize: 17 }}>
              boş günler:
            </span>
            {emptyDays.map(({ d, i }) => (
              <button
                key={dayKey(d)}
                type="button"
                className="m-chip"
                aria-label={`${DAY_NAMES[i]} ${d.format('D')} gününe aktivite ekle`}
                onClick={() => onAddActivity(d)}
              >
                {SHORT[i]} {d.format('D')} +
              </button>
            ))}
          </div>
        )}
        {!loading && selectedItems.length > 0 && (
          <span className="muted" style={{ fontSize: 12 }}>
            Kartı basılı tutup yukarıdaki şeritte bir güne bırakarak taşıyabilirsin.
          </span>
        )}
      </div>
    </>
  )
}
