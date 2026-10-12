import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { ActionIcon, Alert, Box, Button, Group, Text } from '@mantine/core'
import {
  IconAlertCircle,
  IconChevronLeft,
  IconChevronRight,
} from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { registerCommandHandler } from '../lib/commands'
import { fetchActivities, fetchCategories } from '../api/activities'
import { ApiError } from '../api/client'
import type { ActivityListItem } from '../api/types'
import { PanelHeader } from '../components/PanelHeader'
import { ActivityDrawer } from '../components/ActivityDrawer'
import { useReschedule } from '../hooks/useReschedule'
import { ActivityCard } from '../components/board/ActivityCard'
import { DayColumn } from '../components/board/DayColumn'
import { columnCollision, columnKeyboardCoordinates } from '../components/board/keyboard'
import { dayKey, dayNameDative, moveToDay, sortActivities, startOfWeek, weekRangeLabel } from '../lib/week'

export function BoardPage() {
  const [weekStart, setWeekStart] = useState<Dayjs>(() => startOfWeek(dayjs()))
  const [activeId, setActiveId] = useState<number | null>(null)
  const [openId, setOpenId] = useState<number | null>(null)

  const [todayKey] = useState(() => dayKey(dayjs()))
  const weekKey = dayKey(weekStart)
  const activitiesKey = useMemo(() => ['activities', weekKey] as const, [weekKey])
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => weekStart.add(i, 'day')), [weekStart])

  const activitiesQuery = useQuery({
    queryKey: activitiesKey,
    queryFn: () => fetchActivities(weekStart.toISOString(), weekStart.add(7, 'day').subtract(1, 'millisecond').toISOString()),
  })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })

  const categoryName = useCallback(
    (id: number) => categoriesQuery.data?.find((c) => c.id === id)?.name ?? 'Kategorisiz',
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
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
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
  const isEmpty = !loading && !activitiesQuery.isError && (activitiesQuery.data?.length ?? 0) === 0
  const isCurrentWeek = weekKey === dayKey(startOfWeek(dayjs()))

  return (
    <>
      <PanelHeader title="Pano" meta={weekRangeLabel(weekStart)}>
        <ActionIcon size="md" aria-label="Önceki hafta" onClick={() => setWeekStart((w) => w.subtract(7, 'day'))}>
          <IconChevronLeft size={16} />
        </ActionIcon>
        <Button size="compact-sm" variant="default" disabled={isCurrentWeek} onClick={() => setWeekStart(startOfWeek(dayjs()))}>
          Bugün
        </Button>
        <ActionIcon size="md" aria-label="Sonraki hafta" onClick={() => setWeekStart((w) => w.add(7, 'day'))}>
          <IconChevronRight size={16} />
        </ActionIcon>
      </PanelHeader>

      {activitiesQuery.isError && (
        <Box px="md" pt="sm">
          <Alert color="red" icon={<IconAlertCircle size={16} />} title="Aktiviteler yüklenemedi">
            <Group justify="space-between">
              <Text size="sm">
                {activitiesQuery.error instanceof ApiError ? activitiesQuery.error.message : 'Beklenmeyen bir hata oluştu.'}
              </Text>
              <Button size="compact-sm" variant="default" onClick={() => activitiesQuery.refetch()}>
                Tekrar dene
              </Button>
            </Group>
          </Alert>
        </Box>
      )}

      {isEmpty && (
        <Text size="xs" c="dimmed" px="md" py={8}>
          Bu hafta boş. Aktivite ekleme formu yakında, şimdilik Swagger'dan ekleyebilirsin.
        </Text>
      )}

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
        <Box style={{ flex: 1, minHeight: 0, overflowX: 'auto' }}>
          <Box
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(7, minmax(0, 1fr))',
              minWidth: 980,
              height: '100%',
            }}
          >
            {days.map((day, i) => (
              <DayColumn
                key={dayKey(day)}
                day={day}
                index={i}
                items={byDay.get(dayKey(day)) ?? []}
                categoryName={categoryName}
                loading={loading}
                onOpen={setOpenId}
                isToday={dayKey(day) === todayKey}
              />
            ))}
          </Box>
        </Box>
        <DragOverlay dropAnimation={{ duration: 200 }}>
          {activeItem ? <ActivityCard item={activeItem} categoryName={categoryName(activeItem.categoryId)} overlay /> : null}
        </DragOverlay>
      </DndContext>
      <ActivityDrawer activityId={openId} onClose={() => setOpenId(null)} categoryName={categoryName} />
    </>
  )
}
