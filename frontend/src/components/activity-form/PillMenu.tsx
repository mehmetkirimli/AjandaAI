import { Button, Menu, Text } from '@mantine/core'
import { IconCheck } from '@tabler/icons-react'
import type { ReactNode } from 'react'

export interface PillOption<T> {
  value: T
  label: string
  icon?: ReactNode
}

interface Props<T> {
  ariaLabel: string
  value: T | null
  options: PillOption<T>[]
  placeholder: string
  icon?: ReactNode
  onChange: (value: T) => void
}

// Küçük nötr hap: tıklayınca seçenek menüsü açar (Linear'ın özellik hapları gibi).
// Uzun etiketler kısaltılır (title ile tamamı görünür); uzun listeler menü içinde kayar.
export function PillMenu<T extends string | number>({ ariaLabel, value, options, placeholder, icon, onChange }: Props<T>) {
  const selected = options.find((o) => o.value === value)
  const label = selected?.label ?? placeholder
  return (
    <Menu position="bottom-start" width={220} withinPortal>
      <Menu.Target>
        <Button
          variant="sketch"
          h={44}
          px={14}
          radius={6}
          fz={14}
          aria-label={ariaLabel}
          title={label}
          maw={220}
          leftSection={selected?.icon ?? icon}
          styles={{ root: { fontWeight: 500 }, inner: { minWidth: 0 }, label: { minWidth: 0 } }}
        >
          <Text span inherit truncate>
            {label}
          </Text>
        </Button>
      </Menu.Target>
      <Menu.Dropdown mah={280} style={{ overflowY: 'auto' }}>
        {options.map((o) => (
          <Menu.Item
            key={o.value}
            title={o.label}
            leftSection={o.icon}
            rightSection={o.value === value ? <IconCheck size={14} /> : undefined}
            onClick={() => onChange(o.value)}
            styles={{ itemLabel: { minWidth: 0 } }}
          >
            <Text span inherit truncate display="block">
              {o.label}
            </Text>
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  )
}
