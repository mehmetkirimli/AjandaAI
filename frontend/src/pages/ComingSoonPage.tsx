import { Button, Center, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { IconTools } from '@tabler/icons-react'
import { Link } from 'react-router-dom'

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <Center mih="60vh">
      <Stack align="center" gap="md">
        <ThemeIcon size={88} radius="xl" variant="light" color="grape">
          <IconTools size={46} />
        </ThemeIcon>
        <Title order={2}>{title}: Yakında</Title>
        <Text c="dimmed" ta="center" maw={380}>
          Bu sayfa üzerinde çalışıyoruz. Çok yakında burada olacak.
        </Text>
        <Button component={Link} to="/" variant="light">
          Haftalık panoya dön
        </Button>
      </Stack>
    </Center>
  )
}
