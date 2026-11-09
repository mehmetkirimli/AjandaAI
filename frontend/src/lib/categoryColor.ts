import {
  IconBarbell,
  IconBook2,
  IconBriefcase,
  IconConfetti,
  IconHeart,
  IconHeartbeat,
  IconLeaf,
  IconMasksTheater,
  IconMusic,
  IconNotebook,
  IconPalette,
  IconPlane,
  IconShoppingBag,
  IconSparkles,
  IconToolsKitchen2,
  IconUsers,
  type Icon,
} from '@tabler/icons-react'


// "Eskiz Defter" paleti (ADR 0021 Karar 5f): kategori rengi yalnızca 3 px sol çizgide, solgun tonlarda.
// Kart dolgusu kategori renginde değildir; metin ve ikon grafittir.
export type Tone = 'sage' | 'petrol' | 'lavender' | 'terra'

export interface ToneStyle {
  // Sol çizgi / nokta rengi
  line: string
}

export const TONES: Record<Tone, ToneStyle> = {
  sage: { line: '#6f8f6a' },
  petrol: { line: '#5e8a8a' },
  lavender: { line: '#7d7aa6' },
  terra: { line: '#b07a5a' },
}

// Bilinmeyen kategoriler bu sırayla döner.
const ROTATION: Tone[] = ['sage', 'petrol', 'lavender', 'terra']

// İptal edilen / kategorisiz aktiviteler için nötr görünüm (kesikli gri çizgi).
export const NEUTRAL: ToneStyle = { line: '#bdbcb6' }

// Ada göre ikon (ve varsa renk). Eşleşme kelime başıyla yapılır ("kişisel" içinde "iş" eşleşmez).
// İlk eşleşen kural kazanır; sıra önemlidir.
const RULES: { keys: string[]; icon: Icon; tone?: Tone }[] = [
  { keys: ['spor', 'fitness', 'koşu', 'antrenman', 'hareket'], icon: IconBarbell, tone: 'sage' },
  { keys: ['seyahat', 'gezi', 'tatil', 'yolculuk'], icon: IconPlane, tone: 'petrol' },
  { keys: ['iş', 'kariyer', 'finans', 'toplantı', 'proje', 'ofis'], icon: IconBriefcase, tone: 'lavender' },
  { keys: ['yeme', 'içme', 'yemek', 'restoran', 'kahvaltı'], icon: IconToolsKitchen2, tone: 'terra' },
  { keys: ['kitap', 'okuma', 'gelişim', 'eğitim', 'ders', 'kurs'], icon: IconBook2, tone: 'petrol' },
  { keys: ['romantizm', 'ilişki', 'aşk'], icon: IconHeart, tone: 'terra' },
  { keys: ['kültür', 'sanat', 'gösteri', 'tiyatro', 'sinema'], icon: IconMasksTheater, tone: 'lavender' },
  { keys: ['eğlence', 'sosyal', 'aile', 'arkadaş', 'parti'], icon: IconConfetti, tone: 'terra' },
  { keys: ['dinlenme', 'zihinsel', 'meditasyon', 'uyku'], icon: IconLeaf, tone: 'petrol' },
  { keys: ['bakım', 'rutin'], icon: IconSparkles, tone: 'sage' },
  { keys: ['sağlık', 'doktor', 'hastane'], icon: IconHeartbeat, tone: 'sage' },
  { keys: ['hobi', 'resim'], icon: IconPalette, tone: 'lavender' },
  { keys: ['müzik', 'konser'], icon: IconMusic, tone: 'lavender' },
  { keys: ['alışveriş', 'market'], icon: IconShoppingBag, tone: 'terra' },
  { keys: ['topluluk', 'grup'], icon: IconUsers, tone: 'petrol' },
]

export interface CategoryLook extends ToneStyle {
  tone: Tone | 'neutral'
  icon: Icon
}

function findRule(name?: string) {
  const words = (name ?? '').toLocaleLowerCase('tr').split(/[^\p{L}]+/u).filter(Boolean)
  return RULES.find((r) => r.keys.some((k) => words.some((w) => w.startsWith(k))))
}

export function categoryTone(categoryId: number, name?: string): Tone {
  return findRule(name)?.tone ?? ROTATION[Math.abs(categoryId) % ROTATION.length]
}

// Kategori → çizgi rengi, ikon: tek kaynak.
export function categoryLook(categoryId: number, name?: string, neutral = false): CategoryLook {
  const icon = findRule(name)?.icon ?? IconNotebook
  if (neutral) return { ...NEUTRAL, tone: 'neutral', icon }
  const tone = categoryTone(categoryId, name)
  return { ...TONES[tone], tone, icon }
}

// Kategori çizgi rengi (nokta, şerit).
export function categoryStrip(categoryId: number, name?: string): string {
  return TONES[categoryTone(categoryId, name)].line
}

export const UNCATEGORIZED = 'Kategorisiz'
