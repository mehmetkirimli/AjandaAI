export function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Istanbul'
  } catch {
    return 'Europe/Istanbul'
  }
}

export function timeZoneOptions(current: string): string[] {
  let zones: string[] = []
  try {
    zones = Intl.supportedValuesOf('timeZone')
  } catch {
    zones = []
  }
  const set = new Set<string>(['Europe/Istanbul', ...zones])
  set.add(current)
  return [...set].sort()
}
