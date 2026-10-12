import { useDraggable, useDroppable } from '@dnd-kit/core'
import { Box, Button, Skeleton, Stack, Text } from '@mantine/core'
import { IconPlus } from '@tabler/icons-react'
import type { Dayjs } from 'dayjs'
import type { ActivityListItem } from '../../api/types'
import { dayKey } from '../../lib/week'
import { ActivityCard } from './ActivityCard'

const SHORT_DAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']

// Boş gün illüstrasyonu (emoji) ve mesajları; gün numarasına göre sabit seçilir (render'da değişmez).
const EMPTY_WEEKDAY = [
  { emoji: '☕', text: 'Harika, bugün için planlanan bir şey yok!' },
  { emoji: '✨', text: 'Boş gün, kahveni al ☕' },
  { emoji: '🌤️', text: 'Bugün sakin geçiyor.' },
]
const EMPTY_WEEKEND = [
  { emoji: '🌙', text: 'Dinlenme zamanı!' },
  { emoji: '😴', text: 'Hafta sonu senin' },
]

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
  const pool = weekend ? EMPTY_WEEKEND : EMPTY_WEEKDAY
  const empty = pool[day.date() % pool.length]

  return (
    <Box
      ref={setNodeRef}
      className={weekend ? 'board-day weekend' : 'board-day'}
      p="xs"
      style={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: weekend ? 190 : 260,
        background: isOver
          ? 'color-mix(in srgb, var(--mantine-color-accent-filled) 7%, var(--app-surface))'
          : 'var(--app-surface)',
        border: isOver ? '1px dashed var(--mantine-color-accent-filled)' : '1px solid var(--app-line)',
        borderRadius: 10,
        transition: 'background 120ms ease',
        minWidth: 0,
      }}
    >
      {/* Gün başlığı: "12 Pzt"; bugün indigo dolu hap. */}
      <Box mb={8} px={4}>
        <Text
          span
          fz={13.5}
          fw={600}
          style={{
            display: 'inline-block',
            padding: '2px 10px',
            marginLeft: -6,
            borderRadius: 999,
            background: isToday ? 'var(--mantine-color-accent-filled)' : 'transparent',
            color: isToday ? '#fff' : undefined,
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
          <Stack align="center" justify="center" gap={4} style={{ flex: 1 }} py="md">
            <Text lh={1} mb={6} aria-hidden style={{ fontSize: 40 }}>
              {isOver ? '📥' : empty.emoji}
            </Text>
            <Text fz={13} c="dimmed" ta="center" maw={220}>
              {isOver ? 'Buraya bırak' : empty.text}
            </Text>
          </Stack>
        ) : (
          items.map((item) => (
            <DraggableCard key={item.id} item={item} categoryName={categoryName(item.categoryId)} onOpen={onOpen} />
          ))
        )}
      </Stack>

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
    </Box>
  )
}
