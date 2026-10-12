import dayjs, { type Dayjs } from 'dayjs'

export type Frequency = 'Daily' | 'Weekdays' | 'Weekends' | 'Weekly' | 'Monthly'

export const MAX_OCCURRENCES = 366

export const FREQUENCY_OPTIONS: { value: Frequency; label: string }[] = [
  { value: 'Daily', label: 'Her gün' },
  { value: 'Weekdays', label: 'Hafta içi' },
  { value: 'Weekends', label: 'Hafta sonu' },
  { value: 'Weekly', label: 'Belirli günler' },
  { value: 'Monthly', label: 'Her ay' },
]

export const SHORT_DAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']
const LONG_DAYS = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']

// ISO gün numarası: 1 = Pazartesi ... 7 = Pazar.
export function isoDay(d: Dayjs): number {
  return ((d.day() + 6) % 7) + 1
}

export function defaultUntil(start: Dayjs, frequency: Frequency): string {
  return start.add(frequency === 'Monthly' ? 6 : 1, 'month').format('YYYY-MM-DD')
}

function matches(date: Dayjs, first: Dayjs, frequency: Frequency, days: number[]): boolean {
  const dow = isoDay(date)
  const weekend = dow >= 6
  switch (frequency) {
    case 'Daily':
      return true
    case 'Weekdays':
      return !weekend
    case 'Weekends':
      return weekend
    case 'Weekly':
      return days.includes(dow)
    case 'Monthly':
      // Ay o güne sahip değilse ayın son günü (backend RecurrenceExpander ile aynı kural).
      return date.date() === Math.min(first.date(), date.daysInMonth())
  }
}

export interface Preview {
  count: number
  tooMany: boolean
  invalidRange: boolean
}

// Backend'deki RecurrenceExpander ile aynı kurallar: ilk günden `until` gününe (dahil) her gün taranır.
export function countOccurrences(startDate: string, frequency: Frequency, days: number[], until: string): Preview {
  const first = dayjs(startDate, 'YYYY-MM-DD')
  const last = dayjs(until, 'YYYY-MM-DD')
  if (!first.isValid() || !last.isValid() || last.isBefore(first, 'day')) {
    return { count: 0, tooMany: false, invalidRange: true }
  }
  let count = 0
  for (let d = first; !d.isAfter(last, 'day'); d = d.add(1, 'day')) {
    if (!matches(d, first, frequency, days)) continue
    count++
    if (count > MAX_OCCURRENCES) return { count, tooMany: true, invalidRange: false }
  }
  return { count, tooMany: false, invalidRange: false }
}

export function frequencyLabel(frequency: string, days: number[], startDay?: number): string {
  switch (frequency) {
    case 'Daily':
      return 'Her gün'
    case 'Weekdays':
      return 'Hafta içi'
    case 'Weekends':
      return 'Hafta sonu'
    case 'Weekly': {
      const sorted = [...days].sort((a, b) => a - b)
      return sorted.length === 1 ? `Her ${LONG_DAYS[sorted[0] - 1]}` : `Her ${sorted.map((d) => SHORT_DAYS[d - 1]).join(', ')}`
    }
    case 'Monthly':
      return startDay ? `Her ay ${startDay}. gün` : 'Her ay'
    default:
      return frequency
  }
}
