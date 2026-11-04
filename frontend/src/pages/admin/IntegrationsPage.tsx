import {
  IconBell,
  IconCalendar,
  IconCalendarCheck,
  IconChartBar,
  IconCloud,
  IconLayoutDashboard,
  IconLayoutGrid,
  IconLock,
  IconMail,
  IconMapPin,
  IconRepeat,
  IconShield,
  IconSparkles,
  IconUserPlus,
  type Icon,
} from '@tabler/icons-react'
import { PageBody, PanelHeader } from '../../components/PanelHeader'
import { NEXT_STEP, ROADMAP, STATUS_FILL, STATUS_ORDER, type RoadmapIcon, type RoadmapItem } from '../../lib/roadmap'
import './integrations.css'

const ICONS: Record<Exclude<RoadmapIcon, 'claude'>, Icon> = {
  lock: IconLock,
  board: IconLayoutDashboard,
  calendar: IconCalendar,
  bell: IconBell,
  repeat: IconRepeat,
  shield: IconShield,
  map: IconMapPin,
  cloud: IconCloud,
  calendarSync: IconCalendarCheck,
  mail: IconMail,
  userPlus: IconUserPlus,
  chart: IconChartBar,
  group: IconLayoutGrid,
}

// Satır görünümü duruma göre: Eklendi yeşil düz çerçeve; Planlandı mor kesikli; Fikir turuncu kesikli.
const ROW_STYLE: Record<string, { bg: string; border: string; icon: string }> = {
  Eklendi: { bg: 'rgba(21,128,61,0.25)', border: '1px solid rgba(74,222,128,0.3)', icon: '#86EFAC' },
  Planlandı: { bg: 'rgba(124,58,237,0.2)', border: '1px dashed rgba(167,139,250,0.5)', icon: '#C4B5FD' },
  Fikir: { bg: 'rgba(194,65,12,0.2)', border: '1px dashed rgba(251,146,60,0.5)', icon: '#FDBA74' },
  Sırada: { bg: 'rgba(15,118,110,0.25)', border: '1px solid rgba(94,234,212,0.4)', icon: '#5EEAD4' },
}

function Row({ item, statusBadge = false }: { item: RoadmapItem; statusBadge?: boolean }) {
  const s = ROW_STYLE[item.status]
  const RowIcon = item.icon === 'claude' ? IconSparkles : ICONS[item.icon]
  return (
    <div className="rm-row" style={{ background: s.bg, border: s.border }}>
      <RowIcon size={20} color={s.icon} aria-hidden style={{ flex: 'none' }} />
      <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span style={{ fontSize: 14, fontWeight: 700 }}>{item.title}</span>
        <span style={{ fontSize: 12 }}>{item.description}</span>
      </span>
      {statusBadge ? (
        <span className="rm-badge" style={{ background: STATUS_FILL[item.status] }}>
          {item.status}
        </span>
      ) : (
        item.tag && (
          <span className="mono" style={{ fontSize: 11, flex: 'none' }}>
            {item.tag}
          </span>
        )
      )}
    </div>
  )
}

function ModelCard({ item }: { item: RoadmapItem }) {
  return (
    <div className="rm-model">
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <span className="rm-model-mark" aria-hidden>
          {item.title.charAt(0)}
        </span>
        <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
          <span style={{ fontSize: 15, fontWeight: 700 }}>{item.title}</span>
          <span style={{ fontSize: 12 }}>{item.description}</span>
        </span>
        <span className="rm-badge" style={{ background: STATUS_FILL[item.status] }}>
          {item.status}
        </span>
      </div>
      {item.tools && (
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
          {item.tools.map((t) => (
            <span key={t} className="mono rm-tool">
              {t}
            </span>
          ))}
        </div>
      )}
    </div>
  )
}

// Yalnızca Admin (AdminRoute). İçerik lib/roadmap.ts'ten; backend çağrısı yok.
export function IntegrationsPage() {
  const counts = STATUS_ORDER.map((s) => ({ status: s, count: ROADMAP.filter((r) => r.status === s).length }))
  const features = ROADMAP.filter((r) => r.column === 'features')
  const mcp = ROADMAP.filter((r) => r.column === 'mcp')
  const ai = ROADMAP.filter((r) => r.column === 'ai')

  return (
    <>
      <PanelHeader title="Entegrasyonlar ve Yol Haritası" kicker="nereden geldik, nereye gidiyoruz">
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', fontSize: 13, fontWeight: 600 }}>
          {counts.map((c) => (
            <span key={c.status} className="rm-count" style={{ background: STATUS_FILL[c.status] }}>
              {c.status} · {c.count}
            </span>
          ))}
        </div>
      </PanelHeader>
      <PageBody>
        <section className="rm-hero">
          <div className="rm-hero-icon" aria-hidden>
            <IconSparkles size={40} stroke={1.8} color="#fff" />
          </div>
          <div style={{ flex: '1 1 260px', display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
            <span style={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.08em' }}>{NEXT_STEP.label}</span>
            <span style={{ fontSize: 24, fontWeight: 800, lineHeight: 1.2 }}>{NEXT_STEP.title}</span>
            <span style={{ fontSize: 15 }}>{NEXT_STEP.description}</span>
          </div>
          <div style={{ width: 180, display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span style={{ fontSize: 13, display: 'flex', justifyContent: 'space-between', gap: 8 }}>
              İlerleme<span className="mono">{NEXT_STEP.progressLabel}</span>
            </span>
            <div
              role="progressbar"
              aria-label="MCP sunucusu ilerlemesi"
              aria-valuenow={NEXT_STEP.progress}
              aria-valuemin={0}
              aria-valuemax={100}
              style={{ height: 10, borderRadius: 5, background: 'rgba(255,255,255,0.25)', overflow: 'hidden' }}
            >
              <div style={{ width: `${NEXT_STEP.progress}%`, height: '100%', background: '#fff' }} />
            </div>
          </div>
        </section>

        <div className="rm-grid">
          <section className="ink-card rm-col">
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: '#4ADE80' }} />
              <span style={{ fontSize: 17, fontWeight: 700 }}>Eklendi</span>
              <span className="hand" style={{ fontSize: 19, marginLeft: 'auto' }}>
                defterde hazır
              </span>
            </div>
            {features.map((r) => (
              <Row key={r.id} item={r} />
            ))}
          </section>

          <section className="ink-card rm-col">
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: '#A78BFA' }} />
              <span style={{ fontSize: 17, fontWeight: 700 }}>Planlandı</span>
              <span className="hand" style={{ fontSize: 19, marginLeft: 'auto' }}>
                MCP araçları
              </span>
            </div>
            {mcp.map((r) => (
              <Row key={r.id} item={r} />
            ))}
          </section>

          <section className="ink-card rm-col">
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: '#2DD4BF' }} />
              <span style={{ fontSize: 17, fontWeight: 700 }}>Yapay zeka modelleri</span>
            </div>
            {ai.map((r) => (r.tools ? <ModelCard key={r.id} item={r} /> : <Row key={r.id} item={r} statusBadge />))}
            <div style={{ flex: 1 }} />
            <span className="hand" style={{ fontSize: 20, textAlign: 'center' }}>
              her yeni özellik, defterine yeni bir sayfa
            </span>
          </section>
        </div>
      </PageBody>
    </>
  )
}
