import {
  Alert,
  Button,
  Collapse,
  Group,
  Loader,
  Modal,
  NumberInput,
  Rating,
  SegmentedControl,
  Stack,
  Switch,
  Text,
  Textarea,
  TextInput,
  UnstyledButton,
} from '@mantine/core'
import { DatePickerInput, TimePicker } from '@mantine/dates'
import { useForm } from '@mantine/form'
import { useMediaQuery } from '@mantine/hooks'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconBolt, IconCheck, IconChevronDown, IconCircleDashed, IconRepeat, IconX } from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useMemo, useRef, useState, type CSSProperties } from 'react'
import { createActivity, createSeries, fetchCategories, updateActivity } from '../../api/activities'
import type { ActivityStatus, ActivityUpdateRequest, Level } from '../../api/types'
import { LEVEL_OPTIONS, STATUS_OPTIONS } from '../../lib/activityOptions'
import { categoryLook } from '../../lib/categoryColor'
import { Spiral } from '../illustrations'
import { MOD_KEY } from '../../lib/commands'
import {
  countOccurrences,
  defaultUntil,
  FREQUENCY_OPTIONS,
  frequencyLabel,
  isoDay,
  MAX_OCCURRENCES,
  SHORT_DAYS,
  type Frequency,
} from '../../lib/recurrence'
import { splitErrors } from '../../lib/formErrors'
import { PriorityBars } from '../board/PriorityBars'
import type { FormRequest } from './context'
import { PillMenu } from './PillMenu'
import './form.css'

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
  // Tekrarla (yalnızca yeni aktivitede)
  repeat: boolean
  frequency: Frequency
  daysOfWeek: number[]
  until: string
  reminderMinutes: string
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
    repeat: false,
    frequency: 'Daily',
    daysOfWeek: [],
    until: '',
    reminderMinutes: '0',
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
  const p = request.prefill
  const fromPrefill: Partial<Values> = p
    ? {
        title: p.title ?? '',
        description: p.description ?? '',
        categoryId: p.categoryId ?? null,
        ...(p.status ? { status: p.status as ActivityStatus } : {}),
        ...(p.priority ? { priority: p.priority } : {}),
        ...(p.energyLevel ? { energyLevel: p.energyLevel as Level } : {}),
        location: p.location ?? '',
        estimatedBudget: p.estimatedBudget ?? 0,
        isFlexible: p.isFlexible ?? false,
        reminderMinutes: p.reminderMinutesBefore ? String(p.reminderMinutesBefore) : '0',
      }
    : {}
  const rec = p?.recurrence
  return {
    ...base,
    ...fromPrefill,
    isAllDay: request.isAllDay ?? false,
    startDate: start.format(DATE),
    startTime: request.isAllDay ? '09:00' : start.format(TIME),
    endDate: end.format(DATE),
    endTime: request.isAllDay ? '10:00' : end.format(TIME),
    repeat: request.repeat ?? Boolean(rec),
    frequency: (rec?.frequency as Frequency | undefined) ?? 'Daily',
    daysOfWeek: rec?.daysOfWeek.length ? rec.daysOfWeek : [isoDay(start)],
    until: rec?.until ? dayjs(rec.until).format(DATE) : defaultUntil(start, 'Daily'),
  }
}

const ERROR_PREFIXES = {
  title: ['başlık'],
  categoryId: ['kategori'],
  start: ['başlangıç'],
  end: ['bitiş zamanı'],
  estimatedBudget: ['tahmini bütçe'],
  rating: ['puan'],
  recurrence: ['haftalık tekrar', 'bitiş tarihi', 'bir seri', 'bu kuralla', 'gün değerleri'],
  reminder: ['hatırlatma süresi'],
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
    Boolean(initial.location || Number(initial.estimatedBudget) > 0 || initial.isFlexible),
  )
  const [confirmClose, setConfirmClose] = useState(false)
  const [generalErrors, setGeneralErrors] = useState<string[]>([])

  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: fetchCategories, staleTime: 5 * 60_000 })
  const categories = (categoriesQuery.data ?? []).filter((c) => c.isActive)
  const isMobile = useMediaQuery('(max-width: 768px)') ?? false
  const v = form.values

  // Kategori seçilmediyse ilk aktif kategori; düzenlemede mevcut kategori pasif olsa bile listede kalır.
  const categoryId = v.categoryId ?? categories[0]?.id ?? null
  const selectedCategory = categories.find((c) => c.id === categoryId)

  const mutation = useMutation({
    mutationFn: async (body: ActivityUpdateRequest): Promise<{ message: string; id?: number }> => {
      if (editing) {
        const saved = await updateActivity(editing.id, body)
        return { message: 'Kaydedildi', id: saved.id }
      }
      if (form.values.repeat) {
        const created = await createSeries({
          ...body,
          recurrence: {
            frequency: form.values.frequency,
            daysOfWeek: form.values.frequency === 'Weekly' ? form.values.daysOfWeek : [],
            until: form.values.until,
          },
          reminderMinutesBefore: Number(form.values.reminderMinutes) || null,
        })
        return { message: `${created.createdCount} aktivite oluşturuldu` }
      }
      const saved = await createActivity(body)
      return { message: 'Aktivite oluşturuldu', id: saved.id }
    },
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      if (result.id !== undefined) void queryClient.invalidateQueries({ queryKey: ['activity', result.id] })
      notifications.show({ color: 'teal', message: result.message, autoClose: 2500 })
      if (request.mode === 'create') request.onSaved?.()
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

  const untilTouched = useRef(false)
  const startMoment = parse(v.startDate, v.startTime)
  const preview = useMemo(
    () => (v.repeat ? countOccurrences(v.startDate, v.frequency, v.daysOfWeek, v.until) : null),
    [v.repeat, v.startDate, v.frequency, v.daysOfWeek, v.until],
  )
  const previewBlocked = preview !== null && (preview.count === 0 || preview.tooMany || preview.invalidRange)

  // Tekrarla kapalıyken saatli aktivite 24 saati aşıyorsa ipucu (yanlışlıkla çok uzun aktivite).
  const longMs = !editing && !v.repeat && !v.isAllDay ? parse(v.endDate, v.endTime).diff(startMoment) : 0
  const longDays = Math.floor(longMs / 86_400_000)

  const humanDuration = longDays >= 7 ? `${Math.floor(longDays / 7)} hafta` : `${longDays} gün`

  const switchToRepeat = () => {
    // Bitiş tarihi = mevcut bitiş alanındaki tarih; ilk aktivitenin bitişi başlangıç gününe çekilir (saat korunur).
    const until = v.endDate
    untilTouched.current = true
    form.setValues({
      repeat: true,
      frequency: 'Weekly',
      daysOfWeek: [isoDay(startMoment)],
      until,
      endDate: v.startDate,
    })
  }

  const toggleRepeat = (on: boolean) => {
    if (!on) {
      form.setFieldValue('repeat', false)
      return
    }
    // Tekrarla açılınca ilk aktivite aynı gün kalır.
    const end = parse(v.startDate, v.endTime)
    form.setValues({
      repeat: true,
      endDate: v.startDate,
      endTime: end.isAfter(startMoment) ? v.endTime : startMoment.add(1, 'hour').format(TIME),
      daysOfWeek: v.daysOfWeek.length > 0 ? v.daysOfWeek : [isoDay(startMoment)],
      until: untilTouched.current ? v.until : defaultUntil(startMoment, v.frequency),
    })
  }

  const changeFrequency = (frequency: Frequency) => {
    form.setValues({
      frequency,
      until: untilTouched.current ? v.until : defaultUntil(startMoment, frequency),
    })
  }

  const toggleDay = (day: number) => {
    const days = v.daysOfWeek.includes(day) ? v.daysOfWeek.filter((d) => d !== day) : [...v.daysOfWeek, day]
    form.setFieldValue('daysOfWeek', days)
  }

  const submit = () => {
    setGeneralErrors([])
    if (previewBlocked) return
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
  // ADR 0024 §2b: başlangıç en fazla 30 gün öncesi olabilir (mevcut eski kayıt kendi tarihini korur).
  const minStartDate = dayjs().subtract(30, 'day').format(DATE)

  return (
    <>
      <Modal
        opened
        onClose={requestClose}
        size={660}
        fullScreen={isMobile}
        withCloseButton={false}
        padding={0}
        radius={isMobile ? 0 : 4}
        overlayProps={{ backgroundOpacity: 0.25, color: '#2b2a27' }}
        transitionProps={{ transition: 'pop', duration: 180 }}
        styles={{
          content: {
            background: 'var(--paper)',
            border: isMobile ? undefined : 'var(--pen)',
            borderRadius: isMobile ? 0 : 'var(--radius-sketch)',
            boxShadow: 'var(--sketch-shadow-lg)',
            // Yatayda köşe yuvarlaklığı korunur, dikeyde kayar: kısa ekranda / yakınlaştırmada alttaki alanlara ulaşılır.
            overflowX: 'hidden',
            overflowY: 'auto',
          },
          body: { padding: 0 },
        }}
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
          {/* Defter dokusu: üst şeritte spiral tel (modal kaydırıldığı için tel kartın içinde kalır). */}
          <div aria-hidden style={{ position: 'relative', height: 30, borderBottom: '1.2px dashed var(--dash)', background: 'var(--paper-strip)' }}>
            <Spiral inset={24} style={{ top: 2 }} />
          </div>

          <Stack gap={18} px={isMobile ? 18 : 32} pt={10} pb={20}>
            <Group gap={10} wrap="nowrap">
              <span className="hand muted" style={{ fontSize: 20 }}>
                {editing ? 'sayfayı düzenliyoruz' : v.repeat ? 'tekrarlayan bir plan' : 'yeni bir sayfa açıyoruz'}
              </span>
              <div style={{ flex: 1 }} />
              <button type="button" className="icon-btn" aria-label="Kapat" onClick={requestClose} style={{ width: 44, height: 44, border: 0 }}>
                <IconX size={18} stroke={1.8} aria-hidden />
              </button>
            </Group>

            <TextInput
              variant="unstyled"
              placeholder="Aktivite başlığı"
              data-autofocus
              aria-label="Başlık"
              className="title-input"
              styles={{
                input: {
                  fontFamily: 'var(--font-hand)',
                  fontSize: isMobile ? 26 : 30,
                  fontWeight: 700,
                  height: 54,
                  borderRadius: 0,
                  color: 'var(--graphite)',
                  background: 'transparent',
                },
              }}
              error={form.errors.title}
              {...form.getInputProps('title')}
            />

            {/* Özellik hapları */}
            <Group gap={8}>
              {!editing && (
                <button
                  type="button"
                  className="form-pill"
                  aria-pressed={v.repeat}
                  onClick={() => toggleRepeat(!v.repeat)}
                >
                  <IconRepeat size={16} stroke={1.7} aria-hidden />
                  Tekrarla
                </button>
              )}
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
                icon={<IconBolt size={16} stroke={1.7} />}
                onChange={(l) => form.setFieldValue('energyLevel', l)}
              />
              <PillMenu
                ariaLabel="Durum"
                value={v.status}
                options={STATUS_OPTIONS}
                placeholder="Durum"
                icon={<IconCircleDashed size={16} stroke={1.7} />}
                onChange={(s) => form.setFieldValue('status', s)}
              />
            </Group>

            {/* Kategori: ikonlu kutular */}
            <Stack gap={10}>
              <Text fz={13} fw={600} id="form-category-label">
                Kategori
              </Text>
              {categoriesQuery.isPending && (
                <Group gap={6}>
                  <Loader size={12} color="var(--graphite)" />
                  <Text size="xs">Kategoriler yükleniyor…</Text>
                </Group>
              )}
              {categoriesQuery.isError && (
                <Group gap={6}>
                  <Text size="xs" c="var(--danger)">
                    Kategoriler yüklenemedi.
                  </Text>
                  <Button size="compact-sm" variant="sketch" onClick={() => void categoriesQuery.refetch()}>
                    Tekrar dene
                  </Button>
                </Group>
              )}
              {categories.length > 0 && (
                <div className="cat-grid" role="radiogroup" aria-labelledby="form-category-label">
                  {categories.map((c) => {
                    const look = categoryLook(c.id, c.name)
                    const Icon = look.icon
                    const on = c.id === categoryId
                    return (
                      <button
                        key={c.id}
                        type="button"
                        role="radio"
                        aria-checked={on}
                        className="cat-box"
                        title={c.name}
                        onClick={() => form.setFieldValue('categoryId', c.id)}
                        style={{ '--cat': look.line } as CSSProperties}
                      >
                        <Icon size={20} stroke={1.7} aria-hidden />
                        <span className="nav-label" style={{ maxWidth: '100%' }}>
                          {c.name}
                        </span>
                      </button>
                    )
                  })}
                </div>
              )}
              {form.errors.categoryId && (
                <Text size="xs" c="var(--danger)">
                  {form.errors.categoryId}
                </Text>
              )}
              {selectedCategory === undefined && categoriesQuery.isSuccess && categories.length === 0 && (
                <Text size="xs" c="var(--danger)">
                  Aktif kategori bulunamadı.
                </Text>
              )}
            </Stack>

            {/* Zaman satırı */}
            <Stack gap={6} mt={4}>
              <Group gap="xs" wrap="wrap">
                <Text fz={13} fw={600} w={84}>
                  {v.repeat ? 'İlk aktivite' : 'Başlangıç'}
                </Text>
                <DatePickerInput
                  size="md"
                  w={170}
                  valueFormat="D MMM YYYY"
                  aria-label="Başlangıç tarihi"
                  value={v.startDate || null}
                  minDate={v.startDate && v.startDate < minStartDate ? v.startDate : minStartDate}
                  onChange={(d) => d && setStart(d, v.startTime)}
                />
                {!v.isAllDay && (
                  <TimePicker
                    size="md"
                    w={104}
                    format="24h"
                    aria-label="Başlangıç saati"
                    value={v.startTime}
                    onChange={(t) => t && setStart(v.startDate, t)}
                  />
                )}
                <Switch
                  ml="auto"
                  size="md"
                  label="Tüm gün"
                  checked={v.isAllDay}
                  onChange={(e) => form.setFieldValue('isAllDay', e.currentTarget.checked)}
                />
              </Group>
              {form.errors.start && (
                <Text size="xs" c="var(--danger)">
                  {form.errors.start}
                </Text>
              )}
              {!(v.repeat && v.isAllDay) && (
              <Group gap="xs" wrap="wrap">
                <Text fz={13} fw={600} w={84}>
                  Bitiş
                </Text>
                {!v.repeat && (
                <DatePickerInput
                  size="md"
                  w={170}
                  valueFormat="D MMM YYYY"
                  aria-label="Bitiş tarihi"
                  value={v.endDate || null}
                  minDate={v.startDate || undefined}
                  onChange={(d) => d && form.setFieldValue('endDate', d)}
                />
                )}
                {!v.isAllDay && (
                  <TimePicker
                    size="md"
                    w={104}
                    format="24h"
                    aria-label="Bitiş saati"
                    value={v.endTime}
                    onChange={(t) => t && form.setFieldValue('endTime', t)}
                  />
                )}
              </Group>
              )}
              {endError && (
                <Text size="xs" c="var(--danger)">
                  {endError}
                </Text>
              )}

              {longDays >= 1 && (
                <Alert color="orange" py={6} icon={<IconRepeat size={16} />}>
                  <Group justify="space-between" wrap="nowrap" gap="xs">
                    <Text size="xs">
                      Bu aktivite {humanDuration} sürüyor. Her hafta tekrarlamak mı istedin?
                    </Text>
                    <Button size="compact-sm" variant="sketch" onClick={switchToRepeat} style={{ flexShrink: 0 }}>
                      Tekrarla'ya geç
                    </Button>
                  </Group>
                </Alert>
              )}

              {!editing && v.repeat && (
                <Stack
                  gap="xs"
                  p="sm"
                  style={{ border: '1.4px dashed var(--faint)', borderRadius: 4, background: 'transparent' }}
                >
                  <SegmentedControl
                    size="md"
                    fullWidth
                    value={v.frequency}
                    data={FREQUENCY_OPTIONS}
                    onChange={(f) => changeFrequency(f as Frequency)}
                  />
                  {v.frequency === 'Weekly' && (
                    <Group gap={4}>
                      {SHORT_DAYS.map((name, i) => (
                        <Button
                          key={name}
                          size="compact-sm"
                          radius="xl"
                          variant={v.daysOfWeek.includes(i + 1) ? 'filled' : 'default'}
                          onClick={() => toggleDay(i + 1)}
                        >
                          {name}
                        </Button>
                      ))}
                    </Group>
                  )}
                  <Group gap="xs" wrap="nowrap" align="center">
                    <Text fz={13} fw={600} w={84}>
                      Bitiş tarihi
                    </Text>
                    <DatePickerInput
                      size="md"
                      w={170}
                      valueFormat="D MMM YYYY"
                      aria-label="Tekrar bitiş tarihi"
                      value={v.until || null}
                      minDate={v.startDate || undefined}
                      onChange={(d) => {
                        if (!d) return
                        untilTouched.current = true
                        form.setFieldValue('until', d)
                      }}
                    />
                  </Group>
                  <Group gap="xs" wrap="nowrap" align="center">
                    <Text fz={13} fw={600} w={84}>
                      Hatırlatma
                    </Text>
                    <SegmentedControl
                      size="md"
                      value={v.reminderMinutes}
                      data={[
                        { value: '0', label: 'Yok' },
                        { value: '15', label: '15 dk önce' },
                        { value: '60', label: '1 saat önce' },
                        { value: '1440', label: '1 gün önce' },
                      ]}
                      onChange={(m) => form.setFieldValue('reminderMinutes', m)}
                    />
                  </Group>
                  {preview && (
                    <Text size="xs" c={previewBlocked ? 'var(--danger)' : undefined}>
                      {preview.invalidRange
                        ? 'Bitiş tarihi başlangıç gününden önce olamaz.'
                        : preview.tooMany
                          ? `Bir seri en fazla ${MAX_OCCURRENCES} aktivite içerebilir; bitiş tarihini yaklaştırın.`
                          : preview.count === 0
                            ? 'Bu kuralla hiç aktivite oluşmuyor.'
                            : `${frequencyLabel(v.frequency, v.daysOfWeek, startMoment.date())} · ${startMoment.format('D MMM')} → ${dayjs(v.until).format('D MMM')} · ${preview.count} aktivite`}
                    </Text>
                  )}
                  {(form.errors.recurrence || form.errors.reminder) && (
                    <Text size="xs" c="var(--danger)">
                      {form.errors.recurrence} {form.errors.reminder}
                    </Text>
                  )}
                </Stack>
              )}
            </Stack>

            {/* Not: çizgili defter satırı, el yazısı */}
            <Stack gap={8}>
              <Text component="label" htmlFor="form-note" fz={13} fw={700}>
                Not
              </Text>
              <Textarea
                id="form-note"
                placeholder="Aklına gelenleri yaz…"
                autosize
                minRows={3}
                maxRows={8}
                classNames={{ input: 'ruled-note' }}
                {...form.getInputProps('description')}
              />
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
                  <TextInput size="md" label="Konum" placeholder="Nerede?" {...form.getInputProps('location')} />
                  <NumberInput
                    size="md"
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
                  <Text size="xs" c="var(--danger)">
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
              <Alert color="orange" icon={<IconAlertCircle size={16} />}>
                {generalErrors.map((m) => (
                  <div key={m}>{m}</div>
                ))}
              </Alert>
            )}
          </Stack>

          <Group gap={10} px={isMobile ? 18 : 32} pb={isMobile ? 24 : 28} pt={4} wrap="wrap">
            <Group gap={8} visibleFrom="sm">
              <span className="mono" style={{ fontSize: 12, padding: '3px 7px', borderRadius: 4, border: '1px solid var(--faint)' }}>
                {MOD_KEY} + Enter
              </span>
              <Text fz={13}>ile kaydet</Text>
            </Group>
            <div style={{ flex: 1 }} />
            <Button variant="sketch" h={44} px={20} fz={14} onClick={requestClose}>
              Vazgeç
            </Button>
            <Button
              variant="ink"
              h={44}
              px={26}
              fz={14}
              loading={mutation.isPending}
              disabled={previewBlocked}
              onClick={submit}
              leftSection={<IconCheck size={18} stroke={2.4} />}
            >
              {editing ? 'Kaydet' : preview && !previewBlocked ? `Deftere yaz · ${preview.count} aktivite` : 'Deftere yaz'}
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
