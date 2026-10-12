import {
  Alert,
  Button,
  Collapse,
  Group,
  Kbd,
  Modal,
  NumberInput,
  Rating,
  Stack,
  Switch,
  Text,
  Textarea,
  TextInput,
  UnstyledButton,
} from '@mantine/core'
import { DatePickerInput, TimePicker } from '@mantine/dates'
import { useForm } from '@mantine/form'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconBolt, IconChevronDown, IconCircleDashed, IconTag } from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useMemo, useState } from 'react'
import { createActivity, fetchCategories, updateActivity } from '../../api/activities'
import type { ActivityStatus, ActivityUpdateRequest, Level } from '../../api/types'
import { LEVEL_OPTIONS, STATUS_OPTIONS } from '../../lib/activityOptions'
import { categoryStrip } from '../../lib/categoryColor'
import { MOD_KEY } from '../../lib/commands'
import { splitErrors } from '../../lib/formErrors'
import { PriorityBars } from '../board/PriorityBars'
import type { FormRequest } from './context'
import { PillMenu } from './PillMenu'

interface Values {
  title: string
  description: string
  categoryId: number | null
  status: ActivityStatus
  priority: Level
  energyLevel: Level
  isAllDay: boolean
  startDate: string
  startTime: string
  endDate: string
  endTime: string
  location: string
  estimatedBudget: number | string
  isFlexible: boolean
  rating: number
  wouldRepeat: boolean
}

const DATE = 'YYYY-MM-DD'
const TIME = 'HH:mm'
const parse = (date: string, time: string) => dayjs(`${date} ${time}`, `${DATE} ${TIME}`)

function initialValues(request: FormRequest): Values {
  const base: Values = {
    title: '',
    description: '',
    categoryId: null,
    status: 'Planned',
    priority: 'Medium',
    energyLevel: 'Medium',
    isAllDay: false,
    startDate: '',
    startTime: '',
    endDate: '',
    endTime: '',
    location: '',
    estimatedBudget: 0,
    isFlexible: false,
    rating: 5,
    wouldRepeat: false,
  }

  if (request.mode === 'edit') {
    const a = request.activity
    const s = dayjs(a.start)
    const e = dayjs(a.end)
    return {
      ...base,
      title: a.title,
      description: a.description ?? '',
      categoryId: a.categoryId,
      status: a.status as ActivityStatus,
      priority: a.priority,
      energyLevel: a.energyLevel as Level,
      isAllDay: a.isAllDay,
      startDate: s.format(DATE),
      startTime: s.format(TIME),
      endDate: e.format(DATE),
      endTime: e.format(TIME),
      location: a.location ?? '',
      estimatedBudget: a.estimatedBudget,
      isFlexible: a.isFlexible,
      rating: a.rating ?? 5,
      wouldRepeat: a.wouldRepeat ?? false,
    }
  }

  // Varsayılan: bir sonraki tam saat, 1 saat süre.
  const start = request.start ? dayjs(request.start) : dayjs().add(1, 'hour').startOf('hour')
  const end = request.end ? dayjs(request.end) : start.add(1, 'hour')
  return {
    ...base,
    isAllDay: request.isAllDay ?? false,
    startDate: start.format(DATE),
    startTime: request.isAllDay ? '09:00' : start.format(TIME),
    endDate: end.format(DATE),
    endTime: request.isAllDay ? '10:00' : end.format(TIME),
  }
}

const ERROR_PREFIXES = {
  title: ['başlık'],
  categoryId: ['kategori'],
  end: ['bitiş'],
  estimatedBudget: ['tahmini bütçe'],
  rating: ['puan'],
}

interface Props {
  request: FormRequest
  onClose: () => void
}

export function ActivityFormModal({ request, onClose }: Props) {
  const queryClient = useQueryClient()
  const editing = request.mode === 'edit' ? request.activity : null
  const initial = useMemo(() => initialValues(request), [request])
  const form = useForm<Values>({ initialValues: initial })
  const [moreOpen, setMoreOpen] = useState(
    Boolean(editing && (editing.location || editing.estimatedBudget > 0 || editing.isFlexible)),
  )
  const [confirmClose, setConfirmClose] = useState(false)
  const [generalErrors, setGeneralErrors] = useState<string[]>([])

  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })
  const categories = (categoriesQuery.data ?? []).filter((c) => c.isActive)
  const v = form.values

  // Kategori seçilmediyse ilk aktif kategori; düzenlemede mevcut kategori pasif olsa bile listede kalır.
  const categoryId = v.categoryId ?? categories[0]?.id ?? null
  const categoryOptions = categories.map((c) => ({
    value: c.id,
    label: c.name,
    icon: (
      <span
        style={{ width: 8, height: 8, borderRadius: '50%', background: categoryStrip(c.id), display: 'inline-block' }}
      />
    ),
  }))
  const selectedCategory = categories.find((c) => c.id === categoryId)

  const mutation = useMutation({
    mutationFn: (body: ActivityUpdateRequest) => (editing ? updateActivity(editing.id, body) : createActivity(body)),
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      void queryClient.invalidateQueries({ queryKey: ['activity', saved.id] })
      notifications.show({ color: 'teal', message: editing ? 'Kaydedildi' : 'Aktivite oluşturuldu', autoClose: 2500 })
      onClose()
    },
    onError: (error) => {
      const { fields, general } = splitErrors(error, ERROR_PREFIXES)
      form.setErrors(fields)
      setGeneralErrors(general)
    },
  })

  // Başlangıç değişince süre korunur (bitiş birlikte kayar).
  const setStart = (date: string, time: string) => {
    const oldStart = parse(v.startDate, v.startTime)
    const oldEnd = parse(v.endDate, v.endTime)
    const duration = oldEnd.diff(oldStart)
    const next = parse(date, time)
    if (!next.isValid()) {
      form.setValues({ startDate: date, startTime: time })
      return
    }
    const end = next.add(duration > 0 ? duration : 3_600_000, 'millisecond')
    form.setValues({
      startDate: date,
      startTime: time,
      endDate: end.format(DATE),
      endTime: end.format(TIME),
    })
  }

  const submit = () => {
    setGeneralErrors([])
    const errors: Partial<Record<keyof Values, string>> = {}
    const title = v.title.trim()
    if (!title) errors.title = 'Başlık zorunludur.'
    else if (title.length > 200) errors.title = 'Başlık en fazla 200 karakter olabilir.'
    if (categoryId === null) errors.categoryId = 'Kategori seçin.'
    const start = v.isAllDay ? parse(v.startDate, '00:00') : parse(v.startDate, v.startTime)
    const end = v.isAllDay ? parse(v.endDate, '23:59') : parse(v.endDate, v.endTime)
    if (!start.isValid() || !end.isValid()) errors.endDate = 'Geçerli bir tarih ve saat girin.'
    else if (!end.isAfter(start)) errors.endDate = 'Bitiş zamanı başlangıçtan sonra olmalıdır.'
    const budget = typeof v.estimatedBudget === 'number' ? v.estimatedBudget : Number(v.estimatedBudget || 0)
    if (budget < 0) errors.estimatedBudget = 'Tahmini bütçe negatif olamaz.'
    form.setErrors(errors)
    if (Object.keys(errors).length > 0 || categoryId === null) return

    const completed = v.status === 'Completed'
    mutation.mutate({
      categoryId,
      title,
      description: v.description.trim(),
      status: v.status,
      priority: v.priority,
      energyLevel: v.energyLevel,
      start: start.toISOString(),
      end: end.toISOString(),
      isAllDay: v.isAllDay,
      location: v.location.trim() || null,
      isFlexible: v.isFlexible,
      estimatedBudget: budget,
      // Puan ve tekrar yalnızca tamamlanan aktivitede gönderilir.
      rating: completed ? v.rating : null,
      wouldRepeat: completed ? v.wouldRepeat : null,
    })
  }

  const requestClose = () => (form.isDirty() ? setConfirmClose(true) : onClose())
  const endError = form.errors.endDate ?? form.errors.end

  return (
    <>
      <Modal
        opened
        onClose={requestClose}
        size={600}
        withCloseButton={false}
        padding={0}
        radius="lg"
        overlayProps={{ backgroundOpacity: 0.55, blur: 2 }}
        styles={{ content: { background: 'var(--app-surface)', border: '1px solid var(--app-line-strong)' } }}
        trapFocus
      >
        <div
          onKeyDown={(e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
              e.preventDefault()
              submit()
            }
          }}
        >
          <Stack gap="sm" p="md" pb="sm">
            <Text size="xs" c="dimmed">
              {editing ? 'Aktiviteyi düzenle' : 'Yeni aktivite'}
            </Text>
            <TextInput
              variant="unstyled"
              placeholder="Aktivite başlığı"
              data-autofocus
              aria-label="Aktivite başlığı"
              styles={{ input: { fontSize: 18, fontWeight: 600, height: 32 } }}
              error={form.errors.title}
              {...form.getInputProps('title')}
            />
            <Textarea
              variant="unstyled"
              placeholder="Açıklama ekle…"
              aria-label="Açıklama"
              autosize
              minRows={2}
              maxRows={8}
              {...form.getInputProps('description')}
            />

            {/* Özellik hapları */}
            <Group gap={6}>
              <PillMenu
                ariaLabel="Kategori"
                value={categoryId}
                options={categoryOptions}
                placeholder="Kategori"
                icon={<IconTag size={13} />}
                onChange={(id) => form.setFieldValue('categoryId', id)}
              />
              <PillMenu
                ariaLabel="Öncelik"
                value={v.priority}
                options={LEVEL_OPTIONS.map((o) => ({ ...o, icon: <PriorityBars priority={o.value} /> }))}
                placeholder="Öncelik"
                onChange={(p) => form.setFieldValue('priority', p)}
              />
              <PillMenu
                ariaLabel="Enerji"
                value={v.energyLevel}
                options={LEVEL_OPTIONS.map((o) => ({ ...o, label: `Enerji: ${o.label}` }))}
                placeholder="Enerji"
                icon={<IconBolt size={13} />}
                onChange={(l) => form.setFieldValue('energyLevel', l)}
              />
              <PillMenu
                ariaLabel="Durum"
                value={v.status}
                options={STATUS_OPTIONS}
                placeholder="Durum"
                icon={<IconCircleDashed size={13} />}
                onChange={(s) => form.setFieldValue('status', s)}
              />
            </Group>
            {form.errors.categoryId && (
              <Text size="xs" c="red">
                {form.errors.categoryId}
              </Text>
            )}
            {selectedCategory === undefined && categoriesQuery.isSuccess && categories.length === 0 && (
              <Text size="xs" c="red">
                Aktif kategori bulunamadı.
              </Text>
            )}

            {/* Zaman satırı */}
            <Stack gap={6} mt={4}>
              <Group gap="xs" wrap="nowrap">
                <Text size="xs" c="dimmed" w={64}>
                  Başlangıç
                </Text>
                <DatePickerInput
                  size="xs"
                  w={150}
                  valueFormat="D MMM YYYY"
                  aria-label="Başlangıç tarihi"
                  value={v.startDate || null}
                  onChange={(d) => d && setStart(d, v.startTime)}
                />
                {!v.isAllDay && (
                  <TimePicker
                    size="xs"
                    w={84}
                    format="24h"
                    aria-label="Başlangıç saati"
                    value={v.startTime}
                    onChange={(t) => t && setStart(v.startDate, t)}
                  />
                )}
                <Switch
                  ml="auto"
                  size="xs"
                  label="Tüm gün"
                  checked={v.isAllDay}
                  onChange={(e) => form.setFieldValue('isAllDay', e.currentTarget.checked)}
                />
              </Group>
              <Group gap="xs" wrap="nowrap">
                <Text size="xs" c="dimmed" w={64}>
                  Bitiş
                </Text>
                <DatePickerInput
                  size="xs"
                  w={150}
                  valueFormat="D MMM YYYY"
                  aria-label="Bitiş tarihi"
                  value={v.endDate || null}
                  minDate={v.startDate || undefined}
                  onChange={(d) => d && form.setFieldValue('endDate', d)}
                />
                {!v.isAllDay && (
                  <TimePicker
                    size="xs"
                    w={84}
                    format="24h"
                    aria-label="Bitiş saati"
                    value={v.endTime}
                    onChange={(t) => t && form.setFieldValue('endTime', t)}
                  />
                )}
              </Group>
              {endError && (
                <Text size="xs" c="red">
                  {endError}
                </Text>
              )}
            </Stack>

            {/* Ek alanlar */}
            <UnstyledButton onClick={() => setMoreOpen((o) => !o)} aria-expanded={moreOpen} mt={2}>
              <Group gap={4} c="dimmed">
                <IconChevronDown
                  size={13}
                  style={{ transform: moreOpen ? undefined : 'rotate(-90deg)', transition: 'transform 120ms' }}
                />
                <Text size="xs">Daha fazla</Text>
              </Group>
            </UnstyledButton>
            <Collapse expanded={moreOpen}>
              <Stack gap="xs" pt={4}>
                <Group grow align="flex-start">
                  <TextInput size="xs" label="Konum" placeholder="Nerede?" {...form.getInputProps('location')} />
                  <NumberInput
                    size="xs"
                    label="Tahmini bütçe"
                    prefix="₺ "
                    min={0}
                    decimalScale={2}
                    thousandSeparator="."
                    decimalSeparator=","
                    {...form.getInputProps('estimatedBudget')}
                  />
                </Group>
                <Switch
                  size="xs"
                  label="Esnek (başka zamana kaydırılabilir)"
                  checked={v.isFlexible}
                  onChange={(e) => form.setFieldValue('isFlexible', e.currentTarget.checked)}
                />
              </Stack>
            </Collapse>
            {v.status === 'Completed' && (
              <Stack gap={6}>
                <Group gap="sm">
                  <Text size="xs" c="dimmed">
                    Puan
                  </Text>
                  <Rating count={10} size="xs" value={v.rating} onChange={(r) => form.setFieldValue('rating', r)} />
                  <Text size="xs">{v.rating}/10</Text>
                </Group>
                {form.errors.rating && (
                  <Text size="xs" c="red">
                    {form.errors.rating}
                  </Text>
                )}
                <Switch
                  size="xs"
                  label="Tekrar eder misin?"
                  checked={v.wouldRepeat}
                  onChange={(e) => form.setFieldValue('wouldRepeat', e.currentTarget.checked)}
                />
              </Stack>
            )}

            {generalErrors.length > 0 && (
              <Alert color="red" icon={<IconAlertCircle size={16} />}>
                {generalErrors.map((m) => (
                  <div key={m}>{m}</div>
                ))}
              </Alert>
            )}
          </Stack>

          <Group
            justify="space-between"
            px="md"
            py="sm"
            style={{ borderTop: '1px solid var(--app-line)' }}
          >
            <Group gap="xs">
              <Button size="compact-md" loading={mutation.isPending} onClick={submit}>
                {editing ? 'Kaydet' : 'Oluştur'}
              </Button>
              <Group gap={3}>
                <Kbd size="xs">{MOD_KEY}</Kbd>
                <Kbd size="xs">Enter</Kbd>
              </Group>
            </Group>
            <Button size="compact-md" variant="subtle" color="gray" onClick={requestClose}>
              Vazgeç
            </Button>
          </Group>
        </div>
      </Modal>

      <Modal
        opened={confirmClose}
        onClose={() => setConfirmClose(false)}
        title="Değişiklikler kaybolsun mu?"
        size="sm"
        centered
        zIndex={400}
      >
        <Text size="sm" c="dimmed" mb="md">
          Kaydedilmemiş değişiklikler var.
        </Text>
        <Group justify="flex-end" gap="xs">
          <Button variant="default" size="compact-md" onClick={() => setConfirmClose(false)}>
            Düzenlemeye devam et
          </Button>
          <Button color="red" size="compact-md" onClick={onClose}>
            Kaybolsun
          </Button>
        </Group>
      </Modal>
    </>
  )
}
