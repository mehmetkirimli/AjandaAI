import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  MouseSensor,
  TouchSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { Alert, Box, Button, Group, Text } from '@mantine/core'
import {
  IconAlertCircle,
} from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { registerCommandHandler, runPageCommand } from '../lib/commands'
import { fetchActivities, fetchCategories } from '../api/activities'
import { ApiError } from '../api/client'
import type { ActivityListItem } from '../api/types'
import { useActivityForm } from '../components/activity-form/context'
import { PanelHeader } from '../components/PanelHeader'
import { ActivityDrawer } from '../components/ActivityDrawer'
import { useReschedule } from '../hooks/useReschedule'
import { ActivityCard } from '../components/board/ActivityCard'
import { DayColumn, WeekendCard } from '../components/board/DayColumn'
import { MobileBoard } from '../components/board/MobileBoard'
import { useAuth } from '../auth/context'
import { useLayout } from '../layout/context'
import { greeting } from '../lib/greeting'
import { columnCollision, columnKeyboardCoordinates } from '../components/board/keyboard'
import { UNCATEGORIZED } from '../lib/categoryColor'
import { dayKey, dayNameDative, moveToDay, sortActivities, startOfWeek, weekRangeLabel } from '../lib/week'

// Gece yarısı ve sekme yeniden görünür olduğunda "bugün" vurgusu güncellenir.
function useTodayKey(): string {
  const [key, setKey] = useState(() => dayKey(dayjs()))
  useEffect(() => {
    const refresh = () => setKey(dayKey(dayjs()))
    let timer: ReturnType<typeof setTimeout>
    const schedule = () => {
      const now = dayjs()
      timer = setTimeout(() => {
        refresh()
        schedule()
      }, now.add(1, 'day').startOf('day').diff(now) + 1000)
    }
    schedule()
    const onVisible = () => {
      if (document.visibilityState === 'visible') refresh()
    }
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      clearTimeout(timer)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [])
  return key
}

export function BoardPage() {
  const [weekStart, setWeekStart] = useState<Dayjs>(() => startOfWeek(dayjs()))
  const [activeId, setActiveId] = useState<number | null>(null)
  const [openId, setOpenId] = useState<number | null>(null)

  const todayKey = useTodayKey()
  const weekKey = dayKey(weekStart)
  const activitiesKey = useMemo(() => ['activities', weekKey] as const, [weekKey])
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => weekStart.add(i, 'day')), [weekStart])

  const activitiesQuery = useQuery({
    queryKey: activitiesKey,
    queryFn: () => fetchActivities(weekStart.toISOString(), weekStart.add(7, 'day').subtract(1, 'millisecond').toISOString()),
  })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })

  const categoryName = useCallback(
    (id: number) => categoriesQuery.data?.find((c) => c.id === id)?.name ?? UNCATEGORIZED,
    [categoriesQuery.data],
  )

  const byDay = useMemo(() => {
    const map = new Map<string, ActivityListItem[]>()
    for (const item of activitiesQuery.data ?? []) {
      const key = dayKey(dayjs(item.start))
      map.set(key, [...(map.get(key) ?? []), item])
    }
    for (const [key, list] of map) map.set(key, sortActivities(list))
    return map
  }, [activitiesQuery.data])

  const moveMutation = useReschedule()
  const navigate = useNavigate()
  const { pathname } = useLocation()

  // Komut paleti: Bugüne dön / Önceki / Sonraki hafta.
  useEffect(
    () =>
      registerCommandHandler((command) => {
        if (command === 'today') setWeekStart(startOfWeek(dayjs()))
        else if (command === 'prev') setWeekStart((w) => w.subtract(7, 'day'))
        else if (command === 'next') setWeekStart((w) => w.add(7, 'day'))
      }),
    [],
  )

  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    // Dokunmatik: kısa basılı tutunca sürükleme başlar, böylece dikey kaydırma engellenmez.
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: columnKeyboardCoordinates }),
  )

  const activeItem = activeId === null ? undefined : activitiesQuery.data?.find((a) => a.id === activeId)

  const handleDragStart = (event: DragStartEvent) => setActiveId(Number(event.active.id))

  const handleDragEnd = (event: DragEndEvent) => {
    setActiveId(null)
    const item = activitiesQuery.data?.find((a) => a.id === Number(event.active.id))
    const overId = event.over?.id
    if (!item || typeof overId !== 'string') return
    const dayIndex = days.findIndex((d) => `day-${dayKey(d)}` === overId)
    if (dayIndex < 0 || dayKey(days[dayIndex]) === dayKey(dayjs(item.start))) return
    moveMutation.mutate({
      item,
      ...moveToDay(item, days[dayIndex]),
      successMessage: `Aktivite ${dayNameDative(dayIndex)} taşındı`,
    })
  }

  const loading = activitiesQuery.isPending
  // Gün kartındaki "Aktivite Ekle": o gün 09:00–10:00 önceden seçili.
  const { openActivityForm } = useActivityForm()
  const handleAddActivity = (day: Dayjs) => {
    openActivityForm({
      mode: 'create',
      start: day.hour(9).minute(0).second(0).millisecond(0).toISOString(),
      end: day.hour(10).minute(0).second(0).millisecond(0).toISOString(),
    })
  }
  const { isMobile } = useLayout()
  const { user } = useAuth()
  const hello = greeting(user?.displayName)
  const goToday = () => setWeekStart(startOfWeek(dayjs()))
  const goPrev = () => setWeekStart((w) => w.subtract(7, 'day'))
  const goNext = () => setWeekStart((w) => w.add(7, 'day'))

  const errorBox = activitiesQuery.isError && (
    <Box px={isMobile ? 16 : 34} pt="sm">
      <Alert color="orange" icon={<IconAlertCircle size={16} />} title="Aktiviteler yüklenemedi">
        <Group justify="space-between">
          <Text size="sm">
            {activitiesQuery.error instanceof ApiError ? activitiesQuery.error.message : 'Beklenmeyen bir hata oluştu.'}
          </Text>
          <Button size="compact-sm" variant="sketch" onClick={() => activitiesQuery.refetch()}>
            Tekrar dene
          </Button>
        </Group>
      </Alert>
    </Box>
  )

  const dayProps = (day: Dayjs, i: number) => ({
    day,
    index: i,
    items: byDay.get(dayKey(day)) ?? [],
    categoryName,
    loading,
    onOpen: setOpenId,
    isToday: dayKey(day) === todayKey,
    onAddActivity: handleAddActivity,
  })

  return (
    <>
      {!isMobile && (
        <PanelHeader
          title="Pano"
          kicker={hello}
          meta={weekRangeLabel(weekStart)}
          view="timeGridWeek"
          onViewChange={(v) => {
            if (v !== 'timeGridWeek') runPageCommand(`view:${v}`, pathname, '/calendar', navigate)
          }}
          onToday={goToday}
          onPrev={goPrev}
          onNext={goNext}
          prevLabel="Önceki hafta"
          nextLabel="Sonraki hafta"
        />
      )}

      {!isMobile && errorBox}

      <DndContext
        sensors={sensors}
        collisionDetection={columnCollision}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        onDragCancel={() => setActiveId(null)}
        accessibility={{
          screenReaderInstructions: {
            draggable:
              'Aktiviteyi taşımak için boşluk tuşuna basın. Sağ ve sol ok tuşlarıyla gün seçin, boşluk ile bırakın, Esc ile vazgeçin.',
          },
        }}
      >
        {isMobile ? (
          <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
            <MobileBoard
              greeting={hello}
              days={days}
              byDay={byDay}
              todayKey={todayKey}
              loading={loading}
              categoryName={categoryName}
              onOpen={setOpenId}
              onAddActivity={handleAddActivity}
              onPrev={goPrev}
              onNext={goNext}
              onToday={goToday}
            />
            {errorBox}
          </div>
        ) : (
          <div className="page-body board-page">
            <div className="board-week">
              {days.slice(0, 5).map((day, i) => (
                <DayColumn key={dayKey(day)} {...dayProps(day, i)} />
              ))}
            </div>
            <div className="board-weekend">
              {days.slice(5).map((day, i) => (
                <WeekendCard key={dayKey(day)} {...dayProps(day, i + 5)} />
              ))}
            </div>
          </div>
        )}
        <DragOverlay dropAnimation={{ duration: 200 }}>
          {activeItem ? <ActivityCard item={activeItem} categoryName={categoryName(activeItem.categoryId)} overlay /> : null}
        </DragOverlay>
      </DndContext>
      <ActivityDrawer activityId={openId} onClose={() => setOpenId(null)} categoryName={categoryName} />
    </>
  )
}
