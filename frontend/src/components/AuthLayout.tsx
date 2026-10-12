import { Box, Stack, Text, Title } from '@mantine/core'
import type { ReactNode } from 'react'
import { Logo } from './Logo'

interface Props {
  title: string
  subtitle?: string
  children: ReactNode
}

// Linear giriş ekranı gibi: tek sütun, ortada, pazarlama paneli yok, uygulama zemini.
export function AuthLayout({ title, subtitle, children }: Props) {
  return (
    <Box
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 16,
        background: 'var(--app-bg)',
      }}
    >
      <Stack w="100%" maw={340} gap="xl">
        <Stack align="center" gap="lg">
          <Logo />
          <Stack gap={4} align="center">
            <Title order={2} fz={20} ta="center">
              {title}
            </Title>
            {subtitle && (
              <Text c="dimmed" size="sm" ta="center">
                {subtitle}
              </Text>
            )}
          </Stack>
        </Stack>
        {children}
      </Stack>
    </Box>
  )
}
