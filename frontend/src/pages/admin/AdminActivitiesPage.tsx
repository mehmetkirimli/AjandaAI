import { Badge, Button, Group, Pagination, Select, Stack, Table, Text } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { useMemo, useState } from 'react'
import { deactivateAdminActivity, fetchAdminActivities, fetchAllAdminUsers, fetchAllCategories } from '../../api/admin'
import type { AdminActivity } from '../../api/types'
import { PageBody, PanelHeader } from '../../components/PanelHeader'
import { categoryColor } from '../../lib/categoryColor'
import { STATUS_OPTIONS, statusLabel } from '../../lib/activityOptions'
import { AdminState } from './AdminState'
import { errorMessage, useConfirm } from './adminUtils'

const PAGE_SIZE = 20

const ACTIVE_OPTIONS = [
  { value: 'true', label: 'Aktif' },
  { value: 'false', label: 'Pasif' },
]

// Yönetim API'si veri minimizasyonu gereği başlık döndürmez (ADR 0018 Karar 6); satır #Id ile tanımlanır.
export function AdminActivitiesPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [userId, setUserId] = useState<string | null>(null)
  const [status, setStatus] = useState<string | null>(null)
  const [isActive, setIsActive] = useState<string | null>('true')
  const [confirmNode, confirm] = useConfirm()

  const usersQuery = useQuery({ queryKey: ['admin', 'users', 'all'], queryFn: fetchAllAdminUsers, staleTime: 60_000 })
  const categoriesQuery = useQuery({ queryKey: ['categories', 'admin'], queryFn: fetchAllCategories, staleTime: 60_000 })
  const query = useQuery({
    queryKey: ['admin', 'activities', { page, userId, status, isActive }],
    queryFn: () =>
      fetchAdminActivities({
        page,
        pageSize: PAGE_SIZE,
        userId: userId ? Number(userId) : undefined,
        status: status ?? undefined,
        isActive: isActive ?? undefined,
      }),
    placeholderData: keepPreviousData,
  })

  const userById = useMemo(() => new Map((usersQuery.data ?? []).map((u) => [u.id, u])), [usersQuery.data])
  const categoryById = useMemo(
    () => new Map((categoriesQuery.data ?? []).map((c) => [c.id, c.name])),
    [categoriesQuery.data],
  )
  const userOptions = useMemo(
    () => (usersQuery.data ?? []).map((u) => ({ value: String(u.id), label: `${u.displayName} (${u.email})` })),
    [usersQuery.data],
  )

  const deactivate = useMutation({
    mutationFn: (a: AdminActivity) => deactivateAdminActivity(a.id),
    onSuccess: (a) => {
      notifications.show({ color: 'teal', message: `#${a.id} pasife alındı.` })
      void queryClient.invalidateQueries({ queryKey: ['admin', 'activities'] })
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
    },
    onError: (e) => notifications.show({ color: 'red', title: 'Pasife alınamadı', message: errorMessage(e) }),
  })

  const setFilter = (fn: (v: string | null) => void) => (v: string | null) => {
    fn(v)
    setPage(1)
  }

  const items = query.data?.items ?? []
  const filtered = Boolean(userId || status || isActive)

  return (
    <>
      <PanelHeader title="Yönetim" meta="Aktiviteler" />
      <PageBody>
        <Stack gap="sm" maw={1000}>
          <Group gap="xs">
            <Select
              size="xs"
              w={240}
              placeholder="Tüm kullanıcılar"
              data={userOptions}
              value={userId}
              onChange={setFilter(setUserId)}
              searchable
              clearable
            />
            <Select
              size="xs"
              w={150}
              placeholder="Tüm durumlar"
              data={STATUS_OPTIONS}
              value={status}
              onChange={setFilter(setStatus)}
              clearable
            />
            <Select
              size="xs"
              w={120}
              placeholder="Aktif + pasif"
              data={ACTIVE_OPTIONS}
              value={isActive}
              onChange={setFilter(setIsActive)}
              clearable
            />
          </Group>
          <AdminState
            loading={query.isPending}
            error={query.error}
            empty={items.length === 0}
            emptyText={filtered ? 'Filtreyle eşleşen aktivite yok.' : 'Henüz aktivite yok.'}
            onRetry={() => void query.refetch()}
          >
            <Table.ScrollContainer minWidth={640}>
              <Table fz="xs" verticalSpacing={6} highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Aktivite</Table.Th>
                    <Table.Th>Sahip</Table.Th>
                    <Table.Th>Kategori</Table.Th>
                    <Table.Th>Başlangıç</Table.Th>
                    <Table.Th>Durum</Table.Th>
                    <Table.Th w={90} />
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {items.map((a) => {
                    const owner = userById.get(a.userId)
                    return (
                      <Table.Tr key={a.id} style={{ opacity: a.isActive ? 1 : 0.55 }}>
                        <Table.Td fw={500} title="Başlık gizlilik gereği yönetim ekranında gösterilmez">
                          #{a.id}
                        </Table.Td>
                        <Table.Td>
                          {owner ? (
                            <Text span size="xs" title={owner.email}>
                              {owner.displayName}
                            </Text>
                          ) : (
                            <Text span size="xs" c="dimmed">
                              Kullanıcı #{a.userId}
                            </Text>
                          )}
                        </Table.Td>
                        <Table.Td>
                          <Group gap={6} wrap="nowrap">
                            <span
                              aria-hidden
                              style={{ width: 8, height: 8, borderRadius: 2, background: categoryColor(a.categoryId) }}
                            />
                            {categoryById.get(a.categoryId) ?? `#${a.categoryId}`}
                          </Group>
                        </Table.Td>
                        <Table.Td c="dimmed">{dayjs(a.start).format('DD.MM.YYYY HH:mm')}</Table.Td>
                        <Table.Td>
                          <Group gap={6} wrap="nowrap">
                            {statusLabel(a.status)}
                            {!a.isActive && (
                              <Badge size="xs" color="gray" variant="light">
                                Pasif
                              </Badge>
                            )}
                          </Group>
                        </Table.Td>
                        <Table.Td>
                          {a.isActive && (
                            <Button
                              size="compact-xs"
                              variant="subtle"
                              color="red"
                              onClick={() =>
                                confirm({
                                  title: 'Aktiviteyi pasife al',
                                  message: `#${a.id} pasife alınacak; sahibinin panosunda ve takviminde görünmeyecek.`,
                                  confirmLabel: 'Pasife al',
                                  onConfirm: () => deactivate.mutate(a),
                                })
                              }
                            >
                              Pasife al
                            </Button>
                          )}
                        </Table.Td>
                      </Table.Tr>
                    )
                  })}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="space-between">
              <Text size="xs" c="dimmed">
                {query.data?.totalCount ?? 0} aktivite
              </Text>
              {(query.data?.totalPages ?? 0) > 1 && (
                <Pagination size="xs" total={query.data?.totalPages ?? 1} value={page} onChange={setPage} />
              )}
            </Group>
          </AdminState>
        </Stack>
      </PageBody>
      {confirmNode}
    </>
  )
}
