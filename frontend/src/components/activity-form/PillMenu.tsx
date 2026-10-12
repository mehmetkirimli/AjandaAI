import { Button, Menu } from '@mantine/core'
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
export function PillMenu<T extends string | number>({ ariaLabel, value, options, placeholder, icon, onChange }: Props<T>) {
  const selected = options.find((o) => o.value === value)
  return (
    <Menu position="bottom-start" width={190} withinPortal>
      <Menu.Target>
        <Button
          variant="default"
          size="compact-sm"
          radius="xl"
          aria-label={ariaLabel}
          leftSection={selected?.icon ?? icon}
          styles={{ root: { fontWeight: 500 } }}
        >
          {selected?.label ?? placeholder}
        </Button>
      </Menu.Target>
      <Menu.Dropdown>
        {options.map((o) => (
          <Menu.Item
            key={o.value}
            leftSection={o.icon}
            rightSection={o.value === value ? <IconCheck size={14} /> : undefined}
            onClick={() => onChange(o.value)}
          >
            {o.label}
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  )
}
