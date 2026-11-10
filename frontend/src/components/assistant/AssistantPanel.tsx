import { IconArrowRight, IconCalendarSearch, IconCloud, IconConfetti, IconMapPin, IconPencil, IconRefresh, IconSparkles, IconTags, IconX, type Icon } from '@tabler/icons-react'
import { useMutation } from '@tanstack/react-query'
import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { sendAssistantMessage } from '../../api/assistant'
import { ApiError } from '../../api/client'
import type { AssistantChatMessage, AssistantProposal } from '../../api/types'
import { Spiral } from '../illustrations'
import { ProposalCard, type ProposalStatus } from './ProposalCard'
import './assistant.css'

const MAX_LENGTH = 2000
const MAX_HISTORY = 20

// Araç anahtarı → Türkçe rozet (ADR 0025 §3).
const TOOLS: Record<string, { label: string; icon: Icon }> = {
  haftayi_listele: { label: 'haftana baktı', icon: IconCalendarSearch },
  bos_zaman_bul: { label: 'boş saatleri buldu', icon: IconCalendarSearch },
  kategorileri_listele: { label: 'kategorilere baktı', icon: IconTags },
  tatilleri_listele: { label: 'tatillere baktı', icon: IconConfetti },
  hava_durumu: { label: 'hava durumuna baktı', icon: IconCloud },
  yer_ara: { label: 'yeri aradı', icon: IconMapPin },
  aktivite_oner: { label: 'öneri hazırladı', icon: IconPencil },
  seri_oner: { label: 'öneri hazırladı', icon: IconPencil },
}

const SUGGESTIONS = ['Bu hafta neler var?', 'Yarın 2 saat boşluk bul', 'Her pazartesi spor ekle']

type Item =
  | { id: number; role: 'user'; text: string }
  | { id: number; role: 'assistant'; text: string; tools: string[]; proposals: { proposal: AssistantProposal; status: ProposalStatus }[] }
  | { id: number; role: 'error'; text: string; retry: string }

// Union üzerinde dağılan Omit: her kol kendi alanlarını korur.
type NewItem = Item extends infer I ? (I extends Item ? Omit<I, 'id'> : never) : never

function toolBadges(tools: string[]) {
  // Aynı etiket (iki öneri aracı) bir kez gösterilir; bilinmeyen araç gösterilmez.
  const seen = new Set<string>()
  return tools.flatMap((t) => {
    const tool = TOOLS[t]
    if (!tool || seen.has(tool.label)) return []
    seen.add(tool.label)
    return [tool]
  })
}

function errorText(error: unknown): string {
  if (error instanceof ApiError) return error.errors.length ? error.errors.join(' ') : error.message
  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}

interface Props {
  // Panoda ve takvimde true; diğer sayfalarda açma düğmesi ve panel gizlenir (konuşma bellekte kalır).
  available: boolean
  isMobile: boolean
}

// Uygulama içi asistan paneli (ADR 0025 §5, taslak: docs/design/eskiz/Eskiz-Asistan.dc.html).
// Konuşma yalnızca istemcide tutulur; sayfa yenilenince sıfırlanır.
export function AssistantPanel({ available, isMobile }: Props) {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<Item[]>([])
  const [draft, setDraft] = useState('')
  const nextId = useRef(1)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const scrollRef = useRef<HTMLDivElement>(null)
  const openerRef = useRef<HTMLButtonElement>(null)
  // "Yeni sohbet" sonrasında gelen eski yanıtı yok saymak için.
  const session = useRef(0)

  const mutation = useMutation({
    mutationFn: (vars: { message: string; history: AssistantChatMessage[]; session: number }) =>
      sendAssistantMessage({ message: vars.message, history: vars.history }),
    onSuccess: (data, vars) => {
      if (vars.session !== session.current) return
      add({
        role: 'assistant',
        text: data.reply,
        tools: data.usedTools ?? [],
        proposals: (data.proposals ?? []).map((proposal) => ({ proposal, status: 'open' as const })),
      })
    },
    onError: (error, vars) => {
      if (vars.session !== session.current) return
      add({ role: 'error', text: errorText(error), retry: vars.message })
    },
  })

  function add(item: NewItem) {
    setItems((list) => [...list, { ...item, id: nextId.current++ } as Item])
  }

  const visible = open && available

  useEffect(() => {
    if (visible) inputRef.current?.focus()
  }, [visible])

  useEffect(() => {
    const el = scrollRef.current
    if (el) el.scrollTop = el.scrollHeight
  }, [items, mutation.isPending])

  // Geçmiş: hatalar hariç, son 20 mesaj (ADR 0025 §4).
  function historyOf(list: Item[]): AssistantChatMessage[] {
    return list
      .filter((i): i is Exclude<Item, { role: 'error' }> => i.role !== 'error')
      .map((i) => ({ role: i.role, text: i.text }))
      .slice(-MAX_HISTORY)
  }

  function send(text: string, retry = false) {
    const message = text.trim()
    if (!message || message.length > MAX_LENGTH || mutation.isPending) return
    let history: AssistantChatMessage[]
    if (retry) {
      // Başarısız mesaj listede kalır; hata satırı kalkar, geçmiş o mesajdan öncesidir.
      const list = items.filter((i) => i.role !== 'error')
      const last = list[list.length - 1]
      history = historyOf(last?.role === 'user' && last.text === message ? list.slice(0, -1) : list)
      setItems(list)
    } else {
      history = historyOf(items)
      add({ role: 'user', text: message })
      setDraft('')
    }
    mutation.mutate({ message, history, session: session.current })
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    send(draft)
  }

  function onInputKey(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault()
      send(draft)
    }
  }

  function close() {
    setOpen(false)
    requestAnimationFrame(() => openerRef.current?.focus())
  }

  function reset() {
    session.current++
    mutation.reset()
    setItems([])
    setDraft('')
    inputRef.current?.focus()
  }

  function setProposalStatus(itemId: number, index: number, status: ProposalStatus) {
    setItems((list) =>
      list.map((i) =>
        i.id === itemId && i.role === 'assistant'
          ? { ...i, proposals: i.proposals.map((p, n) => (n === index ? { ...p, status } : p)) }
          : i,
      ),
    )
  }

  if (!available) return null

  const tooLong = draft.length > MAX_LENGTH

  return (
    <>
      {!visible && (
        <button
          ref={openerRef}
          type="button"
          className="assistant-opener"
          data-mobile={isMobile}
          aria-label="Asistanı aç"
          onClick={() => setOpen(true)}
        >
          <IconSparkles size={18} stroke={1.7} aria-hidden />
          asistana sor
        </button>
      )}

      {visible && (
        <aside
          aria-label="Asistan"
          className="assistant-panel"
          data-mobile={isMobile}
          onKeyDown={(e) => {
            if (e.key === 'Escape') {
              e.stopPropagation()
              close()
            }
          }}
        >
          {!isMobile && <Spiral />}
          <header className="assistant-header">
            <IconSparkles size={26} stroke={1.7} aria-hidden />
            <div className="assistant-heading">
              <span className="assistant-title">asistan</span>
              <span className="assistant-subtitle">ajandanı benim için düzenler</span>
            </div>
            <div style={{ flex: 1 }} />
            <button type="button" className="assistant-icon-btn" aria-label="Yeni sohbet" title="Yeni sohbet" onClick={reset}>
              <IconRefresh size={16} stroke={1.8} aria-hidden />
            </button>
            <button type="button" className="assistant-icon-btn" aria-label="Kapat" title="Kapat" onClick={close}>
              <IconX size={16} stroke={1.8} aria-hidden />
            </button>
          </header>

          <div ref={scrollRef} className="assistant-thread" aria-live="polite">
            {items.length === 0 && !mutation.isPending && (
              <span className="assistant-note">merhaba! haftana bakabilir, boş saat bulabilir, ekleyeceğin şeyi önerebilirim.</span>
            )}
            {items.map((item) => {
              if (item.role === 'user') {
                return (
                  <div key={item.id} className="assistant-user">
                    {item.text}
                  </div>
                )
              }
              if (item.role === 'error') {
                return (
                  <div key={item.id} className="assistant-error" role="alert">
                    <span>{item.text}</span>
                    <button type="button" className="assistant-btn" onClick={() => send(item.retry, true)} disabled={mutation.isPending}>
                      Tekrar dene
                    </button>
                  </div>
                )
              }
              const badges = toolBadges(item.tools)
              return (
                <div key={item.id} className="assistant-turn">
                  {badges.length > 0 && (
                    <div className="assistant-badges">
                      {badges.map(({ label, icon: BadgeIcon }) => (
                        <span key={label} className="assistant-badge">
                          <BadgeIcon size={13} stroke={1.8} aria-hidden />
                          {label}
                        </span>
                      ))}
                    </div>
                  )}
                  {item.text && <div className="assistant-reply">{item.text}</div>}
                  {item.proposals.map((p, n) => (
                    <ProposalCard
                      key={n}
                      proposal={p.proposal}
                      status={p.status}
                      onStatus={(status) => setProposalStatus(item.id, n, status)}
                    />
                  ))}
                </div>
              )
            })}
            {mutation.isPending && <span className="assistant-note assistant-typing">yazıyor…</span>}
          </div>

          <div className="assistant-chips">
            {SUGGESTIONS.map((s) => (
              <button key={s} type="button" className="assistant-chip" disabled={mutation.isPending} onClick={() => send(s)}>
                {s}
              </button>
            ))}
          </div>

          <form className="assistant-input" onSubmit={onSubmit} data-invalid={tooLong}>
            <label htmlFor="assistant-message" className="assistant-sr">
              Mesaj
            </label>
            <textarea
              id="assistant-message"
              ref={inputRef}
              rows={2}
              value={draft}
              placeholder="aklına geleni yaz…"
              aria-describedby="assistant-counter"
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={onInputKey}
            />
            <button type="submit" className="assistant-send" aria-label="Gönder" disabled={!draft.trim() || tooLong || mutation.isPending}>
              <IconArrowRight size={18} stroke={2} aria-hidden />
            </button>
          </form>
          <div className="assistant-foot">
            <span>Gemini · ücretsiz katman · demo verisiyle kullan</span>
            <span id="assistant-counter" data-invalid={tooLong}>
              {draft.length} / {MAX_LENGTH}
            </span>
          </div>
        </aside>
      )}
    </>
  )
}
