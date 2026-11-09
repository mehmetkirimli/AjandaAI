// Profil → "Claude bağlantısı" kartı (ADR 0024 §1, docs/mcp-kurulum.md).
// Düz anahtar yalnızca bu bileşenin state'inde yaşar: kapatınca/sayfadan çıkınca silinir,
// localStorage'a ve console'a yazılmaz.

import { Alert, Badge, Button, CopyButton, Group, Modal, Stack, Text, TextInput } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertTriangle, IconCheck, IconCopy, IconPlug } from '@tabler/icons-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useState, type FormEvent } from 'react'
import { ApiError, apiBaseUrl } from '../api/client'
import { CardTitle } from '../components/CardTitle'
import { createToken, listTokens, revokeToken } from '../api/tokens'
import type { PersonalAccessToken, PersonalAccessTokenCreated } from '../api/types'

const TOKENS_KEY = ['auth', 'tokens']

function errorText(e: unknown): string {
  if (e instanceof ApiError) return e.errors.length > 0 ? e.errors.join(' ') : e.message
  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}

function formatDate(value: string): string {
  return dayjs(value).format('D MMM YYYY')
}

function CopyField({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={4}>
      <Group justify="space-between" wrap="nowrap">
        <Text fz={13} fw={600}>
          {label}
        </Text>
        <CopyButton value={value} timeout={2000}>
          {({ copied, copy }) => (
            <Button
              size="xs"
              variant="light"
              color={copied ? 'teal' : 'gray'}
              leftSection={copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
              onClick={copy}
            >
              {copied ? 'Kopyalandı' : 'Kopyala'}
            </Button>
          )}
        </CopyButton>
      </Group>
      <code className="mono token-box">{value}</code>
    </Stack>
  )
}

function TokenRow({ token, onRevoke }: { token: PersonalAccessToken; onRevoke: () => void }) {
  const revoked = token.revokedAt !== null
  const expired = !revoked && dayjs(token.expiresAt).isBefore(dayjs())
  const inactive = revoked || expired
  return (
    <li className="token-row" style={{ opacity: inactive ? 0.55 : 1 }}>
      <div style={{ minWidth: 0, flex: 1 }}>
        <Group gap={8} wrap="wrap">
          <Text fw={600} style={{ overflowWrap: 'anywhere' }}>
            {token.name}
          </Text>
          <span className="mono" style={{ fontSize: 12 }}>
            {token.prefix}…
          </span>
          {revoked && <Badge color="gray" variant="light">İptal edildi</Badge>}
          {expired && <Badge color="orange" variant="light">Süresi doldu</Badge>}
        </Group>
        <Text fz={12} c="dimmed">
          Oluşturma {formatDate(token.createdAt)} · Son kullanım{' '}
          {token.lastUsedAt ? formatDate(token.lastUsedAt) : 'hiç kullanılmadı'} · Bitiş {formatDate(token.expiresAt)}
        </Text>
      </div>
      {!inactive && (
        <Button size="xs" variant="subtle" color="red" onClick={onRevoke}>
          İptal et
        </Button>
      )}
    </li>
  )
}

export function ClaudeConnectionCard() {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [nameError, setNameError] = useState<string | null>(null)
  const [created, setCreated] = useState<PersonalAccessTokenCreated | null>(null)
  const [toRevoke, setToRevoke] = useState<PersonalAccessToken | null>(null)

  const tokens = useQuery({ queryKey: TOKENS_KEY, queryFn: listTokens })

  const create = useMutation({
    mutationFn: (n: string) => createToken(n),
    onSuccess: (data) => {
      setCreated(data)
      setName('')
      setNameError(null)
    },
    onError: (e) => setNameError(errorText(e)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: TOKENS_KEY }),
  })

  const revoke = useMutation({
    mutationFn: (id: number) => revokeToken(id),
    onSuccess: () => {
      setToRevoke(null)
      notifications.show({ color: 'teal', title: 'İptal edildi', message: 'Anahtar artık kullanılamaz.' })
    },
    onError: (e) => notifications.show({ color: 'red', title: 'İptal edilemedi', message: errorText(e) }),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: TOKENS_KEY }),
  })

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (name.trim().length === 0) {
      setNameError('Anahtar adı zorunludur.')
      return
    }
    create.mutate(name.trim())
  }

  const command = created
    ? `claude mcp add --transport http --scope local ajandaai ${apiBaseUrl()}/mcp --header "Authorization: Bearer ${created.token}"`
    : ''

  return (
    <section className="paper" style={{ padding: 22 }}>
      <Stack>
        <CardTitle icon={<IconPlug size={18} stroke={1.7} />}>Claude bağlantısı</CardTitle>
        <Text fz={14}>Claude'un ajandana erişmesi için anahtar. Yalnızca araçlara erişir, şifren değildir.</Text>

        {created ? (
          <Stack gap="sm" className="token-created">
            <Alert color="orange" icon={<IconAlertTriangle size={16} />}>
              Bu anahtar bir daha gösterilmeyecek.
            </Alert>
            <CopyField label={`Anahtar: ${created.name}`} value={created.token} />
            <CopyField label="Claude Code komutu" value={command} />
            <Group justify="flex-end">
              <Button variant="default" onClick={() => setCreated(null)}>
                Kaydettim, kapat
              </Button>
            </Group>
          </Stack>
        ) : (
          <form onSubmit={submit} noValidate>
            <Group align="flex-start" wrap="wrap" gap="sm">
              <TextInput
                style={{ flex: '1 1 220px' }}
                label="Anahtar adı"
                placeholder="ör. İş bilgisayarı"
                value={name}
                maxLength={100}
                onChange={(e) => {
                  setName(e.currentTarget.value)
                  setNameError(null)
                }}
                error={nameError}
              />
              <Button type="submit" loading={create.isPending} mt={25} h={36} variant="ink">
                Anahtar oluştur
              </Button>
            </Group>
          </form>
        )}

        {tokens.isError && (
          <Alert color="orange" icon={<IconAlertTriangle size={16} />}>
            {errorText(tokens.error)}
          </Alert>
        )}
        {tokens.data && tokens.data.length === 0 && (
          <Text fz={13} c="dimmed">
            Henüz anahtarın yok.
          </Text>
        )}
        {tokens.data && tokens.data.length > 0 && (
          <ul className="token-list">
            {tokens.data.map((t) => (
              <TokenRow key={t.id} token={t} onRevoke={() => setToRevoke(t)} />
            ))}
          </ul>
        )}
      </Stack>

      <Modal opened={toRevoke !== null} onClose={() => setToRevoke(null)} title="Anahtarı iptal et" centered>
        <Stack>
          <Text fz={14}>
            <b>{toRevoke?.name}</b> anahtarını kullanan Claude bağlantısı hemen çalışmayı bırakır. Bu işlem geri alınamaz.
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setToRevoke(null)}>
              Vazgeç
            </Button>
            <Button color="red" loading={revoke.isPending} onClick={() => toRevoke && revoke.mutate(toRevoke.id)}>
              İptal et
            </Button>
          </Group>
        </Stack>
      </Modal>
    </section>
  )
}
