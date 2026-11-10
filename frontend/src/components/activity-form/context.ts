import { createContext, useContext } from 'react'
import type { ActivityDetail, ActivityUpdateRequest, SeriesCreateRequest } from '../../api/types'

// Asistan önerisinden önceden doldurma (ADR 0025 §3, "Değiştir"). Zaman alanları start/end ile gelir.
export type FormPrefill = Partial<Omit<ActivityUpdateRequest, 'start' | 'end' | 'isAllDay'>> & {
  recurrence?: SeriesCreateRequest['recurrence']
  reminderMinutesBefore?: number | null
}

export type FormRequest =
  | {
      mode: 'create'
      // Önceden doldurulacak zaman (ISO). Yoksa bir sonraki tam saat, 1 saat süreyle.
      start?: string
      end?: string
      isAllDay?: boolean
      // Toplu aktivite: Tekrarla bölümü açık başlar.
      repeat?: boolean
      prefill?: FormPrefill
      // Yalnızca başarılı kayıtta çağrılır (asistan kartı "deftere yazıldı" olur); iptalde çağrılmaz.
      onSaved?: () => void
    }
  | { mode: 'edit'; activity: ActivityDetail }

export interface ActivityFormApi {
  openActivityForm: (request: FormRequest) => void
}

export const ActivityFormContext = createContext<ActivityFormApi | null>(null)

export function useActivityForm(): ActivityFormApi {
  const ctx = useContext(ActivityFormContext)
  if (!ctx) throw new Error('useActivityForm, ActivityFormProvider içinde kullanılmalı.')
  return ctx
}
