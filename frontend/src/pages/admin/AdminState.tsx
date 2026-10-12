import { Alert, Button, Center, Loader, Stack, Text } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import type { ReactNode } from 'react'
import { errorMessage } from './adminUtils'

// Yönetim ekranlarının ortak yükleniyor / hata / boş durumları.
export function AdminState({
  loading,
  error,
  empty,
  emptyText,
  onRetry,
  children,
}: {
  loading: boolean
  error: unknown
  empty: boolean
  emptyText: string
  onRetry: () => void
  children: ReactNode
}) {
  if (loading) {
    return (
      <Center py="xl">
        <Loader size="sm" />
      </Center>
    )
  }
  if (error) {
    return (
      <Alert color="red" icon={<IconAlertCircle size={16} />} title="Yüklenemedi">
        <Stack gap="xs" align="flex-start">
          <Text size="sm">{errorMessage(error)}</Text>
          <Button size="compact-xs" variant="default" onClick={onRetry}>
            Tekrar dene
          </Button>
        </Stack>
      </Alert>
    )
  }
  if (empty) {
    return (
      <Center py="xl">
        <Text size="sm" c="dimmed">
          {emptyText}
        </Text>
      </Center>
    )
  }
  return <>{children}</>
}

