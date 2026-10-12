import {
  Alert,
  Badge,
  Button,
  Group,
  Menu,
  Modal,
  Pagination,
  PasswordInput,
  SegmentedControl,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core'
import { useForm } from '@mantine/form'
import { useDebouncedValue } from '@mantine/hooks'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconDots, IconSearch, IconUserPlus } from '@tabler/icons-react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { createAdminUser, deactivateAdminUser, fetchAdminUsers, reactivateAdminUser, updateAdminUser } from '../../api/admin'
import type { AdminUser, UserRole } from '../../api/types'
import { useAuth } from '../../auth/context'
import { PageBody, PanelHeader } from '../../components/PanelHeader'
import { splitErrors } from '../../lib/formErrors'
import { browserTimeZone, timeZoneOptions } from '../../lib/timeZones'
import { AdminState } from './AdminState'
import { errorMessage, useConfirm } from './adminUtils'

const PAGE_SIZE = 20

function CreateUserModal({ opened, onClose }: { opened: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [general, setGeneral] = useState<string[]>([])
  const zones = useMemo(() => timeZoneOptions(browserTimeZone()), [])
  const form = useForm({
    initialValues: { displayName: '', email: '', timeZoneId: browserTimeZone(), password: '', role: 'User' as UserRole },
    validate: {
      displayName: (v) => (v.trim() ? null : 'Ad zorunludur.'),
      email: (v) => (/^\S+@\S+\.\S+$/.test(v.trim()) ? null : 'Geçerli bir e-posta girin.'),
      timeZoneId: (v) => (v ? null : 'Saat dilimi seçin.'),
      password: (v) => (v ? null : 'Şifre zorunludur.'),
    },
  })

  const close = () => {
    form.reset()
    setGeneral([])
    onClose()
  }

  const mutation = useMutation({
    mutationFn: createAdminUser,
    onSuccess: (u) => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      notifications.show({ color: 'teal', title: 'Kullanıcı eklendi', message: `${u.displayName} doğrulanmış olarak eklendi.` })
      close()
    },
    onError: (e) => {
      const { fields, general: g } = splitErrors(e, {
        displayName: ['görünen ad', 'ad '],
        email: ['email', 'e-posta'],
        timeZoneId: ['saat dilimi', 'timezone'],
        password: ['şifre', 'password'],
      })
      form.setErrors(fields)
      setGeneral(g)
    },
  })

  const submit = form.onSubmit((v) =>
    mutation.mutate({ ...v, displayName: v.displayName.trim(), email: v.email.trim() }),
  )

  return (
    <Modal opened={opened} onClose={close} title="Kullanıcı ekle" size="sm" centered>
      <form onSubmit={submit} noValidate>
        <Stack gap="sm">
          {general.length > 0 && (
            <Alert color="red" icon={<IconAlertCircle size={16} />}>
              {general.map((m) => (
                <div key={m}>{m}</div>
              ))}
            </Alert>
          )}
          <TextInput size="xs" label="Ad" data-autofocus {...form.getInputProps('displayName')} />
          <TextInput size="xs" label="E-posta" type="email" {...form.getInputProps('email')} />
          <Select size="xs" label="Saat dilimi" data={zones} searchable {...form.getInputProps('timeZoneId')} />
          <PasswordInput size="xs" label="Şifre" autoComplete="new-password" {...form.getInputProps('password')} />
          <Stack gap={4}>
            <Text size="xs" fw={500}>
              Rol
            </Text>
            <SegmentedControl size="xs" data={['User', 'Admin']} {...form.getInputProps('role')} />
          </Stack>
          <Text size="xs" c="dimmed">
            Admin'in eklediği kullanıcı doğrulanmış sayılır; e-posta doğrulaması gerekmez.
          </Text>
          <Group justify="flex-end" gap="xs">
            <Button size="xs" variant="default" onClick={close}>
              Vazgeç
            </Button>
            <Button size="xs" type="submit" loading={mutation.isPending}>
              Ekle
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}

export function AdminUsersPage() {
  const { user: me } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [debounced] = useDebouncedValue(search.trim(), 300)
  const [page, setPage] = useState(1)
  const [createOpen, setCreateOpen] = useState(false)
  const [confirmNode, confirm] = useConfirm()

  const query = useQuery({
    queryKey: ['admin', 'users', { page, search: debounced }],
    queryFn: () => fetchAdminUsers({ page, pageSize: PAGE_SIZE, search: debounced || undefined }),
    placeholderData: keepPreviousData,
  })

  const onDone = (title: string) => {
    void queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    notifications.show({ color: 'teal', message: title })
  }
  const onFail = (e: unknown) => notifications.show({ color: 'red', title: 'İşlem başarısız', message: errorMessage(e) })

  const roleMutation = useMutation({
    mutationFn: ({ u, role }: { u: AdminUser; role: UserRole }) =>
      updateAdminUser(u.id, { email: u.email, displayName: u.displayName, timeZoneId: u.timeZoneId, role }),
    onSuccess: (u) => onDone(`${u.displayName} artık ${u.role === 'Admin' ? 'Admin' : 'User'}.`),
    onError: onFail,
  })
  const deactivateMutation = useMutation({
    mutationFn: (u: AdminUser) => deactivateAdminUser(u.id),
    onSuccess: (u) => onDone(`${u.displayName} pasife alındı.`),
    onError: onFail,
  })

  const reactivateMutation = useMutation({
    mutationFn: (u: AdminUser) => reactivateAdminUser(u),
    onSuccess: (u) => onDone(`${u.displayName} yeniden aktif edildi.`),
    onError: onFail,
  })

  const items = query.data?.items ?? []

  return (
    <>
      <PanelHeader title="Yönetim" meta="Kullanıcılar">
        <Button size="compact-xs" leftSection={<IconUserPlus size={13} />} onClick={() => setCreateOpen(true)}>
          Kullanıcı ekle
        </Button>
      </PanelHeader>
      <PageBody>
        <Stack gap="sm" maw={1000}>
          <TextInput
            size="xs"
            maw={280}
            placeholder="Ad veya e-posta ara…"
            leftSection={<IconSearch size={13} />}
            value={search}
            onChange={(e) => {
              setSearch(e.currentTarget.value)
              setPage(1)
            }}
          />
          <AdminState
            loading={query.isPending}
            error={query.error}
            empty={items.length === 0}
            emptyText={debounced ? 'Aramayla eşleşen kullanıcı yok.' : 'Henüz kullanıcı yok.'}
            onRetry={() => void query.refetch()}
          >
            <Table.ScrollContainer minWidth={640}>
              <Table fz="xs" verticalSpacing={6} highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Ad</Table.Th>
                    <Table.Th>E-posta</Table.Th>
                    <Table.Th>Rol</Table.Th>
                    <Table.Th>Doğrulandı</Table.Th>
                    <Table.Th>Durum</Table.Th>
                    <Table.Th w={40} />
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {items.map((u) => {
                    const self = u.id === me?.id
                    const nextRole: UserRole = u.role === 'Admin' ? 'User' : 'Admin'
                    return (
                      <Table.Tr key={u.id} style={{ opacity: u.isActive ? 1 : 0.55 }}>
                        <Table.Td fw={500}>
                          {u.displayName}
                          {self && (
                            <Text span size="xs" c="dimmed" ml={6}>
                              (sen)
                            </Text>
                          )}
                        </Table.Td>
                        <Table.Td c="dimmed">{u.email}</Table.Td>
                        <Table.Td>
                          <Badge size="xs" variant={u.role === 'Admin' ? 'light' : 'default'}>
                            {u.role}
                          </Badge>
                        </Table.Td>
                        <Table.Td>{u.emailConfirmedAt ? 'Evet' : <Text span size="xs" c="dimmed">Hayır</Text>}</Table.Td>
                        <Table.Td>
                          <Badge size="xs" color={u.isActive ? 'teal' : 'gray'} variant="dot">
                            {u.isActive ? 'Aktif' : 'Pasif'}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Menu position="bottom-end" width={190}>
                            <Menu.Target>
                              <Button size="compact-xs" variant="subtle" color="gray" aria-label="İşlemler">
                                <IconDots size={14} />
                              </Button>
                            </Menu.Target>
                            <Menu.Dropdown>
                              <Tooltip label="Kendi rolünü düşüremezsin" disabled={!(self && u.role === 'Admin')}>
                                <div>
                                  <Menu.Item
                                    disabled={self || !u.isActive}
                                    onClick={() =>
                                      confirm({
                                        title: 'Rolü değiştir',
                                        message:
                                          nextRole === 'User'
                                            ? `${u.displayName} User rolüne düşürülecek ve oturumu kapanacak.`
                                            : `${u.displayName} Admin yapılacak.`,
                                        confirmLabel: nextRole === 'User' ? "User'a düşür" : 'Admin yap',
                                        onConfirm: () => roleMutation.mutate({ u, role: nextRole }),
                                      })
                                    }
                                  >
                                    {nextRole === 'Admin' ? 'Admin yap' : "User'a düşür"}
                                  </Menu.Item>
                                </div>
                              </Tooltip>
                              {u.isActive ? (
                                <Menu.Item
                                  color="red"
                                  disabled={self}
                                  onClick={() =>
                                    confirm({
                                      title: 'Pasife al',
                                      message: `${u.displayName} pasife alınacak; giriş yapamayacak ve oturumları kapanacak.`,
                                      confirmLabel: 'Pasife al',
                                      onConfirm: () => deactivateMutation.mutate(u),
                                    })
                                  }
                                >
                                  Pasife al
                                </Menu.Item>
                              ) : (
                                <Menu.Item onClick={() => reactivateMutation.mutate(u)}>Aktif et</Menu.Item>
                              )}
                            </Menu.Dropdown>
                          </Menu>
                        </Table.Td>
                      </Table.Tr>
                    )
                  })}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            {(query.data?.totalPages ?? 0) > 1 && (
              <Group justify="space-between">
                <Text size="xs" c="dimmed">
                  {query.data?.totalCount} kullanıcı
                </Text>
                <Pagination size="xs" total={query.data?.totalPages ?? 1} value={page} onChange={setPage} />
              </Group>
            )}
          </AdminState>
        </Stack>
      </PageBody>
      <CreateUserModal opened={createOpen} onClose={() => setCreateOpen(false)} />
      {confirmNode}
    </>
  )
}
