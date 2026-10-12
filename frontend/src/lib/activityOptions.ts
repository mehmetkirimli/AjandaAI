import type { ActivityStatus, Level } from '../api/types'

export const STATUS_OPTIONS: { value: ActivityStatus; label: string }[] = [
  { value: 'Planned', label: 'Planlandı' },
  { value: 'InProgress', label: 'Devam ediyor' },
  { value: 'Completed', label: 'Tamamlandı' },
  { value: 'Cancelled', label: 'İptal' },
]

export const LEVEL_OPTIONS: { value: Level; label: string }[] = [
  { value: 'Low', label: 'Düşük' },
  { value: 'Medium', label: 'Orta' },
  { value: 'High', label: 'Yüksek' },
]

export function statusLabel(value: string): string {
  return STATUS_OPTIONS.find((o) => o.value === value)?.label ?? value
}

export function levelLabel(value: string): string {
  return LEVEL_OPTIONS.find((o) => o.value === value)?.label ?? value
}
