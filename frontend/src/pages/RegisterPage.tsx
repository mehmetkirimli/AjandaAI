import { Alert, Anchor, Button, PasswordInput, Select, Stack, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { register } from '../api/auth'
import { AuthLayout } from '../components/AuthLayout'
import { splitErrors } from '../lib/formErrors'
import { browserTimeZone, timeZoneOptions } from '../lib/timeZones'

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
    <AuthLayout title="Yeni bir defter aç" subtitle="E-postana bir doğrulama bağlantısı göndereceğiz.">
      <form onSubmit={submit} noValidate>
        <Stack gap={16}>
          {errors.length > 0 && (
            <Alert color="orange" icon={<IconAlertCircle size={18} />}>
              {errors.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <TextInput
            label="E-posta"
            placeholder="ornek@mail.com"
            autoComplete="email"
            {...form.getInputProps('email')}
          />
          <TextInput
            label="Görünen ad"
            placeholder="Adınız"
            autoComplete="name"
            {...form.getInputProps('displayName')}
          />
          <Select
            label="Saat dilimi"
            data={zones}
            searchable
            allowDeselect={false}
            {...form.getInputProps('timeZoneId')}
          />
          <PasswordInput
            label="Şifre"
            description="En az 10 karakter. Uzun bir cümle kullanabilirsiniz."
            autoComplete="new-password"
            {...form.getInputProps('password')}
          />
          <Button type="submit" variant="rainbow" loading={submitting} fullWidth h={52} radius={14} fz={16} mt="xs">
            Kayıt ol
          </Button>
          <Text ta="center" fz={14}>
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
