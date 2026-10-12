import { useHotkeys } from '@mantine/hooks'
import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { ActivityFormContext, type ActivityFormApi, type FormRequest } from './context'
import { ActivityFormModal } from './ActivityFormModal'

// Formu uygulamanın her yerinden açan sağlayıcı. "C" kısayolu (odak input'ta değilken) boş form açar.
export function ActivityFormProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ request: FormRequest; nonce: number } | null>(null)

  const openActivityForm = useCallback((request: FormRequest) => {
    setState((s) => (s ? s : { request, nonce: Date.now() }))
  }, [])

  useHotkeys([['c', () => openActivityForm({ mode: 'create' })]])

  const api = useMemo<ActivityFormApi>(() => ({ openActivityForm }), [openActivityForm])

  return (
    <ActivityFormContext.Provider value={api}>
      {children}
      {state && <ActivityFormModal key={state.nonce} request={state.request} onClose={() => setState(null)} />}
    </ActivityFormContext.Provider>
  )
}
