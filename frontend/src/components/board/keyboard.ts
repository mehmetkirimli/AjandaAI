import type { CollisionDetection, KeyboardCoordinateGetter } from '@dnd-kit/core'
import { pointerWithin, rectIntersection } from '@dnd-kit/core'

// Klavyeyle sürüklerken sağ/sol ok tuşu bir sonraki/önceki gün sütununa atlar.
export const columnKeyboardCoordinates: KeyboardCoordinateGetter = (
  event,
  { context: { droppableRects, droppableContainers, collisionRect }, currentCoordinates },
) => {
  if ((event.code !== 'ArrowLeft' && event.code !== 'ArrowRight') || !collisionRect) return undefined
  event.preventDefault()

  const columns = droppableContainers
    .getEnabled()
    .map((container) => droppableRects.get(container.id))
    .filter((rect): rect is NonNullable<typeof rect> => rect !== undefined)
    .sort((a, b) => a.left - b.left)
  if (columns.length === 0) return undefined

  const centerX = collisionRect.left + collisionRect.width / 2
  let current = 0
  let bestDistance = Infinity
  columns.forEach((rect, i) => {
    const distance = Math.abs(rect.left + rect.width / 2 - centerX)
    if (distance < bestDistance) {
      bestDistance = distance
      current = i
    }
  })

  const next = Math.min(columns.length - 1, Math.max(0, current + (event.code === 'ArrowRight' ? 1 : -1)))
  const target = columns[next]
  return { x: target.left + (target.width - collisionRect.width) / 2, y: currentCoordinates.y }
}

// İşaretçi ile: imlecin üstündeki sütun; klavyede (imleç yok): kesişen sütun.
export const columnCollision: CollisionDetection = (args) => {
  const pointer = pointerWithin(args)
  return pointer.length > 0 ? pointer : rectIntersection(args)
}
