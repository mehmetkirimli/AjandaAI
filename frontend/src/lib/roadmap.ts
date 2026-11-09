// Yönetim → Entegrasyonlar ve Yol Haritası: SABİT liste (ADR 0021 Karar 5e, veritabanı yok).
// Her aşama bitince lider docs/roadmap.md ile birlikte günceller.

export type RoadmapStatus = 'Eklendi' | 'Sırada' | 'Planlandı' | 'Fikir'

export type RoadmapIcon =
  | 'lock'
  | 'board'
  | 'calendar'
  | 'bell'
  | 'repeat'
  | 'shield'
  | 'map'
  | 'cloud'
  | 'calendarSync'
  | 'mail'
  | 'userPlus'
  | 'chart'
  | 'group'
  | 'claude'

export interface RoadmapItem {
  id: string
  title: string
  description: string
  status: RoadmapStatus
  // Sütun: özellikler, MCP araçları veya yapay zeka modelleri
  column: 'features' | 'mcp' | 'ai'
  icon: RoadmapIcon
  // Sağdaki küçük etiket (sürüm, aşama)
  tag?: string
  // Yalnızca model kartında: MCP araç adları
  tools?: string[]
}

export const NEXT_STEP = {
  label: 'SIRADAKİ BÜYÜK ADIM · v2.3b',
  title: "Hazır MCP'ler",
  description: 'MongoDB log analizi, hava durumu, harita, server-memory.',
  progressLabel: 'Planlanıyor',
  progress: 5,
}

export const ROADMAP: RoadmapItem[] = [
  { id: 'auth', title: 'Hesap ve güvenlik', description: 'JWT, e-posta doğrulama, roller', status: 'Eklendi', column: 'features', icon: 'lock', tag: 'v2.1' },
  { id: 'board', title: 'Haftalık Pano', description: 'Sürükle-bırak ile gün değiştir', status: 'Eklendi', column: 'features', icon: 'board', tag: 'F1' },
  { id: 'calendar', title: 'Takvim', description: 'Hafta / ay / gün görünümü', status: 'Eklendi', column: 'features', icon: 'calendar', tag: 'F2' },
  { id: 'activity', title: 'Aktivite ve hatırlatma', description: 'Ekle, düzenle, hatırlat', status: 'Eklendi', column: 'features', icon: 'bell', tag: 'F3' },
  { id: 'series', title: 'Tekrarlayan seriler', description: 'Tek formla 183 aktivite', status: 'Eklendi', column: 'features', icon: 'repeat', tag: 'F3b' },
  { id: 'admin', title: 'Yönetim paneli', description: 'Kullanıcı, kategori, moderasyon', status: 'Eklendi', column: 'features', icon: 'shield', tag: 'F4' },
  { id: 'mcp-server', title: 'MCP sunucusu', description: 'Claude Code ile çalışıyor: 5 araç', status: 'Eklendi', column: 'features', icon: 'claude', tag: 'v2.3' },

  { id: 'maps', title: 'Google Maps', description: 'Aktiviteye konum, yol süresi', status: 'Sırada', column: 'mcp', icon: 'map', tag: 'v2.3b' },
  { id: 'weather', title: 'Hava durumu', description: 'Dış mekân planında uyarı', status: 'Sırada', column: 'mcp', icon: 'cloud', tag: 'v2.3b' },
  { id: 'gcal', title: 'Google Calendar', description: 'İki yönlü senkron', status: 'Planlandı', column: 'mcp', icon: 'calendarSync', tag: 'MCP' },
  { id: 'email', title: 'E-posta bildirimleri', description: 'Hatırlatma gerçekten gönderilir', status: 'Planlandı', column: 'mcp', icon: 'mail', tag: 'v2.4' },
  { id: 'invite', title: 'Davet ve ortak aktivite', description: 'Arkadaşını plana ekle', status: 'Planlandı', column: 'mcp', icon: 'userPlus', tag: 'v2.5' },

  {
    id: 'claude',
    title: 'Claude',
    description: 'Desktop ve Code, MCP ile',
    status: 'Eklendi',
    column: 'ai',
    icon: 'claude',
    tools: ['aktivite_ekle', 'seri_ekle', 'haftayi_listele', 'bos_zaman_bul', 'kategorileri_listele'],
  },
  { id: 'analysis', title: 'Analiz ajanı', description: 'Haftanı puanlar, not düşer', status: 'Fikir', column: 'ai', icon: 'chart' },
  { id: 'grouping', title: 'Plan gruplama', description: 'Benzer planları birleştir', status: 'Fikir', column: 'ai', icon: 'group' },
]

export const STATUS_ORDER: RoadmapStatus[] = ['Eklendi', 'Sırada', 'Planlandı', 'Fikir']

export const STATUS_FILL: Record<RoadmapStatus, string> = {
  Eklendi: '#15803D',
  Sırada: '#0F766E',
  Planlandı: '#7C3AED',
  Fikir: '#C2410C',
}
