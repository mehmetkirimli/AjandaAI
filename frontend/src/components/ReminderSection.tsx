import { ActionIcon, Badge, Button, Group, Stack, Text, TextInput } from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { IconBell, IconTrash } from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useState } from 'react'
import { createReminder, deleteReminder, fetchReminders } from '../api/reminders'
import type { ActivityDetail } from '../api/types'
import { ApiError } from '../api/client'

type Choice = '15' | '60' | '1440' | 'custom'
const QUICK: { value: Exclude<Choice, 'custom'>; minutes: number; label: string }[] = [
  { value: '15', minutes: 15, label: '15 dk önce' },
  { value: '60', minutes: 60, label: '1 saat önce' },
  { value: '1440', minutes: 1440, label: '1 gün önce' },
]

// Sunucu hataları (geçmiş olamaz, başlangıçtan sonra olamaz...) satırın altında gösterilir.
function messagesOf(error: unknown): string[] {
  if (error instanceof ApiError) return error.errors.length > 0 ? error.errors : [error.message]
  return ['Beklenmeyen bir hata oluştu.']
}

function relativeLabel(activityStart: dayjs.Dayjs, remindAt: dayjs.Dayjs): string {
  const minutes = activityStart.diff(remindAt, 'minute')
  if (minutes === 15) return '15 dk önce'
  if (minutes === 60) return '1 saat önce'
  if (minutes === 1440) return '1 gün önce'
  return minutes > 0 ? 'Başlangıçtan önce' : 'Başlangıçta'
}

// Hızlı seçenekler aktivitenin başlangıcından geriye hesaplanır; tüm gün aktivitelerde başlangıç günün 00:00'ıdır.
function baseStart(activity: ActivityDetail): dayjs.Dayjs {
  return activity.isAllDay ? dayjs(activity.start).startOf('day') : dayjs(activity.start)
}

export function ReminderSection({ activity }: { activity: ActivityDetail }) {
  const queryClient = useQueryClient()
  const key = ['reminders', activity.id] as const
  const remindersQuery = useQuery({ queryKey: key, queryFn: () => fetchReminders(activity.id) })
  const [choice, setChoice] = useState<Choice>('15')
  const [custom, setCustom] = useState<string | null>(null)
  const [note, setNote] = useState('')
  const [errors, setErrors] = useState<string[]>([])

  const start = baseStart(activity)
  const now = dayjs()

  const remindAt = (): dayjs.Dayjs | null => {
    if (choice === 'custom') return custom ? dayjs(custom) : null
    const q = QUICK.find((o) => o.value === choice)
    return q ? start.subtract(q.minutes, 'minute') : null
  }

  const addMutation = useMutation({
    mutationFn: (at: dayjs.Dayjs) => createReminder(activity.id, at.toISOString(), note.trim() || null),
    onSuccess: () => {
      setNote('')
      setCustom(null)
      setErrors([])
      void queryClient.invalidateQueries({ queryKey: key })
    },
    onError: (error) => setErrors(messagesOf(error)),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteReminder(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: key }),
    onError: (error) => setErrors(messagesOf(error)),
  })

  const handleAdd = () => {
    const at = remindAt()
    if (!at) {
      setErrors(['Hatırlatma zamanı seçin.'])
      return
    }
    if (at.isBefore(dayjs())) {
      setErrors(['Hatırlatma zamanı geçmiş bir tarih olamaz.'])
      return
    }
    if (at.isAfter(dayjs(activity.start))) {
      setErrors(['Hatırlatma zamanı, aktivitenin başlangıcından sonra olamaz.'])
      return
    }
    setErrors([])
    addMutation.mutate(at)
  }

  return (
    <Stack gap="xs">
      <Group gap={6} c="dimmed">
        <IconBell size={14} />
        <Text size="xs" fw={500}>
          Hatırlatmalar
        </Text>
      </Group>

      {remindersQuery.data?.length === 0 && (
        <Text size="xs" c="dimmed">
          Henüz hatırlatma yok.
        </Text>
      )}
      {remindersQuery.data?.map((r) => {
        const at = dayjs(r.remindAt)
        return (
          <Group key={r.id} justify="space-between" wrap="nowrap" align="flex-start">
            <Stack gap={0} style={{ minWidth: 0 }}>
              <Text size="sm">
                {relativeLabel(start, at)} · {at.format('D MMM HH:mm')}
              </Text>
              {r.note && (
                <Text size="xs" c="dimmed" style={{ overflowWrap: 'anywhere' }}>
                  {r.note}
                </Text>
              )}
            </Stack>
            <Group gap={4} wrap="nowrap">
              {r.isSent && (
                <Badge size="xs" variant="light" color="gray">
                  Gönderildi
                </Badge>
              )}
              <ActionIcon
                size="sm"
                aria-label="Hatırlatmayı sil"
                loading={deleteMutation.isPending && deleteMutation.variables === r.id}
                onClick={() => deleteMutation.mutate(r.id)}
              >
                <IconTrash size={14} />
              </ActionIcon>
            </Group>
          </Group>
        )
      })}

      <Stack gap={6} mt={4}>
        <Group gap={6}>
          {QUICK.map((o) => {
            const past = start.subtract(o.minutes, 'minute').isBefore(now)
            return (
              <Button
                key={o.value}
                size="compact-sm"
                radius="xl"
                variant={choice === o.value ? 'light' : 'default'}
                disabled={past}
                onClick={() => setChoice(o.value)}
              >
                {o.label}
              </Button>
            )
          })}
          <Button
            size="compact-sm"
            radius="xl"
            variant={choice === 'custom' ? 'light' : 'default'}
            onClick={() => setChoice('custom')}
          >
            Özel…
          </Button>
        </Group>
        {choice === 'custom' && (
          <DateTimePicker
            size="xs"
            placeholder="Tarih ve saat seç"
            valueFormat="D MMM YYYY HH:mm"
            value={custom}
            onChange={setCustom}
            minDate={now.format('YYYY-MM-DD')}
          />
        )}
        <Group gap="xs" wrap="nowrap">
          <TextInput
            size="xs"
            style={{ flex: 1 }}
            placeholder="Not (isteğe bağlı)"
            value={note}
            onChange={(e) => setNote(e.currentTarget.value)}
          />
          <Button size="compact-md" loading={addMutation.isPending} onClick={handleAdd}>
            Ekle
          </Button>
        </Group>
        {errors.map((m) => (
          <Text key={m} size="xs" c="red">
            {m}
          </Text>
        ))}
        {activity.isAllDay && (
          <Text size="xs" c="dimmed">
            Tüm gün aktivitelerinde süreler günün 00:00'ından hesaplanır.
          </Text>
        )}
      </Stack>
    </Stack>
  )
}
