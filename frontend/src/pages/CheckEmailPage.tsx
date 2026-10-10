import { Alert, Button, Center, Stack, Text, ThemeIcon } from '@mantine/core'
import { IconInfoCircle, IconMailCheck } from '@tabler/icons-react'
import { Link, useLocation } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'

export function CheckEmailPage() {
  const location = useLocation()
  const email = (location.state as { email?: string } | null)?.email

  return (
    <AuthLayout title="E-postanı kontrol et">
      <Stack align="center" gap="md">
        <Center>
          <ThemeIcon size={72} radius="xl" variant="light" color="indigo">
            <IconMailCheck size={40} />
          </ThemeIcon>
        </Center>
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
        <Alert variant="light" color="yellow" icon={<IconInfoCircle size={18} />} w="100%" title="Geliştirme notu">
          Geliştirme ortamında e-posta gönderilmez: doğrulama bağlantısı backend konsol çıktısında (log) yazılır.
        </Alert>
        <Button component={Link} to="/login" variant="light" fullWidth>
          Giriş sayfasına git
        </Button>
      </Stack>
    </AuthLayout>
  )
}
