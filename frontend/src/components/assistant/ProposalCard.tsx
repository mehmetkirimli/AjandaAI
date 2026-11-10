import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import { createActivity, createSeries, fetchCategories } from '../../api/activities'
import type { AssistantProposal } from '../../api/types'
import { categoryLook } from '../../lib/categoryColor'
import { splitErrors } from '../../lib/formErrors'
import { frequencyLabel } from '../../lib/recurrence'
import { useActivityForm } from '../activity-form/context'

export type ProposalStatus = 'open' | 'saved' | 'dismissed'

interface Props {
  proposal: AssistantProposal
  status: ProposalStatus
  onStatus: (status: ProposalStatus) => void
}

// Asistanın önerisi (ADR 0025 §3): asistan kayıt oluşturmaz; yalnızca kullanıcı "Deftere yaz" derse mevcut
// POST /api/activities veya /series çağrılır, gövde öneriyle birebir aynıdır.
export function ProposalCard({ proposal, status, onStatus }: Props) {
  const queryClient = useQueryClient()
  const { openActivityForm } = useActivityForm()
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })

  const body = proposal.kind === 'series' ? proposal.series : proposal.activity

  const mutation = useMutation({
    mutationFn: async () => {
      if (proposal.kind === 'series' && proposal.series) await createSeries(proposal.series)
      else if (proposal.activity) await createActivity(proposal.activity)
      else throw new Error('Öneri boş.')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      onStatus('saved')
    },
  })

  if (!body) return null

  const category = categoriesQuery.data?.find((c) => c.id === body.categoryId)
  const look = categoryLook(body.categoryId, category?.name)
  const Icon = look.icon
  const start = dayjs(body.start).locale('tr')
  const end = dayjs(body.end).locale('tr')
  const series = proposal.kind === 'series' ? proposal.series : null
  const when = series
    ? `${frequencyLabel(series.recurrence.frequency, series.recurrence.daysOfWeek, start.date())} · ${dayjs(series.recurrence.until).locale('tr').format('D MMMM')} tarihine kadar`
    : start.format('dddd D MMMM')
  const time = body.isAllDay ? 'tüm gün' : `${start.format('HH:mm')} – ${end.format('HH:mm')}`
  const errors = mutation.isError ? splitErrors(mutation.error, {}).general : []

  const edit = () =>
    openActivityForm({
      mode: 'create',
      start: body.start,
      end: body.end,
      isAllDay: body.isAllDay,
      prefill: {
        ...body,
        ...(series ? { recurrence: series.recurrence, reminderMinutesBefore: series.reminderMinutesBefore } : {}),
      },
      onSaved: () => onStatus('saved'),
    })

  return (
    <div className="assistant-proposal" data-status={status}>
      <div className="assistant-proposal-head">
        <span className="assistant-proposal-line" style={{ background: look.line }} />
        <Icon size={18} stroke={1.7} aria-hidden />
        <span className="assistant-proposal-title">
          <span className="assistant-proposal-name">{body.title}</span>
          <span className="assistant-proposal-meta">
            {category?.name ?? 'Kategori'} · {when}
          </span>
        </span>
        <span className="assistant-proposal-time">{time}</span>
      </div>

      {status === 'saved' && <span className="assistant-proposal-done">deftere yazıldı ✓</span>}
      {status === 'dismissed' && <span className="assistant-proposal-done">vazgeçildi</span>}
      {status === 'open' && (
        <>
          {errors.map((e) => (
            <span key={e} className="assistant-error-text" role="alert">
              {e}
            </span>
          ))}
          <div className="assistant-proposal-actions">
            <button type="button" className="assistant-btn-primary" disabled={mutation.isPending} onClick={() => mutation.mutate()}>
              {mutation.isPending ? 'yazılıyor…' : 'Deftere yaz'}
            </button>
            <button type="button" className="assistant-btn" disabled={mutation.isPending} onClick={edit}>
              Değiştir
            </button>
            <button type="button" className="assistant-btn" disabled={mutation.isPending} onClick={() => onStatus('dismissed')}>
              Vazgeç
            </button>
          </div>
        </>
      )}
    </div>
  )
}
