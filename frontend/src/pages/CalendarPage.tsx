import type {
  DateSelectArg,
  DatesSetArg,
  EventClickArg,
  EventContentArg,
  EventDropArg,
  EventInput,
} from '@fullcalendar/core'
import trLocale from '@fullcalendar/core/locales/tr'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin, { type EventResizeDoneArg } from '@fullcalendar/interaction'
import FullCalendar from '@fullcalendar/react'
import timeGridPlugin from '@fullcalendar/timegrid'
import { ActionIcon, Button, Loader, SegmentedControl } from '@mantine/core'
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useEffect, useMemo, useRef, useState } from 'react'
import { registerCommandHandler } from '../lib/commands'
import { fetchActivities, fetchCategories } from '../api/activities'
import type { ActivityListItem } from '../api/types'
import { PanelHeader } from '../components/PanelHeader'
import { useActivityForm } from '../components/activity-form/context'
import { ActivityDrawer } from '../components/ActivityDrawer'
import { useReschedule } from '../hooks/useReschedule'
import { categoryFill } from '../lib/categoryColor'
import { DAY_NAMES, formatDuration, moveToDay, timeDative } from '../lib/week'
import './calendar.css'

type ViewType = 'timeGridWeek' | 'dayGridMonth' | 'timeGridDay'
const VIEWS: { value: ViewType; label: string }[] = [
  { value: 'timeGridWeek', label: 'Hafta' },
  { value: 'dayGridMonth', label: 'Ay' },
  { value: 'timeGridDay', label: 'Gün' },
]

const TIME_FORMAT = { hour: '2-digit', minute: '2-digit', hour12: false } as const

function dayName(d: dayjs.Dayjs): string {
  return DAY_NAMES[(d.day() + 6) % 7]
}

function renderEvent(arg: EventContentArg) {
  const month = arg.view.type === 'dayGridMonth'
  const ellipsis = { overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' } as const
  if (month || arg.event.allDay) {
    return (
      <div style={{ ...ellipsis, fontSize: 13, fontWeight: 600 }}>
        {arg.timeText && <span style={{ marginRight: 4, fontSize: 12, fontWeight: 400, opacity: 0.85 }}>{arg.timeText}</span>}
        {arg.event.title}
      </div>
    )
  }
  return (
    <div style={{ overflow: 'hidden', lineHeight: 1.2 }}>
      <div style={{ ...ellipsis, fontWeight: 600, fontSize: 13 }}>{arg.event.title}</div>
      <div style={{ ...ellipsis, opacity: 0.85, fontSize: 12, fontWeight: 400 }}>{arg.timeText}</div>
    </div>
  )
}

export default function CalendarPage() {
  const calendarRef = useRef<FullCalendar>(null)
  const [range, setRange] = useState<{ from: string; to: string } | null>(null)
  const [title, setTitle] = useState('')
  const [viewType, setViewType] = useState<ViewType>('timeGridWeek')
  const [openId, setOpenId] = useState<number | null>(null)
  const reschedule = useReschedule()
  const { openActivityForm } = useActivityForm()

  // Anahtar ['activities', ...] öneki altında: Pano ile aynı önek, mutasyonlar ikisini de günceller.
  const activitiesQuery = useQuery({
    queryKey: ['activities', range?.from, range?.to],
    queryFn: () => (range ? fetchActivities(range.from, range.to) : Promise.resolve([])),
    enabled: range !== null,
    placeholderData: (previous) => previous,
  })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })
  const categoryName = (id: number) => categoriesQuery.data?.find((c) => c.id === id)?.name ?? 'Kategorisiz'

  const items = activitiesQuery.data
  const byId = useMemo(() => new Map((items ?? []).map((a) => [a.id, a])), [items])

  const events = useMemo<EventInput[]>(
    () =>
      (items ?? []).map((a) => {
        // Pano hapıyla aynı kural: iptal veya kategorisiz gri, aksi halde kategori rengi (shade 7).
        const known = categoriesQuery.data?.some((c) => c.id === a.categoryId) ?? true
        const neutral = a.status === 'Cancelled' || !known
        const fill = neutral ? 'var(--mantine-color-gray-7)' : categoryFill(a.categoryId)
        return {
          id: String(a.id),
          title: a.title,
          start: a.isAllDay ? dayjs(a.start).format('YYYY-MM-DD') : a.start,
          end: a.isAllDay ? undefined : a.end,
          allDay: a.isAllDay,
          backgroundColor: fill,
          borderColor: fill,
          textColor: '#ffffff',
          // Tüm gün etkinliğinin süresi uzatılmaz (IsAllDay değişimi F3'ün işi).
          durationEditable: !a.isAllDay,
        }
      }),
    [items, categoriesQuery.data],
  )

  const api = () => calendarRef.current?.getApi()

  // Komut paleti: bugün / önceki / sonraki ve görünüm değiştirme.
  useEffect(
    () =>
      registerCommandHandler((command) => {
        const cal = calendarRef.current?.getApi()
        if (!cal) return
        if (command === 'today') cal.today()
        else if (command === 'prev') cal.prev()
        else if (command === 'next') cal.next()
        else cal.changeView(command.slice('view:'.length))
      }),
    [],
  )

  const handleDatesSet = (info: DatesSetArg) => {
    setTitle(info.view.title)
    setViewType(info.view.type as ViewType)
    const from = info.start.toISOString()
    const to = new Date(info.end.getTime() - 1).toISOString()
    setRange((r) => (r?.from === from && r?.to === to ? r : { from, to }))
  }

  const save = (item: ActivityListItem, start: string, end: string, successMessage: string, revert: () => void) => {
    reschedule.mutate({ item, start, end, successMessage }, { onError: revert })
  }

  const handleDrop = (info: EventDropArg) => {
    const item = byId.get(Number(info.event.id))
    if (!item || !info.event.start) return info.revert()
    const newStart = dayjs(info.event.start)
    if (item.isAllDay) {
      const moved = moveToDay(item, newStart)
      save(item, moved.start, moved.end, `${item.title} ${dayName(newStart)} gününe taşındı`, info.revert)
      return
    }
    const duration = dayjs(item.end).diff(dayjs(item.start))
    const end = info.event.end ?? new Date(info.event.start.getTime() + duration)
    save(
      item,
      info.event.start.toISOString(),
      end.toISOString(),
      `${item.title} ${dayName(newStart)} ${newStart.format('HH:mm')}'${timeDative(newStart.minute())} taşındı`,
      info.revert,
    )
  }

  const handleResize = (info: EventResizeDoneArg) => {
    const item = byId.get(Number(info.event.id))
    if (!item || !info.event.start || !info.event.end) return info.revert()
    const minutes = dayjs(info.event.end).diff(dayjs(info.event.start), 'minute')
    save(
      item,
      info.event.start.toISOString(),
      info.event.end.toISOString(),
      `Süre ${formatDuration(minutes)} oldu`,
      info.revert,
    )
  }

  // Boş alan seçimi: seçilen aralık önceden dolu form açar. Tüm gün seçiminde bitiş (dışlayıcı) bir gün geri alınır.
  const handleSelect = (info: DateSelectArg) => {
    if (info.allDay) {
      const lastDay = dayjs(info.end).subtract(1, 'day')
      const start = dayjs(info.start)
      openActivityForm({
        mode: 'create',
        isAllDay: true,
        start: start.toISOString(),
        end: (lastDay.isBefore(start) ? start : lastDay).toISOString(),
      })
    } else {
      openActivityForm({ mode: 'create', start: info.start.toISOString(), end: info.end.toISOString() })
    }
    info.view.calendar.unselect()
  }

  const handleClick = (info: EventClickArg) => setOpenId(Number(info.event.id))

  return (
    <>
      <PanelHeader title="Takvim" meta={title}>
        {activitiesQuery.isFetching && <Loader size={12} />}
        <SegmentedControl size="xs" value={viewType} data={VIEWS} onChange={(v) => api()?.changeView(v)} />
        <ActionIcon size="md" aria-label="Önceki" onClick={() => api()?.prev()}>
          <IconChevronLeft size={16} />
        </ActionIcon>
        <Button size="compact-sm" variant="default" onClick={() => api()?.today()}>
          Bugün
        </Button>
        <ActionIcon size="md" aria-label="Sonraki" onClick={() => api()?.next()}>
          <IconChevronRight size={16} />
        </ActionIcon>
      </PanelHeader>

      <div style={{ flex: 1, minHeight: 0 }}>
      <FullCalendar
        ref={calendarRef}
        plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
        initialView="timeGridWeek"
        headerToolbar={false}
        locale={trLocale}
        firstDay={1}
        height="100%"
        nowIndicator
        slotMinTime="06:00:00"
        slotMaxTime="24:00:00"
        scrollTime="08:00:00"
        snapDuration="00:15:00"
        slotLabelFormat={TIME_FORMAT}
        eventTimeFormat={TIME_FORMAT}
        allDayText="Tüm gün"
        dayMaxEvents
        eventDisplay="block"
        editable
        eventResizableFromStart={false}
        selectable
        selectMirror
        events={events}
        eventContent={renderEvent}
        datesSet={handleDatesSet}
        eventDrop={handleDrop}
        eventResize={handleResize}
        eventClick={handleClick}
        select={handleSelect}
        // Tüm gün <-> saatli geçişi IsAllDay'i değiştirir (F3); ay görünümünde tüm hücreler allDay sayılır.
        eventAllow={(dropInfo, dragged) =>
          calendarRef.current?.getApi().view.type === 'dayGridMonth' || dropInfo.allDay === dragged?.allDay
        }
      />
      </div>

      <ActivityDrawer activityId={openId} onClose={() => setOpenId(null)} categoryName={categoryName} />
    </>
  )
}
