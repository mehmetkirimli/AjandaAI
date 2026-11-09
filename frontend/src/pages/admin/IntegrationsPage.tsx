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
import { Spiral } from '../../components/illustrations'
import { NEXT_STEP, ROADMAP, STATUS_LINE, STATUS_ORDER, type RoadmapIcon, type RoadmapItem } from '../../lib/roadmap'
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

// Satır: 3 px durum çizgisi; eklenen/sıradaki düz, planlanan/fikir kesikli çizgi (henüz yazılmamış sayfa).
function StatusBadge({ status }: { status: RoadmapItem['status'] }) {
  return (
    <span className="rm-badge" style={{ borderLeftColor: STATUS_LINE[status] }}>
      {status}
    </span>
  )
}

function Row({ item, statusBadge = false }: { item: RoadmapItem; statusBadge?: boolean }) {
  const RowIcon = item.icon === 'claude' ? IconSparkles : ICONS[item.icon]
  return (
    <div className="rm-row" data-dashed={item.status === 'Planlandı' || item.status === 'Fikir'} style={{ borderLeftColor: STATUS_LINE[item.status] }}>
      <RowIcon size={20} stroke={1.7} aria-hidden style={{ flex: 'none' }} />
      <span style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <span style={{ fontSize: 14, fontWeight: 600 }}>{item.title}</span>
        <span style={{ fontSize: 12, color: 'var(--graphite-2)' }}>{item.description}</span>
      </span>
      {statusBadge ? (
        <StatusBadge status={item.status} />
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
          <span style={{ fontSize: 14, fontWeight: 600 }}>{item.title}</span>
          <span style={{ fontSize: 12, color: 'var(--graphite-2)' }}>{item.description}</span>
        </span>
        <StatusBadge status={item.status} />
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
            <span key={c.status} className="rm-count" style={{ borderLeftColor: STATUS_LINE[c.status] }}>
              {c.status} · {c.count}
            </span>
          ))}
        </div>
      </PanelHeader>
      <PageBody>
        <section className="rm-hero paper">
          <Spiral />
          <div className="rm-hero-icon" aria-hidden>
            <IconSparkles size={34} stroke={1.5} />
          </div>
          <div style={{ flex: '1 1 260px', display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
            <span className="hand muted" style={{ fontSize: 17 }}>{NEXT_STEP.label.toLocaleLowerCase('tr')}</span>
            <span style={{ fontFamily: 'var(--font-hand)', fontSize: 28, fontWeight: 700, lineHeight: 1.1 }}>{NEXT_STEP.title}</span>
            <span style={{ fontSize: 14, color: 'var(--graphite-2)' }}>{NEXT_STEP.description}</span>
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
              style={{ height: 10, border: '1.4px solid var(--graphite)', borderRadius: 3, overflow: 'hidden' }}
            >
              <div style={{ width: `${NEXT_STEP.progress}%`, height: '100%', background: 'var(--graphite)' }} />
            </div>
          </div>
        </section>

        <div className="rm-grid">
          <section className="paper rm-col">
            <Spiral />
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: STATUS_LINE.Eklendi }} />
              <span style={{ fontFamily: 'var(--font-hand)', fontSize: 21, fontWeight: 700 }}>Eklendi</span>
              <span className="hand muted" style={{ fontSize: 16, marginLeft: 'auto' }}>
                defterde hazır
              </span>
            </div>
            {features.map((r) => (
              <Row key={r.id} item={r} />
            ))}
          </section>

          <section className="paper rm-col">
            <Spiral />
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: STATUS_LINE.Planlandı }} />
              <span style={{ fontFamily: 'var(--font-hand)', fontSize: 21, fontWeight: 700 }}>Planlandı</span>
              <span className="hand muted" style={{ fontSize: 16, marginLeft: 'auto' }}>
                MCP araçları
              </span>
            </div>
            {mcp.map((r) => (
              <Row key={r.id} item={r} />
            ))}
          </section>

          <section className="paper rm-col">
            <Spiral />
            <div className="rm-col-head">
              <span className="rm-dot" style={{ background: STATUS_LINE.Sırada }} />
              <span style={{ fontFamily: 'var(--font-hand)', fontSize: 21, fontWeight: 700 }}>Yapay zeka modelleri</span>
            </div>
            {ai.map((r) => (r.tools ? <ModelCard key={r.id} item={r} /> : <Row key={r.id} item={r} statusBadge />))}
            <div style={{ flex: 1 }} />
            <span className="hand muted" style={{ fontSize: 17, textAlign: 'center' }}>
              her yeni özellik, defterine yeni bir sayfa
            </span>
          </section>
        </div>
      </PageBody>
    </>
  )
}
