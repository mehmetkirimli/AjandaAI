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
            <LostPage width={160} />
            <Text fw={800} fz={22}>
              404 · Sayfa bulunamadı
            </Text>
            <Text ta="center" maw={360} style={{ opacity: 0.8 }}>
              Aradığın sayfa yırtılmış ya da hiç yazılmamış olabilir.
            </Text>
            <Button component={Link} to="/" variant="rainbow" h={48} px={24} radius={14} mt="xs">
              Panoya dön
            </Button>
          </Stack>
        </Center>
      </PageBody>
    </>
  )
}
