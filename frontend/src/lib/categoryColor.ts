// Kategori id'sinden tutarlı renk: aynı kategori her yerde aynı rengi alır.
// Vurgu rengiyle (accent: indigo) karışmasın diye indigo ve kırmızı yok.
const PALETTE = ['blue', 'violet', 'green', 'teal', 'grape', 'cyan', 'orange', 'pink', 'lime', 'yellow']

export function categoryColor(categoryId: number): string {
  return PALETTE[Math.abs(categoryId) % PALETTE.length]
}

// Koyu yüzey üstünde yumuşak ton: dolgu rengi %16 opaklıkla yüzeyle karışır (Pano kartı ve takvim olayı ortak).
export function categoryTint(categoryId: number, percent = 16): string {
  return `color-mix(in srgb, var(--mantine-color-${categoryColor(categoryId)}-filled) ${percent}%, var(--app-surface))`
}

export function categoryStrip(categoryId: number): string {
  return `var(--mantine-color-${categoryColor(categoryId)}-filled)`
}

export const UNCATEGORIZED = 'Kategorisiz'

// Parlak dolgu renklerinde koyu, diğerlerinde beyaz yazı (okunurluk).
const LIGHT_FILLS = new Set(['yellow', 'lime', 'cyan', 'orange'])
export function categoryTextColor(categoryId: number): string {
  return LIGHT_FILLS.has(categoryColor(categoryId)) ? '#1a1b1e' : '#ffffff'
}

// Pano hapı: derin, doygun ton (shade 7); tüm renklerde beyaz metin okunur.
export function categoryFill(categoryId: number): string {
  return `var(--mantine-color-${categoryColor(categoryId)}-7)`
}
