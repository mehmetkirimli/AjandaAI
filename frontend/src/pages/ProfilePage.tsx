import { Alert, Button, PasswordInput, Select, SimpleGrid, Stack, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconLock, IconUser } from '@tabler/icons-react'
import dayjs from 'dayjs'
import { useMemo, useState } from 'react'
import { changePassword, updateProfile } from '../api/auth'
import { useAuth } from '../auth/context'
import { CardTitle } from '../components/CardTitle'
import { NoteAndPencil, Scribble, Spiral } from '../components/illustrations'
import { ClaudeConnectionCard } from './ClaudeConnectionCard'
import './profile.css'
import { splitErrors } from '../lib/formErrors'
import { timeZoneOptions } from '../lib/timeZones'

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  const letters = parts.length > 1 ? parts[0][0] + parts[parts.length - 1][0] : (parts[0] ?? '?').slice(0, 2)
  return letters.toLocaleUpperCase('tr')
}

function ProfileInfoCard() {
  const { user, updateUser } = useAuth()
  const [submitting, setSubmitting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])
  const zones = useMemo(() => timeZoneOptions(user?.timeZoneId ?? 'Europe/Istanbul'), [user?.timeZoneId])

  const form = useForm({
    initialValues: {
      displayName: user?.displayName ?? '', timeZoneId: user?.timeZoneId ?? 'Europe/Istanbul',
      location: user?.location ?? '',
    },
    validate: {
      displayName: (v) => (v.trim().length === 0 ? 'Görünen ad zorunludur.' : null),
      timeZoneId: (v) => (v ? null : 'Saat dilimi seçin.'),
      location: (v) => (v.trim().length > 100 ? 'Konum en fazla 100 karakter olabilir.' : null),
    },
  })

  const dirty =
    form.values.displayName.trim() !== user?.displayName || form.values.timeZoneId !== user?.timeZoneId ||
    form.values.location.trim() !== (user?.location ?? '')

  const submit = form.onSubmit(async (values) => {
    setSubmitting(true)
    setErrors([])
    try {
      const me = await updateProfile(values.displayName.trim(), values.timeZoneId, values.location.trim() || null)
      updateUser(me)
      form.setValues({ displayName: me.displayName, timeZoneId: me.timeZoneId, location: me.location ?? '' })
      notifications.show({ color: 'teal', title: 'Kaydedildi', message: 'Profil bilgilerin güncellendi.' })
    } catch (e) {
      const { fields, general } = splitErrors(e, { displayName: ['görünen ad'], timeZoneId: ['saat dilimi'], location: ['konum'] })
      form.setErrors(fields)
      setErrors(general)
    } finally {
      setSubmitting(false)
    }
  })

  return (
    <section className="paper" style={{ padding: 22, height: '100%' }}>
      <form onSubmit={submit} noValidate>
        <Stack>
          <CardTitle icon={<IconUser size={18} stroke={1.7} />}>Kişisel bilgiler</CardTitle>
          {errors.length > 0 && (
            <Alert color="orange" icon={<IconAlertCircle size={16} />}>
              {errors.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <TextInput
            label="E-posta"
            value={user?.email ?? ''}
            readOnly
            disabled
            description="E-posta değişikliği yakında"
          />
          <TextInput label="Ad soyad" {...form.getInputProps('displayName')} />
          <Select
            label="Saat dilimi"
            data={zones}
            searchable
            allowDeselect={false}
            {...form.getInputProps('timeZoneId')}
          />
          <TextInput
            label="Konumum"
            placeholder="Üsküdar, İstanbul"
            maxLength={100}
            description="Şehir ve ilçe yeterli (örn. Üsküdar, İstanbul). Asistan hava ve yer sorularında kullanır; tam adres yazma."
            {...form.getInputProps('location')}
          />
          <Button type="submit" loading={submitting} disabled={!dirty} fullWidth h={44} fz={14} variant="ink">
            Kaydet
          </Button>
        </Stack>
      </form>
    </section>
  )
}

function PasswordCard() {
  const { endLocalSession } = useAuth()
  const [submitting, setSubmitting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])

  const form = useForm({
    initialValues: { currentPassword: '', newPassword: '', confirm: '' },
    validate: {
      currentPassword: (v) => (v.length === 0 ? 'Mevcut şifre zorunludur.' : null),
      newPassword: (v) => (v.length < 10 ? 'Şifre en az 10 karakter olmalı.' : null),
      confirm: (v, values) => (v !== values.newPassword ? 'Şifreler eşleşmiyor' : null),
    },
  })

  const submit = form.onSubmit(async (values) => {
    setSubmitting(true)
    setErrors([])
    try {
      await changePassword(values.currentPassword, values.newPassword)
      // Sunucu tüm oturumları kapattı; yerel oturum temizlenir, ProtectedRoute /login'e yönlendirir.
      endLocalSession('Şifren değiştirildi. Yeni şifrenle tekrar giriş yap.')
    } catch (e) {
      const { fields, general } = splitErrors(e, {
        currentPassword: ['mevcut şifre'],
        newPassword: ['şifre', 'yeni şifre'],
      })
      form.setErrors(fields)
      setErrors(general)
      setSubmitting(false)
    }
  })

  return (
    <section className="paper" style={{ padding: 22, height: '100%' }}>
      <form onSubmit={submit} noValidate>
        <Stack>
          <CardTitle icon={<IconLock size={18} stroke={1.7} />}>Şifre değiştir</CardTitle>
          {errors.length > 0 && (
            <Alert color="orange" icon={<IconAlertCircle size={16} />}>
              {errors.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <PasswordInput
            label="Mevcut şifre"
            autoComplete="current-password"
            {...form.getInputProps('currentPassword')}
          />
          <PasswordInput
            label="Yeni şifre"
            description="En az 10 karakter, uzun bir cümle kullanabilirsin"
            autoComplete="new-password"
            {...form.getInputProps('newPassword')}
          />
          <PasswordInput
            label="Yeni şifre (tekrar)"
            autoComplete="new-password"
            {...form.getInputProps('confirm')}
          />
          <Text fz={13} style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
            <IconAlertCircle size={16} color="var(--graphite)" style={{ flex: 'none' }} aria-hidden />
            Şifren değişince tüm cihazlarda oturumun kapanır.
          </Text>
          <Button type="submit" loading={submitting} fullWidth h={44} fz={14} variant="ink">
            Şifreyi güncelle
          </Button>
        </Stack>
      </form>
    </section>
  )
}

export function ProfilePage() {
  const { user } = useAuth()
  if (!user) return null

  return (
    <div className="page-body" style={{ display: 'flex', flexDirection: 'column', gap: 24, paddingTop: 34 }}>
      {/* Defter kapağı: kâğıt kapak, üstte spiral tel, avatar ve ad. */}
      <section className="profile-cover paper">
        <Spiral />
        <div className="profile-avatar" aria-hidden>
          {initials(user.displayName)}
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
          <span className="hand muted" style={{ fontSize: 20 }}>
            bu defter şuna ait:
          </span>
          <h1 style={{ margin: 0, fontFamily: 'var(--font-hand)', fontSize: 38, fontWeight: 700, lineHeight: 1.05, overflowWrap: 'anywhere' }}>
            {user.displayName}
          </h1>
          <Scribble width={220} />
          <span style={{ fontSize: 13, color: 'var(--graphite-2)', overflowWrap: 'anywhere', marginTop: 4 }}>
            {user.email} · {user.role === 'Admin' ? 'Admin' : 'Kullanıcı'} · Üyelik: {dayjs(user.createdAt).format('D MMMM YYYY')}
          </span>
        </div>
        <div style={{ flex: 1 }} />
        <NoteAndPencil width={130} style={{ alignSelf: 'center' }} />
      </section>
      {/* key: kayıttan dönen yeni me ile form başlangıç değerleri yenilenir. */}
      <SimpleGrid cols={{ base: 1, md: 2 }} spacing={18}>
        <ProfileInfoCard key={`${user.displayName}|${user.timeZoneId}|${user.location ?? ''}`} />
        <PasswordCard />
      </SimpleGrid>
      <ClaudeConnectionCard />
    </div>
  )
}