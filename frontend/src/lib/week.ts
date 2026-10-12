import dayjs, { type Dayjs } from 'dayjs'
import 'dayjs/locale/tr'
import type { ActivityListItem } from '../api/types'

dayjs.locale('tr')

export const DAY_NAMES = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']

// Haftanın başlangıcı (Pazartesi 00:00, tarayıcı yerel saati).
export function startOfWeek(date: Dayjs): Dayjs {
  const offset = (date.day() + 6) % 7
  return date.startOf('day').subtract(offset, 'day')
}

export function dayKey(date: Dayjs): string {
  return date.format('YYYY-MM-DD')
}

export function weekRangeLabel(weekStart: Dayjs): string {
  const end = weekStart.add(6, 'day')
  if (weekStart.month() === end.month()) {
    return `${weekStart.format('D')} – ${end.format('D MMMM YYYY')}`
  }
  if (weekStart.year() === end.year()) {
    return `${weekStart.format('D MMMM')} – ${end.format('D MMMM YYYY')}`
  }
  return `${weekStart.format('D MMMM YYYY')} – ${end.format('D MMMM YYYY')}`
}

// "Çarşamba'ya" gibi yönelme hali eki: günlere göre elle (Türkçe ünlü uyumu).
const DATIVE = ["Pazartesi'ye", "Salı'ya", "Çarşamba'ya", "Perşembe'ye", "Cuma'ya", "Cumartesi'ye", "Pazar'a"]
export function dayNameDative(index: number): string {
  return DATIVE[index] ?? DAY_NAMES[index]
}

// Aktiviteyi hedef güne taşır: yerel saat ve süre korunur.
export function moveToDay(item: Pick<ActivityListItem, 'start' | 'end'>, target: Dayjs): { start: string; end: string } {
  const start = dayjs(item.start)
  const durationMs = dayjs(item.end).diff(start)
  const newStart = target.hour(start.hour()).minute(start.minute()).second(start.second()).millisecond(start.millisecond())
  const newEnd = newStart.add(durationMs, 'millisecond')
  // Npgsql timestamptz yalnızca UTC offset kabul eder (+03:00 ile 500 döner); bu yüzden UTC ISO gönderilir.
  return { start: newStart.toISOString(), end: newEnd.toISOString() }
}

export function sortActivities(items: ActivityListItem[]): ActivityListItem[] {
  return [...items].sort((a, b) => {
    if (a.isAllDay !== b.isAllDay) return a.isAllDay ? -1 : 1
    return dayjs(a.start).valueOf() - dayjs(b.start).valueOf()
  })
}

// Türkçe yönelme eki için sayının okunuşundaki son ünlüye bakılır (saat:dakika sonu: dakika).
const DIGIT_SUFFIX = ['a', 'e', 'ye', 'e', 'e', 'e', 'ya', 'ye', 'e', 'a']
const TENS_SUFFIX: Record<number, string> = { 0: 'a', 10: 'a', 20: 'ye', 30: 'a', 40: 'a', 50: 'ye' }
export function timeDative(minute: number): string {
  return minute % 10 === 0 ? TENS_SUFFIX[minute] : DIGIT_SUFFIX[minute % 10]
}

export function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return [h > 0 ? `${h} sa` : '', m > 0 ? `${m} dk` : ''].filter(Boolean).join(' ') || '0 dk'
}
