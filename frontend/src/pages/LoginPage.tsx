import { Alert, Anchor, Button, PasswordInput, Stack, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { IconAlertCircle, IconAt, IconLock } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../auth/context'
import { splitErrors } from '../lib/formErrors'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'
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
    <AuthLayout title="Tekrar hoş geldin" subtitle="Ajandana ulaşmak için giriş yap.">
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
          <PasswordInput
            label="Şifre"
            placeholder="Şifreniz"
            leftSection={<IconLock size={16} />}
            autoComplete="current-password"
            {...form.getInputProps('password')}
          />
          <Button type="submit" loading={submitting} fullWidth size="md" mt="xs">
            Giriş yap
          </Button>
          <Text ta="center" size="sm" c="dimmed">
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
