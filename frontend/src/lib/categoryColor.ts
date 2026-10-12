// Kategori id'sinden tutarlı renk: aynı kategori her yerde aynı rengi alır.
// Vurgu rengiyle (accent: indigo) karışmasın diye indigo ve kırmızı yok.
const PALETTE = ['blue', 'teal', 'grape', 'cyan', 'lime', 'orange', 'violet', 'yellow', 'green', 'pink']

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
