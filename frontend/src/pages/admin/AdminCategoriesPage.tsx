import { ActionIcon, Badge, Button, Group, Paper, Stack, Text, TextInput, Tooltip } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconGripVertical, IconPencil, IconPlus, IconX } from '@tabler/icons-react'
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import { restrictToVerticalAxis } from '@dnd-kit/modifiers'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { createCategory, fetchAllCategories, reorderCategories, updateCategory } from '../../api/admin'
import type { Category } from '../../api/types'
import { PageBody, PanelHeader } from '../../components/PanelHeader'
import { categoryColor } from '../../lib/categoryColor'
import { AdminState } from './AdminState'
import { errorMessage, useConfirm } from './adminUtils'

const ADMIN_KEY = ['categories', 'admin'] as const

function CategoryRow({
  category,
  onRename,
  onToggle,
}: {
  category: Category
  onRename: (name: string) => Promise<unknown>
  onToggle: () => void
}) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({
    id: category.id,
  })
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(category.name)

  const save = async () => {
    const trimmed = name.trim()
    if (!trimmed || trimmed === category.name) {
      setEditing(false)
      setName(category.name)
      return
    }
    try {
      await onRename(trimmed)
      setEditing(false)
    } catch {
      // Hata bildirimi üst bileşende; düzenleme açık kalır.
    }
  }

  return (
    <Group
      ref={setNodeRef}
      gap="xs"
      wrap="nowrap"
      px={8}
      py={6}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        borderBottom: '1px solid var(--app-line)',
        background: isDragging ? 'var(--app-layer-1)' : undefined,
        position: 'relative',
        zIndex: isDragging ? 1 : undefined,
      }}
    >
      <ActionIcon
        ref={setActivatorNodeRef}
        size="sm"
        variant="subtle"
        color="gray"
        aria-label={`${category.name} sırasını değiştir`}
        style={{ cursor: isDragging ? 'grabbing' : 'grab', touchAction: 'none' }}
        {...attributes}
        {...listeners}
      >
        <IconGripVertical size={14} />
      </ActionIcon>
      <span
        aria-hidden
        style={{
          width: 8,
          height: 8,
          borderRadius: 2,
          flex: 'none',
          background: categoryColor(category.id),
          opacity: category.isActive ? 1 : 0.4,
        }}
      />
      {editing ? (
        <TextInput
          size="xs"
          style={{ flex: 1 }}
          value={name}
          autoFocus
          onChange={(e) => setName(e.currentTarget.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') void save()
            if (e.key === 'Escape') {
              setEditing(false)
              setName(category.name)
            }
          }}
        />
      ) : (
        <Text size="sm" style={{ flex: 1, opacity: category.isActive ? 1 : 0.5 }} truncate>
          {category.name}
        </Text>
      )}
      {!category.isActive && (
        <Badge size="xs" color="gray" variant="light">
          Pasif
        </Badge>
      )}
      {editing ? (
        <Group gap={2} wrap="nowrap">
          <ActionIcon size="sm" variant="subtle" aria-label="Kaydet" onClick={() => void save()}>
            <IconCheck size={14} />
          </ActionIcon>
          <ActionIcon
            size="sm"
            variant="subtle"
            color="gray"
            aria-label="Vazgeç"
            onClick={() => {
              setEditing(false)
              setName(category.name)
            }}
          >
            <IconX size={14} />
          </ActionIcon>
        </Group>
      ) : (
        <Group gap={2} wrap="nowrap">
          <Tooltip label="Yeniden adlandır">
            <ActionIcon size="sm" variant="subtle" color="gray" aria-label="Yeniden adlandır" onClick={() => setEditing(true)}>
              <IconPencil size={14} />
            </ActionIcon>
          </Tooltip>
          <Button size="compact-xs" variant="subtle" color={category.isActive ? 'red' : 'teal'} onClick={onToggle}>
            {category.isActive ? 'Pasife al' : 'Aktif et'}
          </Button>
        </Group>
      )}
    </Group>
  )
}

export function AdminCategoriesPage() {
  const queryClient = useQueryClient()
  const [newName, setNewName] = useState('')
  const [confirmNode, confirm] = useConfirm()
  const query = useQuery({ queryKey: ADMIN_KEY, queryFn: fetchAllCategories })
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  // Hem yönetim listesini hem diğer ekranların ['categories'] önbelleğini tazeler.
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['categories'] })
  const fail = (title: string) => (e: unknown) => notifications.show({ color: 'red', title, message: errorMessage(e) })

  const reorder = useMutation({
    mutationFn: (ids: number[]) => reorderCategories(ids),
    onMutate: async (ids) => {
      await queryClient.cancelQueries({ queryKey: ADMIN_KEY })
      const previous = queryClient.getQueryData<Category[]>(ADMIN_KEY)
      if (previous) {
        const byId = new Map(previous.map((c) => [c.id, c]))
        queryClient.setQueryData<Category[]>(
          ADMIN_KEY,
          ids.map((id, i) => ({ ...byId.get(id)!, sortOrder: i + 1 })),
        )
      }
      return { previous }
    },
    onError: (_e, _ids, ctx) => {
      if (ctx?.previous) queryClient.setQueryData(ADMIN_KEY, ctx.previous)
      notifications.show({ color: 'red', message: 'Sıralama kaydedilemedi' })
    },
    onSettled: refresh,
  })

  const create = useMutation({
    mutationFn: createCategory,
    onSuccess: (c) => {
      setNewName('')
      notifications.show({ color: 'teal', message: `"${c.name}" eklendi.` })
    },
    onError: fail('Kategori eklenemedi'),
    onSettled: refresh,
  })

  const update = useMutation({
    mutationFn: ({ c, name, isActive }: { c: Category; name: string; isActive: boolean }) =>
      updateCategory(c.id, name, isActive),
    onError: fail('Kategori güncellenemedi'),
    onSettled: refresh,
  })

  const items = query.data ?? []

  const onDragEnd = ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) return
    const ids = items.map((c) => c.id)
    const from = ids.indexOf(Number(active.id))
    const to = ids.indexOf(Number(over.id))
    if (from < 0 || to < 0) return
    reorder.mutate(arrayMove(ids, from, to))
  }

  const toggle = (c: Category) => {
    if (c.isActive) {
      confirm({
        title: 'Kategoriyi pasife al',
        message: `"${c.name}" pasife alınacak; yeni aktivitelerde seçilemeyecek. Mevcut aktiviteler etkilenmez.`,
        confirmLabel: 'Pasife al',
        onConfirm: () => update.mutate({ c, name: c.name, isActive: false }),
      })
    } else {
      update.mutate({ c, name: c.name, isActive: true })
    }
  }

  const submitNew = (e: React.FormEvent) => {
    e.preventDefault()
    const name = newName.trim()
    if (name) create.mutate(name)
  }

  return (
    <>
      <PanelHeader title="Yönetim" meta="Kategoriler" />
      <PageBody>
        <Stack gap="sm" maw={560}>
          <Text size="xs" c="dimmed">
            Tutamaçtan sürükleyerek sırayı değiştir. Bu sıra aktivite formunda ve filtrelerde kullanılır.
          </Text>
          <form onSubmit={submitNew}>
            <Group gap="xs" wrap="nowrap">
              <TextInput
                size="xs"
                style={{ flex: 1 }}
                placeholder="Yeni kategori adı"
                value={newName}
                onChange={(e) => setNewName(e.currentTarget.value)}
              />
              <Button
                size="xs"
                type="submit"
                leftSection={<IconPlus size={13} />}
                loading={create.isPending}
                disabled={!newName.trim()}
              >
                Ekle
              </Button>
            </Group>
          </form>
          <AdminState
            loading={query.isPending}
            error={query.error}
            empty={items.length === 0}
            emptyText="Henüz kategori yok."
            onRetry={() => void query.refetch()}
          >
            <Paper withBorder radius="md" style={{ overflow: 'hidden' }}>
              <DndContext
                sensors={sensors}
                collisionDetection={closestCenter}
                modifiers={[restrictToVerticalAxis]}
                onDragEnd={onDragEnd}
              >
                <SortableContext items={items.map((c) => c.id)} strategy={verticalListSortingStrategy}>
                  {items.map((c) => (
                    <CategoryRow
                      key={c.id}
                      category={c}
                      onRename={(name) => update.mutateAsync({ c, name, isActive: c.isActive })}
                      onToggle={() => toggle(c)}
                    />
                  ))}
                </SortableContext>
              </DndContext>
            </Paper>
          </AdminState>
        </Stack>
      </PageBody>
      {confirmNode}
    </>
  )
}
