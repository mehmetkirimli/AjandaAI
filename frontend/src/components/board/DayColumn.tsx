import { useDraggable, useDroppable } from '@dnd-kit/core'
import { Box, Button, Skeleton, Stack, Text } from '@mantine/core'
import { IconCoffee, IconPlus } from '@tabler/icons-react'
import type { Dayjs } from 'dayjs'
import type { ActivityListItem } from '../../api/types'
import { dayKey } from '../../lib/week'
import { ActivityCard } from './ActivityCard'

const SHORT_DAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

interface Props {
  day: Dayjs
  index: number
  items: ActivityListItem[]
  categoryName: (id: number) => string
  loading: boolean
  isToday: boolean
  onOpen: (id: number) => void
  // F3'te o günle ekleme formu açılacak.
  onAddActivity: (day: Dayjs) => void
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
      style={{ touchAction: 'manipulation', outlineOffset: 2, borderRadius: 8 }}
    >
      <ActivityCard
        item={item}
        categoryName={categoryName}
        style={{ opacity: isDragging ? 0.35 : 1, filter: isDragging ? 'grayscale(0.6)' : undefined }}
      />
    </div>
  )
}

export function DayColumn({ day, index, items, categoryName, loading, isToday, onOpen, onAddActivity }: Props) {
  const { setNodeRef, isOver } = useDroppable({ id: `day-${dayKey(day)}` })
  const weekend = index >= 5

  return (
    <Box
      ref={setNodeRef}
      className={weekend ? 'board-day weekend' : 'board-day'}
      p="xs"
      style={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: 160,
        background: isOver
          ? 'color-mix(in srgb, var(--mantine-color-accent-filled) 6%, var(--app-surface))'
          : 'var(--app-surface)',
        border: isOver ? '1px dashed var(--mantine-color-accent-filled)' : '1px solid var(--app-line)',
        borderRadius: 10,
        transition: 'background 120ms ease',
        minWidth: 0,
      }}
    >
      {/* Gün başlığı: "12 Pzt"; bugün parlayan accent rozet. */}
      <Box mb={8} px={4}>
        <Text
          span
          fz={12.5}
          fw={600}
          style={{
            display: 'inline-block',
            padding: '1px 9px',
            marginLeft: -6,
            borderRadius: 999,
            background: isToday ? 'var(--mantine-color-accent-filled)' : 'transparent',
            color: isToday ? '#fff' : undefined,
            boxShadow: isToday ? '0 0 12px rgba(94, 106, 210, 0.45)' : undefined,
          }}
        >
          {day.format('D')} {SHORT_DAYS[index]}
        </Text>
      </Box>

      <Stack gap={6} style={{ flex: 1 }}>
        {loading ? (
          <>
            <Skeleton h={40} radius={8} />
            {index % 2 === 0 && <Skeleton h={40} radius={8} />}
          </>
        ) : items.length === 0 ? (
          <button type="button" className="day-dropzone" data-over={isOver} onClick={() => onAddActivity(day)}>
            <IconCoffee size={18} aria-hidden />
            <span>{isOver ? 'Buraya bırak' : 'Boş · eklemek için tıkla'}</span>
          </button>
        ) : (
          items.map((item) => (
            <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen} />
          ))
        )}
      </Stack>

      {items.length > 0 && (
      <Button
        mt={8}
        size="compact-sm"
        variant="subtle"
        color="gray"
        justify="center"
        className="board-add"
        leftSection={<IconPlus size={14} />}
        onClick={() => onAddActivity(day)}
      >
        Aktivite Ekle
      </Button>
      )}
    </Box>
  )
}
