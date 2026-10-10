import { useDraggable, useDroppable } from '@dnd-kit/core'
import { Box, Group, Skeleton, Stack, Text } from '@mantine/core'
import type { Dayjs } from 'dayjs'
import type { ActivityListItem } from '../../api/types'
import { DAY_NAMES, dayKey } from '../../lib/week'
import { ActivityCard } from './ActivityCard'

interface Props {
  day: Dayjs
  index: number
  items: ActivityListItem[]
  categoryName: (id: number) => string
  loading: boolean
  isToday: boolean
}

function DraggableCard({ item, categoryName }: { item: ActivityListItem; categoryName: string }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: item.id })
  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      aria-label={`${item.title}. Taşımak için boşluk tuşuna basın, sağ ve sol ok tuşlarıyla gün değiştirin.`}
      style={{ touchAction: 'manipulation', outlineOffset: 2, borderRadius: 'var(--mantine-radius-md)' }}
    >
      <ActivityCard
        item={item}
        categoryName={categoryName}
        style={{ opacity: isDragging ? 0.35 : 1, filter: isDragging ? 'grayscale(0.6)' : undefined }}
      />
    </div>
  )
}

export function DayColumn({ day, index, items, categoryName, loading, isToday }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })

  return (
    <Box
      ref={setNodeRef}
      p="xs"
      style={{
        borderRadius: 'var(--mantine-radius-lg)',
        border: isOver
          ? '2px dashed var(--mantine-color-indigo-filled)'
          : isToday
            ? '2px solid var(--mantine-color-indigo-5)'
            : '2px solid var(--mantine-color-default-border)',
        background: isOver ? 'var(--mantine-color-indigo-light)' : 'var(--mantine-color-gray-light)',
        minWidth: 0,
        transition: 'background 120ms ease, border-color 120ms ease',
        minHeight: 360,
      }}
    >
      <Group justify="space-between" px={6} py={6} mb="xs" wrap="nowrap">
        <Text fw={700} size="sm" c={isToday ? 'indigo' : undefined}>
          {DAY_NAMES[index]}
        </Text>
        <Box
          w={30}
          h={30}
          style={{
            borderRadius: '50%',
            display: 'grid',
            placeItems: 'center',
            background: isToday ? 'var(--mantine-color-indigo-filled)' : 'transparent',
            color: isToday ? 'white' : 'var(--mantine-color-dimmed)',
            fontWeight: 700,
            fontSize: 14,
          }}
        >
          {day.format('D')}
        </Box>
      </Group>
      <Stack gap="xs">
        {loading ? (
          <>
            <Skeleton h={88} radius="md" />
            {index % 2 === 0 && <Skeleton h={88} radius="md" />}
            {index % 3 === 0 && <Skeleton h={88} radius="md" />}
          </>
        ) : items.length === 0 ? (
          <Text size="xs" c="dimmed" ta="center" py="md">
            {isOver ? 'Buraya bırak' : 'Boş'}
          </Text>
        ) : (
          items.map((item) => <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} />)
        )}
      </Stack>
    </Box>
  )
}
