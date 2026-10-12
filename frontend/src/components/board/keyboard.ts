import type { CollisionDetection, KeyboardCoordinateGetter } from '@dnd-kit/core'
import { pointerWithin, rectIntersection } from '@dnd-kit/core'

// Klavyeyle sürüklerken sağ/sol ok tuşu okuma sırasına göre (üst sıra, sonra alt sıra) bir sonraki/önceki gün kartına atlar.
export const columnKeyboardCoordinates: KeyboardCoordinateGetter = (
  event,
  { context: { droppableRects, droppableContainers, collisionRect } },
) => {
  if ((event.code !== 'ArrowLeft' && event.code !== 'ArrowRight') || !collisionRect) return undefined
  event.preventDefault()

  const cards = droppableContainers
    .getEnabled()
    .map((container) => droppableRects.get(container.id))
    .filter((rect): rect is NonNullable<typeof rect> => rect !== undefined)
    .sort((a, b) => (Math.abs(a.top - b.top) > 20 ? a.top - b.top : a.left - b.left))
  if (cards.length === 0) return undefined

  const centerX = collisionRect.left + collisionRect.width / 2
  const centerY = collisionRect.top + collisionRect.height / 2
  let current = 0
  let bestDistance = Infinity
  cards.forEach((rect, i) => {
    const distance = Math.hypot(rect.left + rect.width / 2 - centerX, rect.top + rect.height / 2 - centerY)
    if (distance < bestDistance) {
      bestDistance = distance
      current = i
    }
  })

  const next = Math.min(cards.length - 1, Math.max(0, current + (event.code === 'ArrowRight' ? 1 : -1)))
  const target = cards[next]
  return { x: target.left + (target.width - collisionRect.width) / 2, y: target.top + 40 }
}

// İşaretçi ile: imlecin üstündeki kart; klavyede (imleç yok): kesişen kart.
export const columnCollision: CollisionDetection = (args) => {
  const pointer = pointerWithin(args)
  return pointer.length > 0 ? pointer : rectIntersection(args)
}
