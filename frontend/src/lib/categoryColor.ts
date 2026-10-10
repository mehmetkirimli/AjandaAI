// Kategori id'sinden tutarlı renk: aynı kategori her yerde aynı rengi alır.
const PALETTE = ['indigo', 'teal', 'grape', 'orange', 'pink', 'cyan', 'lime', 'violet', 'red', 'blue', 'yellow', 'green']

export function categoryColor(categoryId: number): string {
  return PALETTE[Math.abs(categoryId) % PALETTE.length]
}
