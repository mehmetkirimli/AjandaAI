import { useDroppable } from '@dnd-kit/core'
import { Skeleton } from '@mantine/core'
import { spotlight } from '@mantine/spotlight'
import { IconChevronLeft, IconChevronRight, IconGripVertical, IconSearch } from '@tabler/icons-react'
import type { Dayjs } from 'dayjs'
import { useState } from 'react'
import type { ActivityListItem } from '../../api/types'
import { categoryLook, UNCATEGORIZED } from '../../lib/categoryColor'
import { activityTime, DAY_NAMES, dayKey } from '../../lib/week'
import { EmptyPage } from '../illustrations'
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
      style={selected && !today ? { borderColor: 'rgba(255,255,255,0.8)' } : undefined}
    >
      {SHORT[index]}
      <b>{day.format('D')}</b>
      {hasItems && <span className="m-strip-dot" />}
    </button>
  )
}

function TodayCard({ item, categoryName }: { item: ActivityListItem; categoryName: string }) {
  const neutral = item.status === 'Cancelled' || categoryName === UNCATEGORIZED
  const look = categoryLook(item.categoryId, categoryName, neutral)
  const Icon = look.icon
  return (
    <div className="m-card" style={{ background: look.gradient, boxShadow: look.shadow }}>
      <IconGripVertical size={18} color="rgba(255,255,255,0.6)" aria-hidden style={{ flex: 'none' }} />
      <span className="act-icon" style={{ width: 40, height: 40, borderRadius: 12 }} aria-hidden>
        <Icon size={20} color="#fff" />
      </span>
      <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span className="act-title" style={{ fontSize: 16 }}>
          {item.title}
        </span>
        <span style={{ fontSize: 13 }}>{categoryName}</span>
      </span>
      <span className="mono" style={{ fontSize: 13, flex: 'none' }}>
        {activityTime(item, true)}
      </span>
    </div>
  )
}

function UpcomingCard({ item, day, index, categoryName }: { item: ActivityListItem; day: Dayjs; index: number; categoryName: string }) {
  const neutral = item.status === 'Cancelled' || categoryName === UNCATEGORIZED
  const look = categoryLook(item.categoryId, categoryName, neutral)
  return (
    <div className="m-upcoming">
      <span style={{ width: 44, display: 'flex', flexDirection: 'column', alignItems: 'center', fontSize: 12, flex: 'none' }}>
        {SHORT[index]}
        <span style={{ fontSize: 20, fontWeight: 800 }}>{day.format('D')}</span>
      </span>
      <span style={{ width: 4, alignSelf: 'stretch', borderRadius: 2, background: `linear-gradient(${look.vivid}, ${look.light})`, flex: 'none' }} />
      <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span className="act-title" style={{ fontSize: 15 }}>
          {item.title}
        </span>
        <span style={{ fontSize: 13 }}>{categoryName}</span>
      </span>
      <span className="mono" style={{ fontSize: 13, flex: 'none' }}>
        {activityTime(item, true)}
      </span>
    </div>
  )
}

// Mobil Pano (taslak: Mobil.dc.html): gradyan başlık + 7 günlük şerit, seçili günün listesi, yaklaşanlar, boş gün çipleri.
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
          <Logo size="compact" plain />
          <div style={{ flex: 1 }} />
          <button type="button" className="icon-btn" style={{ border: 0, background: 'rgba(255,255,255,0.18)' }} aria-label="Önceki hafta" onClick={onPrev}>
            <IconChevronLeft size={20} aria-hidden />
          </button>
          <button type="button" className="icon-btn" style={{ border: 0, background: 'rgba(255,255,255,0.18)' }} aria-label="Sonraki hafta" onClick={onNext}>
            <IconChevronRight size={20} aria-hidden />
          </button>
          <button type="button" className="icon-btn" style={{ border: 0, background: 'rgba(255,255,255,0.18)' }} aria-label="Ara" onClick={() => spotlight.open()}>
            <IconSearch size={20} aria-hidden />
          </button>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <span className="hand" style={{ fontSize: 26 }}>
            {greeting}
          </span>
          <span style={{ fontSize: 30, fontWeight: 800, letterSpacing: '-0.02em', lineHeight: 1.1 }}>
            {selectedDay.format('D MMMM')} {DAY_NAMES[selected]}
          </span>
          <span style={{ fontSize: 14, display: 'flex', alignItems: 'center', gap: 10 }}>
            {loading ? 'Yükleniyor…' : `${isTodaySelected ? 'Bugün' : 'Bu gün'} ${selectedItems.length} planın var`}
            {todayIndex < 0 && (
              <button
                type="button"
                onClick={onToday}
                style={{ background: 'rgba(255,255,255,0.2)', border: 0, color: '#fff', borderRadius: 10, height: 32, padding: '0 12px', fontFamily: 'inherit', cursor: 'pointer' }}
              >
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

      <div className="page-body" style={{ display: 'flex', flexDirection: 'column', gap: 12, paddingTop: 18 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
          <span style={{ fontSize: 17, fontWeight: 700 }}>{isTodaySelected ? 'Bugün' : DAY_NAMES[selected]}</span>
          <span className="hand" style={{ fontSize: 20 }}>
            {selectedItems.length > 0 ? 'sayfan hazır' : 'sayfa boş'}
          </span>
        </div>

        {loading ? (
          <>
            <Skeleton h={64} radius={16} style={{ opacity: 0.35 }} />
            <Skeleton h={64} radius={16} style={{ opacity: 0.35 }} />
          </>
        ) : selectedItems.length === 0 ? (
          <button type="button" className="day-empty ink-card" style={{ minHeight: 140 }} onClick={() => onAddActivity(selectedDay)}>
            <EmptyPage width={84} />
            <span className="hand" style={{ fontSize: 20 }}>
              Boş sayfa, yeni fırsat · eklemek için dokun
            </span>
          </button>
        ) : (
          selectedItems.map((item) => (
            <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen}>
              <TodayCard item={item} categoryName={categoryName(item.categoryId)} />
            </DraggableCard>
          ))
        )}

        {!loading && upcoming.length > 0 && (
          <>
            <span style={{ fontSize: 17, fontWeight: 700, marginTop: 6 }}>Yaklaşanlar</span>
            {upcoming.map(({ item, d, i }) => (
              <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen}>
                <UpcomingCard item={item} day={d} index={i} categoryName={categoryName(item.categoryId)} />
              </DraggableCard>
            ))}
          </>
        )}

        {!loading && emptyDays.length > 0 && (
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <span className="hand" style={{ fontSize: 20 }}>
              Boş günler:
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
          <span style={{ fontSize: 13, opacity: 0.75 }}>Kartı basılı tutup yukarıdaki şeritte bir güne bırakarak taşıyabilirsin.</span>
        )}
      </div>
    </>
  )
}
