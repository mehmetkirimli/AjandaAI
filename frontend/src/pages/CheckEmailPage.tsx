import { Alert, Button, Stack, Text } from '@mantine/core'
import { IconInfoCircle } from '@tabler/icons-react'
import { Link, useLocation } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { MailCheck } from '../components/illustrations'

export function CheckEmailPage() {
  const location = useLocation()
  const email = (location.state as { email?: string } | null)?.email

  return (
    <AuthLayout title="E-postanı kontrol et" subtitle="Defterin hazır, bir adım kaldı.">
      <Stack align="center" gap="md">
        <MailCheck width={140} />
        <Text ta="center">
          {email ? (
            <>
              <b>{email}</b> adresine bir doğrulama bağlantısı gönderdik.
            </>
          ) : (
            'E-posta adresine bir doğrulama bağlantısı gönderdik.'
          )}{' '}
          Bağlantıya tıkladıktan sonra giriş yapabilirsin.
        </Text>
        <Alert color="teal" icon={<IconInfoCircle size={18} />} w="100%" title="Geliştirme notu">
          Geliştirme ortamında e-posta gönderilmez: doğrulama bağlantısı backend konsol çıktısında (log) yazılır.
        </Alert>
        <Button component={Link} to="/login" variant="rainbow" fullWidth h={52} radius={14} fz={16}>
          Giriş sayfasına git
        </Button>
      </Stack>
    </AuthLayout>
  )
}
