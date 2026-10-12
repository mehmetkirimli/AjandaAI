import { Button, Center, Stack, Text } from '@mantine/core'
import { IconTools } from '@tabler/icons-react'
import { Link } from 'react-router-dom'
import { PageBody, PanelHeader } from '../components/PanelHeader'

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <>
      <PanelHeader title={title} />
      <PageBody>
        <Center mih="50vh">
          <Stack align="center" gap="xs">
            <Text c="dimmed">
              <IconTools size={28} />
            </Text>
            <Text fw={500}>Yakında</Text>
            <Text c="dimmed" size="sm" ta="center" maw={340}>
              Bu sayfa üzerinde çalışıyoruz.
            </Text>
            <Button component={Link} to="/" variant="default" size="xs" mt="xs">
              Panoya dön
            </Button>
          </Stack>
        </Center>
      </PageBody>
    </>
  )
}
