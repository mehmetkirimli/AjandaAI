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
    <AuthLayout title="E-posta doğrulama" subtitle="Defterinin anahtarını kontrol ediyoruz.">
      <Stack align="center" gap="md">
        {state.kind === 'loading' && (
          <>
            <Center h={72}>
              <Loader color="var(--graphite)" />
            </Center>
            <Text>Doğrulanıyor…</Text>
          </>
        )}
        {state.kind === 'success' && (
          <>
            <ThemeIcon size={72} radius="50%" variant="outline" color="graphite" style={{ borderWidth: 1.8 }}>
              <IconCircleCheck size={40} stroke={1.5} />
            </ThemeIcon>
            <Text fw={600} fz="lg">
              E-postan doğrulandı!
            </Text>
            <Text ta="center" c="dimmed">
              Artık giriş yapabilirsin.
            </Text>
            <Button component={Link} to="/login" variant="ink" fullWidth h={46} fz={14}>
              Giriş yap
            </Button>
          </>
        )}
        {state.kind === 'invalid' && (
          <>
            <ThemeIcon size={72} radius="50%" variant="outline" color="red" style={{ borderWidth: 1.8 }}>
              <IconCircleX size={40} stroke={1.5} />
            </ThemeIcon>
            <Text fw={600} fz="lg">
              Bağlantı geçersiz
            </Text>
            <Text ta="center" c="dimmed">
              {state.message} Süresi dolmuş veya daha önce kullanılmış olabilir.
            </Text>
            <Button component={Link} to="/register" variant="sketch" fullWidth h={44}>
              Yeniden kayıt ol
            </Button>
            <Button component={Link} to="/login" variant="subtle" color="gray" fullWidth h={44}>
              Giriş sayfasına dön
            </Button>
          </>
        )}
      </Stack>
    </AuthLayout>
  )
}
