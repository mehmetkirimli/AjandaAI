// Kategori id'sinden tutarlı renk: aynı kategori her yerde aynı rengi alır.
// Vurgu rengiyle (accent: indigo) karışmasın diye indigo ve kırmızı yok.
const PALETTE = ['blue', 'violet', 'green', 'teal', 'grape', 'cyan', 'orange', 'pink', 'lime', 'yellow']

export function categoryColor(categoryId: number): string {
  return PALETTE[Math.abs(categoryId) % PALETTE.length]
}

export function categoryStrip(categoryId: number): string {
  return `var(--mantine-color-${categoryColor(categoryId)}-filled)`
}

export const UNCATEGORIZED = 'Kategorisiz'
