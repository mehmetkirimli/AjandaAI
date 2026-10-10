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
import { Alert, Box, Button, Center, Group, Paper, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import {
  IconAlertCircle,
  IconCalendarOff,
  IconCalendarWeek,
  IconChevronLeft,
  IconChevronRight,
} from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useMemo, useState } from 'react'
import { fetchActivities, fetchCategories, rescheduleActivity } from '../api/activities'
import { ApiError } from '../api/client'
import type { ActivityListItem } from '../api/types'
import { ActivityCard } from '../components/board/ActivityCard'
import { DayColumn } from '../components/board/DayColumn'
import { columnCollision, columnKeyboardCoordinates } from '../components/board/keyboard'
import { dayKey, dayNameDative, moveToDay, sortActivities, startOfWeek, weekRangeLabel } from '../lib/week'

interface MoveVars {
  item: ActivityListItem
  dayIndex: number
  start: string
  end: string
}

export function BoardPage() {
  const queryClient = useQueryClient()
  const [weekStart, setWeekStart] = useState<Dayjs>(() => startOfWeek(dayjs()))
  const [activeId, setActiveId] = useState<number | null>(null)

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

  const moveMutation = useMutation({
    mutationFn: (vars: MoveVars) => rescheduleActivity(vars.item.id, vars.start, vars.end),
    // İyimser güncelleme: kart hemen yeni yerde görünür.
    onMutate: async (vars) => {
      await queryClient.cancelQueries({ queryKey: activitiesKey })
      const previous = queryClient.getQueryData<ActivityListItem[]>(activitiesKey)
      queryClient.setQueryData<ActivityListItem[]>(activitiesKey, (old) =>
        old?.map((a) => (a.id === vars.item.id ? { ...a, start: vars.start, end: vars.end } : a)),
      )
      return { previous }
    },
    onError: (error, _vars, context) => {
      if (context?.previous) queryClient.setQueryData(activitiesKey, context.previous)
      notifications.show({
        color: 'red',
        title: 'Aktivite taşınamadı',
        message: error instanceof ApiError ? error.errors[0] ?? error.message : 'Beklenmeyen bir hata oluştu.',
      })
    },
    onSuccess: (_data, vars) => {
      notifications.show({
        color: 'teal',
        title: 'Taşındı',
        message: `Aktivite ${dayNameDative(vars.dayIndex)} taşındı`,
        autoClose: 2500,
      })
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: activitiesKey }),
  })

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
    moveMutation.mutate({ item, dayIndex, ...moveToDay(item, days[dayIndex]) })
  }

  const loading = activitiesQuery.isPending
  const isEmpty = !loading && !activitiesQuery.isError && (activitiesQuery.data?.length ?? 0) === 0
  const isCurrentWeek = weekKey === dayKey(startOfWeek(dayjs()))

  return (
    <Stack gap="lg">
      <Group justify="space-between" wrap="wrap">
        <Stack gap={0}>
          <Title order={2}>Haftalık Pano</Title>
          <Text c="dimmed" fw={500}>
            {weekRangeLabel(weekStart)}
          </Text>
        </Stack>
        <Group gap="xs">
          <Button
            variant="default"
            leftSection={<IconChevronLeft size={16} />}
            onClick={() => setWeekStart((w) => w.subtract(7, 'day'))}
          >
            Önceki
          </Button>
          <Button
            variant={isCurrentWeek ? 'light' : 'filled'}
            leftSection={<IconCalendarWeek size={16} />}
            onClick={() => setWeekStart(startOfWeek(dayjs()))}
          >
            Bugün
          </Button>
          <Button
            variant="default"
            rightSection={<IconChevronRight size={16} />}
            onClick={() => setWeekStart((w) => w.add(7, 'day'))}
          >
            Sonraki
          </Button>
        </Group>
      </Group>

      {activitiesQuery.isError && (
        <Alert color="red" variant="light" icon={<IconAlertCircle size={18} />} title="Aktiviteler yüklenemedi">
          <Group justify="space-between">
            <Text size="sm">
              {activitiesQuery.error instanceof ApiError ? activitiesQuery.error.message : 'Beklenmeyen bir hata oluştu.'}
            </Text>
            <Button size="xs" variant="white" onClick={() => activitiesQuery.refetch()}>
              Tekrar dene
            </Button>
          </Group>
        </Alert>
      )}

      {isEmpty && (
        <Paper withBorder radius="lg" p="xl" bg="var(--mantine-color-indigo-light)">
          <Center>
            <Stack align="center" gap="xs">
              <ThemeIcon size={64} radius="xl" variant="white" color="indigo">
                <IconCalendarOff size={34} />
              </ThemeIcon>
              <Title order={4}>Bu hafta boş</Title>
              <Text c="dimmed" ta="center" maw={420}>
                Aktivite ekleme formu yakında, şimdilik Swagger'dan ekleyebilirsin.
              </Text>
            </Stack>
          </Center>
        </Paper>
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
        <Box style={{ overflowX: 'auto', paddingBottom: 8 }}>
          <Box
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(7, minmax(0, 1fr))',
              minWidth: 1100,
              gap: 'var(--mantine-spacing-sm)',
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
                isToday={dayKey(day) === todayKey}
              />
            ))}
          </Box>
        </Box>
        <DragOverlay dropAnimation={{ duration: 200 }}>
          {activeItem ? <ActivityCard item={activeItem} categoryName={categoryName(activeItem.categoryId)} overlay /> : null}
        </DragOverlay>
      </DndContext>
    </Stack>
  )
}
