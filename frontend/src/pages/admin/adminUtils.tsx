import { Button, Group, Modal, Stack, Text } from '@mantine/core'
import { useState, type ReactNode } from 'react'
import { ApiError } from '../../api/client'

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.errors.length > 0 ? error.errors.join(' ') : error.message
  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}

// Onaylı işlem (pasife alma vb.): useConfirm() modal düğümünü ve açıcı fonksiyonu döner.
export interface ConfirmRequest {
  title: string
  message: string
  confirmLabel: string
  onConfirm: () => void
}

export function useConfirm(): [ReactNode, (req: ConfirmRequest) => void] {
  const [req, setReq] = useState<ConfirmRequest | null>(null)
  const node = (
    <Modal opened={req !== null} onClose={() => setReq(null)} title={req?.title} size="sm" centered>
      <Stack>
        <Text size="sm">{req?.message}</Text>
        <Group justify="flex-end" gap="xs">
          <Button size="xs" variant="default" onClick={() => setReq(null)}>
            Vazgeç
          </Button>
          <Button
            size="xs"
            color="red"
            onClick={() => {
              req?.onConfirm()
              setReq(null)
            }}
          >
            {req?.confirmLabel}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
  return [node, setReq]
}
