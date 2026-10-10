import { Alert, Anchor, Button, PasswordInput, Select, Stack, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { IconAlertCircle, IconAt, IconLock, IconUser, IconWorld } from '@tabler/icons-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { register } from '../api/auth'
import { AuthLayout } from '../components/AuthLayout'
import { splitErrors } from '../lib/formErrors'

function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Istanbul'
  } catch {
    return 'Europe/Istanbul'
  }
}

function timeZoneOptions(current: string): string[] {
  let zones: string[] = []
  try {
    zones = Intl.supportedValuesOf('timeZone')
  } catch {
    zones = []
  }
  const set = new Set<string>(['Europe/Istanbul', ...zones])
  set.add(current)
  return [...set].sort()
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [submitting, setSubmitting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])
  const zones = useMemo(() => timeZoneOptions(browserTimeZone()), [])

  const form = useForm({
    initialValues: { email: '', displayName: '', timeZoneId: browserTimeZone(), password: '' },
    validate: {
      email: (v) => (/^\S+@\S+\.\S+$/.test(v.trim()) ? null : 'Geçerli bir e-posta adresi girin.'),
      displayName: (v) => (v.trim().length === 0 ? 'Görünen ad zorunludur.' : null),
      timeZoneId: (v) => (v ? null : 'Saat dilimi seçin.'),
      password: (v) => (v.length < 10 ? 'Şifre en az 10 karakter olmalı.' : null),
    },
  })

  const submit = form.onSubmit(async (values) => {
    setSubmitting(true)
    setErrors([])
    try {
      await register({ ...values, email: values.email.trim(), displayName: values.displayName.trim() })
      navigate('/check-email', { state: { email: values.email.trim() } })
    } catch (e) {
      const { fields, general } = splitErrors(e, {
        email: ['email', 'e-posta'],
        displayName: ['görünen ad'],
        timeZoneId: ['saat dilimi'],
        password: ['şifre'],
      })
      form.setErrors(fields)
      setErrors(general)
    } finally {
      setSubmitting(false)
    }
  })

  return (
    <AuthLayout title="Hesap oluştur" subtitle="Birkaç saniyede başla; e-postana bir doğrulama bağlantısı göndereceğiz.">
      <form onSubmit={submit} noValidate>
        <Stack>
          {errors.length > 0 && (
            <Alert color="red" icon={<IconAlertCircle size={18} />} variant="light">
              {errors.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <TextInput
            label="E-posta"
            placeholder="ornek@mail.com"
            leftSection={<IconAt size={16} />}
            autoComplete="email"
            {...form.getInputProps('email')}
          />
          <TextInput
            label="Görünen ad"
            placeholder="Adınız"
            leftSection={<IconUser size={16} />}
            autoComplete="name"
            {...form.getInputProps('displayName')}
          />
          <Select
            label="Saat dilimi"
            data={zones}
            searchable
            allowDeselect={false}
            leftSection={<IconWorld size={16} />}
            {...form.getInputProps('timeZoneId')}
          />
          <PasswordInput
            label="Şifre"
            description="En az 10 karakter. Uzun bir cümle kullanabilirsiniz."
            leftSection={<IconLock size={16} />}
            autoComplete="new-password"
            {...form.getInputProps('password')}
          />
          <Button type="submit" loading={submitting} fullWidth size="md" mt="xs">
            Kayıt ol
          </Button>
          <Text ta="center" size="sm" c="dimmed">
            Zaten hesabın var mı?{' '}
            <Anchor component={Link} to="/login" fw={600}>
              Giriş yap
            </Anchor>
          </Text>
        </Stack>
      </form>
    </AuthLayout>
  )
}
