import { Button, Center, Stack, Text } from '@mantine/core'
import { IconMapOff } from '@tabler/icons-react'
import { Link } from 'react-router-dom'
import { PageBody, PanelHeader } from '../components/PanelHeader'

export function NotFoundPage() {
  return (
    <>
      <PanelHeader title="Sayfa bulunamadı" />
      <PageBody>
        <Center mih="50vh">
          <Stack align="center" gap="xs">
            <Text c="dimmed">
              <IconMapOff size={28} />
            </Text>
            <Text fw={500}>404 · Sayfa bulunamadı</Text>
            <Text c="dimmed" size="sm" ta="center" maw={340}>
              Aradığınız sayfa taşınmış ya da hiç var olmamış olabilir.
            </Text>
            <Button component={Link} to="/" variant="default" size="xs" mt="xs">
              Ana sayfaya dön
            </Button>
          </Stack>
        </Center>
      </PageBody>
    </>
  )
}
