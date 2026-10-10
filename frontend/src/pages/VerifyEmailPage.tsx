import { Button, Center, Loader, Stack, Text, ThemeIcon } from '@mantine/core'
import { IconCircleCheck, IconCircleX } from '@tabler/icons-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { verifyEmail } from '../api/auth'
import { ApiError } from '../api/client'
import { AuthLayout } from '../components/AuthLayout'

type State = { kind: 'loading' } | { kind: 'success' } | { kind: 'invalid'; message: string }

export function VerifyEmailPage() {
  const [params] = useSearchParams()
  const token = params.get('token')
  const [state, setState] = useState<State>(
    token ? { kind: 'loading' } : { kind: 'invalid', message: 'Doğrulama bağlantısı eksik veya hatalı.' },
  )
  // Token tek kullanımlıktır; StrictMode çift çalıştırması ikinci isteği atmamalı.
  const started = useRef(false)

  useEffect(() => {
    if (!token || started.current) return
    started.current = true
    verifyEmail(token)
      .then(() => setState({ kind: 'success' }))
      .catch((e: unknown) =>
        setState({
          kind: 'invalid',
          message: e instanceof ApiError ? e.message : 'Doğrulama sırasında bir hata oluştu.',
        }),
      )
  }, [token])

  return (
    <AuthLayout title="E-posta doğrulama">
      <Stack align="center" gap="md">
        {state.kind === 'loading' && (
          <>
            <Center h={72}>
              <Loader />
            </Center>
            <Text c="dimmed">Doğrulanıyor...</Text>
          </>
        )}
        {state.kind === 'success' && (
          <>
            <ThemeIcon size={72} radius="xl" variant="light" color="teal">
              <IconCircleCheck size={42} />
            </ThemeIcon>
            <Text fw={600} fz="lg">
              E-postan doğrulandı!
            </Text>
            <Text c="dimmed" ta="center">
              Artık giriş yapabilirsin.
            </Text>
            <Button component={Link} to="/login" fullWidth size="md">
              Giriş yap
            </Button>
          </>
        )}
        {state.kind === 'invalid' && (
          <>
            <ThemeIcon size={72} radius="xl" variant="light" color="red">
              <IconCircleX size={42} />
            </ThemeIcon>
            <Text fw={600} fz="lg">
              Bağlantı geçersiz
            </Text>
            <Text c="dimmed" ta="center">
              {state.message} Süresi dolmuş veya daha önce kullanılmış olabilir.
            </Text>
            <Button component={Link} to="/register" variant="light" fullWidth>
              Yeniden kayıt ol
            </Button>
            <Button component={Link} to="/login" variant="subtle" fullWidth>
              Giriş sayfasına dön
            </Button>
          </>
        )}
      </Stack>
    </AuthLayout>
  )
}
