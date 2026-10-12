import { Alert, Badge, Divider, Drawer, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { IconAlertCircle, IconBolt, IconCalendar, IconCoin, IconFlag, IconInfoCircle, IconMapPin } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import type { ReactNode } from 'react'
import { fetchActivity } from '../api/activities'
import { ApiError } from '../api/client'
import { categoryColor } from '../lib/categoryColor'

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
          <Divider />
          <Text size="xs" c="dimmed">
            Düzenleme yakında (F3)
          </Text>
        </Stack>
      )}
    </Drawer>
  )
}
