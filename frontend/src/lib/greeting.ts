// Saate göre el yazısı karşılama: "Günaydın Mehmet,".
export function greeting(displayName: string | undefined, now = new Date()): string {
  const h = now.getHours()
  const word = h >= 5 && h < 12 ? 'Günaydın' : h >= 12 && h < 18 ? 'İyi günler' : h >= 18 && h < 22 ? 'İyi akşamlar' : 'İyi geceler'
  const first = displayName?.trim().split(/\s+/)[0]
  return first ? `${word} ${first},` : `${word},`
}
