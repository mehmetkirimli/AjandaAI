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

// "Özel Defter" paleti (ADR 0021 Karar 5e): dolgu koyu ton (üstünde beyaz metin), canlı ton yalnızca ikon/parlama.
export type Tone = 'green' | 'teal' | 'violet' | 'orange'

export interface ToneStyle {
  // Dolgu (beyaz metin okunur)
  fill: string
  // Kart gradyanı
  gradient: string
  // Canlı ton: ikon, çerçeve, parlama
  vivid: string
  // Açık ikon tonu (koyu zemin üstünde)
  light: string
  // Kart gölgesi
  shadow: string
}

export const TONES: Record<Tone, ToneStyle> = {
  green: {
    fill: '#15803D',
    gradient: 'linear-gradient(135deg, #15803D, #0F766E)',
    vivid: '#4ADE80',
    light: '#86EFAC',
    shadow: '0 6px 16px rgba(21,128,61,0.35)',
  },
  teal: {
    fill: '#0F766E',
    gradient: 'linear-gradient(135deg, #0F766E, #0E7490)',
    vivid: '#2DD4BF',
    light: '#5EEAD4',
    shadow: '0 6px 16px rgba(15,118,110,0.35)',
  },
  violet: {
    fill: '#7C3AED',
    gradient: 'linear-gradient(135deg, #7C3AED, #6D28D9)',
    vivid: '#A78BFA',
    light: '#C4B5FD',
    shadow: '0 6px 16px rgba(124,58,237,0.35)',
  },
  orange: {
    fill: '#C2410C',
    gradient: 'linear-gradient(135deg, #C2410C, #9A3412)',
    vivid: '#FB923C',
    light: '#FDBA74',
    shadow: '0 6px 16px rgba(194,65,12,0.4)',
  },
}

// Bilinmeyen kategoriler bu sırayla döner.
const ROTATION: Tone[] = ['green', 'teal', 'violet', 'orange']

// İptal edilen / kategorisiz aktiviteler için nötr görünüm.
export const NEUTRAL: ToneStyle = {
  fill: '#3a3156',
  gradient: 'linear-gradient(135deg, #3a3156, #2a2244)',
  vivid: '#b9b2cf',
  light: '#d6d0e8',
  shadow: '0 6px 16px rgba(0,0,0,0.3)',
}

// Ada göre ikon (ve varsa renk). Eşleşme kelime başıyla yapılır ("kişisel" içinde "iş" eşleşmez).
// İlk eşleşen kural kazanır; sıra önemlidir.
const RULES: { keys: string[]; icon: Icon; tone?: Tone }[] = [
  { keys: ['spor', 'fitness', 'koşu', 'antrenman', 'hareket'], icon: IconBarbell, tone: 'green' },
  { keys: ['seyahat', 'gezi', 'tatil', 'yolculuk'], icon: IconPlane, tone: 'teal' },
  { keys: ['iş', 'kariyer', 'finans', 'toplantı', 'proje', 'ofis'], icon: IconBriefcase, tone: 'violet' },
  { keys: ['yeme', 'içme', 'yemek', 'restoran', 'kahvaltı'], icon: IconToolsKitchen2, tone: 'orange' },
  { keys: ['kitap', 'okuma', 'gelişim', 'eğitim', 'ders', 'kurs'], icon: IconBook2, tone: 'teal' },
  { keys: ['romantizm', 'ilişki', 'aşk'], icon: IconHeart, tone: 'orange' },
  { keys: ['kültür', 'sanat', 'gösteri', 'tiyatro', 'sinema'], icon: IconMasksTheater, tone: 'violet' },
  { keys: ['eğlence', 'sosyal', 'aile', 'arkadaş', 'parti'], icon: IconConfetti, tone: 'orange' },
  { keys: ['dinlenme', 'zihinsel', 'meditasyon', 'uyku'], icon: IconLeaf, tone: 'teal' },
  { keys: ['bakım', 'rutin'], icon: IconSparkles, tone: 'green' },
  { keys: ['sağlık', 'doktor', 'hastane'], icon: IconHeartbeat, tone: 'green' },
  { keys: ['hobi', 'resim'], icon: IconPalette, tone: 'violet' },
  { keys: ['müzik', 'konser'], icon: IconMusic, tone: 'violet' },
  { keys: ['alışveriş', 'market'], icon: IconShoppingBag, tone: 'orange' },
  { keys: ['topluluk', 'grup'], icon: IconUsers, tone: 'teal' },
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

// Kategori → renk, gradyan, ikon: tek kaynak.
export function categoryLook(categoryId: number, name?: string, neutral = false): CategoryLook {
  const icon = findRule(name)?.icon ?? IconNotebook
  if (neutral) return { ...NEUTRAL, tone: 'neutral', icon }
  const tone = categoryTone(categoryId, name)
  return { ...TONES[tone], tone, icon }
}

// Geriye dönük: kategori dolgu rengi (nokta, şerit).
export function categoryStrip(categoryId: number, name?: string): string {
  return TONES[categoryTone(categoryId, name)].fill
}

export const UNCATEGORIZED = 'Kategorisiz'
