import { Alert, Badge, Button, Divider, Drawer, Group, Loader, Modal, Stack, Text, Title } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconArchive, IconPencil, IconRepeat, IconBolt, IconCalendar, IconCoin, IconFlag, IconInfoCircle, IconMapPin } from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useState, type ReactNode } from 'react'
import { deactivateActivity, deactivateSeries, fetchActivity } from '../api/activities'
import { ApiError } from '../api/client'
import { categoryColor } from '../lib/categoryColor'
import { frequencyLabel } from '../lib/recurrence'
import { useActivityForm } from './activity-form/context'
import { ReminderSection } from './ReminderSection'

const STATUS: Record<string, string> = {
  Planned: 'Planlandı',
  InProgress: 'Devam ediyor',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal edildi',
}
const LEVEL: Record<string, string> = { Low: 'Düşük', Medium: 'Orta', High: 'Yüksek' }
const PRIORITY_COLOR: Record<string, string> = { High: 'red', Medium: 'yellow', Low: 'gray' }

function Row({ icon, label, children }: { icon: ReactNode; label: string; children: ReactNode }) {
  return (
    <Group gap="sm" wrap="nowrap" align="flex-start">
      <Text c="dimmed" mt={2}>
        {icon}
      </Text>
      <Stack gap={0}>
        <Text size="xs" c="dimmed">
          {label}
        </Text>
        <Text size="sm" component="div">
          {children}
        </Text>
      </Stack>
    </Group>
  )
}

function formatRange(start: string, end: string, allDay: boolean): string {
  const s = dayjs(start)
  const e = dayjs(end)
  if (allDay) return `${s.format('D MMMM YYYY dddd')} · Tüm gün`
  if (s.isSame(e, 'day')) return `${s.format('D MMMM YYYY dddd')} · ${s.format('HH:mm')} – ${e.format('HH:mm')}`
  return `${s.format('D MMM HH:mm')} – ${e.format('D MMM YYYY HH:mm')}`
}

interface Props {
  activityId: number | null
  onClose: () => void
  categoryName: (id: number) => string
}

// Salt okunur detay paneli (düzenleme F3'te). Pano ve Takvim aynı bileşeni kullanır.
export function ActivityDrawer({ activityId, onClose, categoryName }: Props) {
  const query = useQuery({
    queryKey: ['activity', activityId],
    queryFn: () => fetchActivity(activityId as number),
    enabled: activityId !== null,
  })
  const a = query.data
  const queryClient = useQueryClient()
  const { openActivityForm } = useActivityForm()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [seriesConfirmOpen, setSeriesConfirmOpen] = useState(false)
  const stopSeries = useMutation({
    mutationFn: () => deactivateSeries(a?.series?.id ?? 0, dayjs(a?.start).format('YYYY-MM-DD')),
    onSuccess: (result) => {
      setSeriesConfirmOpen(false)
      onClose()
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      void queryClient.invalidateQueries({ queryKey: ['activity'] })
      notifications.show({ message: `${result.deactivatedCount} aktivite durduruldu`, autoClose: 2500 })
    },
    onError: (error) =>
      notifications.show({
        color: 'red',
        title: 'Seri durdurulamadı',
        message: error instanceof ApiError ? error.message : 'Beklenmeyen bir hata oluştu.',
      }),
  })
  const deactivate = useMutation({
    mutationFn: () => deactivateActivity(activityId as number),
    onSuccess: () => {
      setConfirmOpen(false)
      onClose()
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      void queryClient.invalidateQueries({ queryKey: ['activity'] })
      notifications.show({ message: 'Aktivite pasife alındı', autoClose: 2500 })
    },
    onError: (error) =>
      notifications.show({
        color: 'red',
        title: 'Pasife alınamadı',
        message: error instanceof ApiError ? error.message : 'Beklenmeyen bir hata oluştu.',
      }),
  })

  return (
    <Drawer opened={activityId !== null} onClose={onClose} position="right" size="md" title="Aktivite detayı">
      {query.isPending && <Loader size="sm" />}
      {query.isError && (
        <Alert color="red" variant="light" icon={<IconAlertCircle size={16} />}>
          {query.error instanceof ApiError ? query.error.message : 'Aktivite yüklenemedi.'}
        </Alert>
      )}
      {a && (
        <Stack gap="md">
          <Stack gap="xs">
            <Title order={3}>{a.title}</Title>
            <Group gap="xs">
              <Badge variant="light" color={categoryColor(a.categoryId)}>
                {categoryName(a.categoryId)}
              </Badge>
              <Badge variant="outline" color="gray">
                {STATUS[a.status] ?? a.status}
              </Badge>
            </Group>
          </Stack>
          <Divider />
          <Row icon={<IconCalendar size={16} />} label="Tarih ve saat">
            {formatRange(a.start, a.end, a.isAllDay)}
          </Row>
          <Row icon={<IconFlag size={16} />} label="Öncelik">
            <Badge size="sm" variant="dot" color={PRIORITY_COLOR[a.priority] ?? 'gray'}>
              {LEVEL[a.priority] ?? a.priority}
            </Badge>
          </Row>
          <Row icon={<IconBolt size={16} />} label="Enerji">
            {LEVEL[a.energyLevel] ?? a.energyLevel}
          </Row>
          <Row icon={<IconMapPin size={16} />} label="Konum">
            {a.location || 'Belirtilmemiş'}
          </Row>
          <Row icon={<IconCoin size={16} />} label="Tahmini bütçe">
            {a.estimatedBudget.toLocaleString('tr-TR')} ₺
          </Row>
          <Row icon={<IconInfoCircle size={16} />} label="Açıklama">
            {a.description ? <span style={{ whiteSpace: 'pre-wrap' }}>{a.description}</span> : 'Açıklama yok'}
          </Row>
          {a.series && (
            <>
              <Row icon={<IconRepeat size={16} />} label="Seri">
                {frequencyLabel(a.series.frequency, a.series.daysOfWeek, dayjs(a.series.startDate).date())} ·{' '}
                {dayjs(a.series.startDate).format('D MMM YYYY')} → {dayjs(a.series.endDate).format('D MMM YYYY')}
              </Row>
              <Button
                size="compact-md"
                variant="subtle"
                color="red"
                style={{ alignSelf: 'flex-start' }}
                onClick={() => setSeriesConfirmOpen(true)}
              >
                Bu ve sonrakileri durdur
              </Button>
            </>
          )}
          <Divider />
          <ReminderSection key={a.id} activity={a} />
          <Divider />
          <Group justify="space-between">
            <Button
              size="compact-md"
              variant="default"
              leftSection={<IconPencil size={14} />}
              onClick={() => {
                openActivityForm({ mode: 'edit', activity: a })
                onClose()
              }}
            >
              Düzenle
            </Button>
            <Button
              size="compact-md"
              variant="subtle"
              color="red"
              leftSection={<IconArchive size={14} />}
              onClick={() => setConfirmOpen(true)}
            >
              Pasife al
            </Button>
          </Group>
        </Stack>
      )}
      <Modal opened={seriesConfirmOpen} onClose={() => setSeriesConfirmOpen(false)} title="Seriyi durdur" size="sm" centered>
        <Text size="sm" c="dimmed" mb="md">
          Bu aktivite ({a ? dayjs(a.start).format('D MMM') : ''}) ve serideki sonraki tüm aktiviteler pasife alınacak.
        </Text>
        <Group justify="flex-end" gap="xs">
          <Button variant="default" size="compact-md" onClick={() => setSeriesConfirmOpen(false)}>
            Vazgeç
          </Button>
          <Button color="red" size="compact-md" loading={stopSeries.isPending} onClick={() => stopSeries.mutate()}>
            Durdur
          </Button>
        </Group>
      </Modal>
      <Modal opened={confirmOpen} onClose={() => setConfirmOpen(false)} title="Pasife al" size="sm" centered>
        <Text size="sm" c="dimmed" mb="md">
          Bu aktivite pasife alınacak ve pano/takvimden kalkacak.
        </Text>
        <Group justify="flex-end" gap="xs">
          <Button variant="default" size="compact-md" onClick={() => setConfirmOpen(false)}>
            Vazgeç
          </Button>
          <Button color="red" size="compact-md" loading={deactivate.isPending} onClick={() => deactivate.mutate()}>
            Pasife al
          </Button>
        </Group>
      </Modal>
    </Drawer>
  )
}
