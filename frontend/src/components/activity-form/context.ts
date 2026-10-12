import { createContext, useContext } from 'react'
import type { ActivityDetail } from '../../api/types'

export type FormRequest =
  | {
      mode: 'create'
      // Önceden doldurulacak zaman (ISO). Yoksa bir sonraki tam saat, 1 saat süreyle.
      start?: string
      end?: string
      isAllDay?: boolean
      // Toplu aktivite: Tekrarla bölümü açık başlar.
      repeat?: boolean
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
