import {
  Alert,
  Avatar,
  Badge,
  Button,
  Container,
  Group,
  Paper,
  PasswordInput,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useForm } from '@mantine/form'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconAt, IconLock, IconUser, IconWorld } from '@tabler/icons-react'
import dayjs from 'dayjs'
import { useMemo, useState } from 'react'
import { changePassword, updateProfile } from '../api/auth'
import { useAuth } from '../auth/context'
import { PageBody, PanelHeader } from '../components/PanelHeader'
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
    initialValues: { displayName: user?.displayName ?? '', timeZoneId: user?.timeZoneId ?? 'Europe/Istanbul' },
    validate: {
      displayName: (v) => (v.trim().length === 0 ? 'Görünen ad zorunludur.' : null),
      timeZoneId: (v) => (v ? null : 'Saat dilimi seçin.'),
    },
  })

  const dirty =
    form.values.displayName.trim() !== user?.displayName || form.values.timeZoneId !== user?.timeZoneId

  const submit = form.onSubmit(async (values) => {
    setSubmitting(true)
    setErrors([])
    try {
      const me = await updateProfile(values.displayName.trim(), values.timeZoneId)
      updateUser(me)
      form.setValues({ displayName: me.displayName, timeZoneId: me.timeZoneId })
      notifications.show({ color: 'teal', title: 'Kaydedildi', message: 'Profil bilgilerin güncellendi.' })
    } catch (e) {
      const { fields, general } = splitErrors(e, { displayName: ['görünen ad'], timeZoneId: ['saat dilimi'] })
      form.setErrors(fields)
      setErrors(general)
    } finally {
      setSubmitting(false)
    }
  })

  return (
    <Paper withBorder radius="md" p="lg">
      <form onSubmit={submit} noValidate>
        <Stack>
          <Title order={4}>Profil bilgileri</Title>
          {errors.length > 0 && (
            <Alert color="red" icon={<IconAlertCircle size={16} />}>
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
            leftSection={<IconAt size={16} />}
          />
          <TextInput label="Görünen ad" leftSection={<IconUser size={16} />} {...form.getInputProps('displayName')} />
          <Select
            label="Saat dilimi"
            data={zones}
            searchable
            allowDeselect={false}
            leftSection={<IconWorld size={16} />}
            {...form.getInputProps('timeZoneId')}
          />
          <Group justify="flex-end">
            <Button type="submit" loading={submitting} disabled={!dirty}>
              Kaydet
            </Button>
          </Group>
        </Stack>
      </form>
    </Paper>
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
    <Paper withBorder radius="md" p="lg">
      <form onSubmit={submit} noValidate>
        <Stack>
          <Title order={4}>Şifre değiştir</Title>
          {errors.length > 0 && (
            <Alert color="red" icon={<IconAlertCircle size={16} />}>
              {errors.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <PasswordInput
            label="Mevcut şifre"
            leftSection={<IconLock size={16} />}
            autoComplete="current-password"
            {...form.getInputProps('currentPassword')}
          />
          <PasswordInput
            label="Yeni şifre"
            description="En az 10 karakter, uzun bir cümle kullanabilirsin"
            leftSection={<IconLock size={16} />}
            autoComplete="new-password"
            {...form.getInputProps('newPassword')}
          />
          <PasswordInput
            label="Yeni şifre (tekrar)"
            description="Şifren değişince tüm cihazlardaki oturumların kapanır."
            leftSection={<IconLock size={16} />}
            autoComplete="new-password"
            {...form.getInputProps('confirm')}
          />
          <Group justify="flex-end">
            <Button type="submit" loading={submitting}>
              Şifreyi değiştir
            </Button>
          </Group>
        </Stack>
      </form>
    </Paper>
  )
}

export function ProfilePage() {
  const { user } = useAuth()
  if (!user) return null

  return (
    <>
      <PanelHeader title="Profilim" />
      <PageBody>
      <Container size="lg" px={0}>
      <Stack gap="md">
        <Paper withBorder radius="md" p="lg">
          <Group gap="lg" wrap="nowrap">
            <Avatar size={64} radius="xl" color="gray" variant="light">
              {initials(user.displayName)}
            </Avatar>
            <Stack gap={4}>
              <Group gap="sm">
                <Title order={3}>{user.displayName}</Title>
                <Badge variant="light" color="gray">
                  {user.role === 'Admin' ? 'Yönetici' : 'Kullanıcı'}
                </Badge>
              </Group>
              <Text c="dimmed">{user.email}</Text>
              <Text c="dimmed" size="sm">
                Üyelik: {dayjs(user.createdAt).format('D MMMM YYYY')}
              </Text>
            </Stack>
          </Group>
        </Paper>
        {/* key: kayıttan dönen yeni me ile form başlangıç değerleri yenilenir. */}
        <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md" style={{ alignItems: 'start' }}>
          <ProfileInfoCard key={`${user.displayName}|${user.timeZoneId}`} />
          <PasswordCard />
        </SimpleGrid>
      </Stack>
      </Container>
      </PageBody>
    </>
  )
}
