import { Box, Center, Group, Paper, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { IconArrowsMove, IconCalendarWeek, IconSparkles } from '@tabler/icons-react'
import type { ReactNode } from 'react'
import { Logo } from './Logo'

const FEATURES = [
  { icon: IconCalendarWeek, text: 'Haftanı tek bakışta gör' },
  { icon: IconArrowsMove, text: 'Sürükle, bırak, yeniden planla' },
  { icon: IconSparkles, text: 'Yapay zekâ ile akıllı ajanda (yakında)' },
]

interface Props {
  title: string
  subtitle?: string
  children: ReactNode
}

// Solda marka paneli, sağda form kartı.
export function AuthLayout({ title, subtitle, children }: Props) {
  return (
    <Box style={{ display: 'flex', minHeight: '100vh' }}>
      <Box
        visibleFrom="md"
        p={64}
        style={{
          flex: '0 0 45%',
          background: 'linear-gradient(135deg, #364fc7 0%, #6741d9 55%, #9c36b5 100%)',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          color: 'white',
        }}
      >
        <Logo light />
        <Stack gap="xl">
          <Title order={1} fz={44} lh={1.15} c="white">
            Zamanını planla,
            <br />
            gününü sahiplen.
          </Title>
          <Text fz="lg" c="indigo.1" maw={440}>
            AjandaAI aktivitelerini, kategorilerini ve hatırlatmalarını tek bir yerde, sade ve ferah bir arayüzde toplar.
          </Text>
          <Stack gap="sm">
            {FEATURES.map(({ icon: Icon, text }) => (
              <Group key={text} gap="sm">
                <ThemeIcon variant="white" color="indigo" radius="xl" size={32}>
                  <Icon size={18} />
                </ThemeIcon>
                <Text c="white">{text}</Text>
              </Group>
            ))}
          </Stack>
        </Stack>
        <Text size="sm" c="indigo.2">
          © AjandaAI
        </Text>
      </Box>

      <Center flex={1} p="md" bg="var(--mantine-color-gray-0)">
        <Stack w="100%" maw={440} gap="lg">
          <Box hiddenFrom="md">
            <Logo />
          </Box>
          <Paper withBorder shadow="md" radius="lg" p={{ base: 'lg', sm: 'xl' }}>
            <Stack gap="xs" mb="lg">
              <Title order={2}>{title}</Title>
              {subtitle && (
                <Text c="dimmed" size="sm">
                  {subtitle}
                </Text>
              )}
            </Stack>
            {children}
          </Paper>
        </Stack>
      </Center>
    </Box>
  )
}
