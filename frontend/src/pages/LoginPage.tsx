import { Alert, Anchor, Button, PasswordInput, Stack, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { IconAlertCircle, IconCircleCheck } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../auth/context'
import { splitErrors } from '../lib/formErrors'

export function LoginPage() {
  const { login, notice } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const navState = location.state as { from?: string; info?: string } | null
  const from = navState?.from ?? '/'
  const info = navState?.info ?? notice
  const [submitting, setSubmitting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])

  const form = useForm({
    initialValues: { email: '', password: '' },
    validate: {
      email: (v) => (/^\S+@\S+\.\S+$/.test(v.trim()) ? null : 'Geçerli bir e-posta adresi girin.'),
      password: (v) => (v.length === 0 ? 'Şifre zorunludur.' : null),
    },
  })

  const submit = form.onSubmit(async (values) => {
    setSubmitting(true)
    setErrors([])
    try {
      await login(values.email.trim(), values.password)
      navigate(from, { replace: true })
    } catch (e) {
      const { fields, general } = splitErrors(e, { email: ['email', 'e-posta'], password: ['şifre'] })
      form.setErrors(fields)
      setErrors(general)
    } finally {
      setSubmitting(false)
    }
  })

  return (
    <AuthLayout title="Tekrar hoş geldin" subtitle="Defterini açmak için giriş yap.">
      <form onSubmit={submit} noValidate>
        <Stack gap={18}>
          {info && (
            <Alert color="teal" icon={<IconCircleCheck size={18} />}>
              {info}
            </Alert>
          )}
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
          <PasswordInput
            label="Şifre"
            placeholder="Şifreniz"
            autoComplete="current-password"
            {...form.getInputProps('password')}
          />
          <Button type="submit" variant="rainbow" loading={submitting} fullWidth h={52} radius={14} fz={16} mt="xs">
            Defterimi aç
          </Button>
          <Text ta="center" fz={14}>
            Hesabın yok mu?{' '}
            <Anchor component={Link} to="/register" fw={600}>
              Kayıt ol
            </Anchor>
          </Text>
        </Stack>
      </form>
    </AuthLayout>
  )
}
