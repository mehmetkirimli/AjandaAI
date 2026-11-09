import { Button, Center, Stack, Text } from '@mantine/core'
import { Link } from 'react-router-dom'
import { LostPage } from '../components/illustrations'
import { PageBody, PanelHeader } from '../components/PanelHeader'

export function NotFoundPage() {
  return (
    <>
      <PanelHeader title="Sayfa bulunamadı" kicker="bu sayfa defterde yok" />
      <PageBody>
        <Center mih="50vh">
          <Stack align="center" gap="sm">
            <LostPage width={150} />
            <Text fw={700} fz={26} ff="var(--font-hand)">
              404 · Sayfa bulunamadı
            </Text>
            <Text ta="center" maw={360} c="dimmed">
              Aradığın sayfa yırtılmış ya da hiç yazılmamış olabilir.
            </Text>
            <Button component={Link} to="/" variant="ink" h={44} px={24} mt="xs">
              Panoya dön
            </Button>
          </Stack>
        </Center>
      </PageBody>
    </>
  )
}
