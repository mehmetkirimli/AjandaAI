import { em } from '@mantine/core'
import { useMediaQuery } from '@mantine/hooks'
import { Notifications } from '@mantine/notifications'

// Mobilde bildirimler üstte: alttaki menü ve "+" butonunu örtmesin.
export function ResponsiveNotifications() {
  const mobile = useMediaQuery(`(max-width: ${em(767.98)})`) ?? false
  return <Notifications position={mobile ? 'top-center' : 'bottom-right'} />
}
