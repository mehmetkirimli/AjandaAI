// Komut paleti ile sayfalar (Pano/Takvim) arasında küçük bir köprü.
// Sayfa o anda açık değilse komut beklemeye alınır ve sayfa açılınca çalıştırılır.

export type PageCommand = 'today' | 'prev' | 'next' | 'view:timeGridWeek' | 'view:dayGridMonth' | 'view:timeGridDay'

type Handler = (command: PageCommand) => void

let handler: Handler | null = null
let pending: PageCommand | null = null

export function registerCommandHandler(fn: Handler): () => void {
  handler = fn
  if (pending) {
    const command = pending
    pending = null
    fn(command)
  }
  return () => {
    if (handler === fn) handler = null
  }
}

export function runPageCommand(
  command: PageCommand,
  pathname: string,
  targetPath: string,
  navigate: (to: string) => void,
): void {
  if (pathname === targetPath && handler) {
    handler(command)
    return
  }
  pending = command
  navigate(targetPath)
}

export const IS_MAC = typeof navigator !== 'undefined' && /mac|iphone|ipad/i.test(navigator.platform)
export const MOD_KEY = IS_MAC ? '⌘' : 'Ctrl'
