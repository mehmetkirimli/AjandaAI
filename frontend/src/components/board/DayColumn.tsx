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
  onOpen: (id: number) => void
}

function DraggableCard({
  item,
  categoryName,
  onOpen,
}: {
  item: ActivityListItem
  categoryName: string
  onOpen: (id: number) => void
}) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: item.id })
  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      onClick={() => onOpen(item.id)}
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

export function DayColumn({ day, index, items, categoryName, loading, isToday, onOpen }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })

  return (
    <Box
      ref={setNodeRef}
      p="xs"
      style={{
        // Çerçevesiz sütun: yalnızca ince ayırıcı; bırakma hedefi hafif zemin + kesikli iç çizgi.
        borderLeft: index === 0 ? 'none' : '1px solid var(--app-line)',
        background: isOver ? 'color-mix(in srgb, var(--mantine-color-accent-filled) 7%, transparent)' : 'transparent',
        outline: isOver ? '1px dashed var(--mantine-color-accent-filled)' : 'none',
        outlineOffset: -4,
        minWidth: 0,
        height: '100%',
        overflowY: 'auto',
        transition: 'background 120ms ease',
      }}
    >
      <Group justify="space-between" px={6} py={2} mb={4} wrap="nowrap">
        <Text fw={500} size="xs" c={isToday ? undefined : "dimmed"}>
          {DAY_NAMES[index]}
        </Text>
        <Box
          w={22}
          h={22}
          style={{
            borderRadius: '50%',
            display: 'grid',
            placeItems: 'center',
            background: isToday ? 'var(--mantine-color-accent-filled)' : 'transparent',
            color: isToday ? 'white' : 'var(--mantine-color-dimmed)',
            fontWeight: 600,
            fontSize: 11,
          }}
        >
          {day.format('D')}
        </Box>
      </Group>
      <Stack gap={6}>
        {loading ? (
          <>
            <Skeleton h={66} radius="md" />
            {index % 2 === 0 && <Skeleton h={66} radius="md" />}
            {index % 3 === 0 && <Skeleton h={66} radius="md" />}
          </>
        ) : items.length === 0 ? (
          <Text size="xs" c="dimmed" ta="center" py="md">
            {isOver ? 'Buraya bırak' : 'Boş'}
          </Text>
        ) : (
          items.map((item) => <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen} />)
        )}
      </Stack>
    </Box>
  )
}
