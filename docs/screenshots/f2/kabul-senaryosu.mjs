// F2 ekran kabulü: takvim hafta görünümü, sürükle (gün+saat), süre uzat, detay paneli, ay görünümü, koyu tema, Pano senkronu.
import { chromium } from 'playwright'
import fs from 'node:fs'

const API = process.env.API, WEB = process.env.WEB, LOG = process.env.API_LOG, OUT = process.env.OUT
const email = `demo${Date.now()}@ajanda.dev`, password = 'mavi-kayik-sahilde-77'
const shot = (p, n) => p.screenshot({ path: `${OUT}/${n}.png` })
const api = async (path, opts = {}) => {
  const r = await fetch(API + path, { ...opts, headers: { 'Content-Type': 'application/json', ...(opts.headers ?? {}) } })
  return { status: r.status, body: await r.json().catch(() => null) }
}
const browser = await chromium.launch({ channel: 'chrome' })
const page = await browser.newPage({ viewport: { width: 1536, height: 864 }, locale: 'tr-TR', timezoneId: 'Europe/Istanbul' })
const errors = []
page.on('console', m => m.type() === 'error' && errors.push(m.text()))

// Hesap (API ile hızlı): kayıt + log'daki link + giriş
await api('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, displayName: 'Mehmet Demo', timeZoneId: 'Europe/Istanbul', password }) })
let token
for (let i = 0; i < 20 && !token; i++) {
  await new Promise(r => setTimeout(r, 400))
  token = [...fs.readFileSync(LOG, 'utf8').matchAll(/verify-email\?token=([\w%-]+)/g)].pop()?.[1]
}
await api('/api/auth/verify-email', { method: 'POST', body: JSON.stringify({ token: decodeURIComponent(token) }) })
const login = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
const auth = { Authorization: `Bearer ${login.body.data.accessToken}` }
const cats = (await api('/api/categories?pageSize=100', { headers: auth })).body.data.items
const monday = new Date('2026-10-05T00:00:00+03:00')
const at = (d, h) => new Date(monday.getTime() + (d * 24 + h) * 3600000)
const plan = [[0, 9, 1, 'Sabah koşusu', 'High'], [0, 14, 2, 'Proje toplantısı', 'Medium'], [1, 19, 2, 'Kitap kulübü', 'Low'],
  [2, 8, 1, 'Diş hekimi', 'High'], [3, 18, 2, 'Akşam yemeği - Ayşe', 'Medium'], [4, 10, 1, 'Haftalık planlama', 'Low'],
  [5, 11, 2, 'Kahvaltı hazırlığı', 'Medium'], [5, 16, 3, 'Sinema', 'Low'], [6, 12, 1.5, 'Pazar yürüyüşü', 'Medium']]
for (const [d, h, dur, title, priority] of plan) {
  const dentist = title === 'Diş hekimi'
  await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({
    categoryId: cats[(d * 3 + h) % cats.length].id, title, description: dentist ? 'Yıllık kontrol, röntgen de çekilecek.' : '',
    status: 'Planned', priority, energyLevel: 'Medium', start: at(d, h).toISOString(), end: at(d, h + dur).toISOString(),
    isAllDay: false, location: dentist ? 'Kadıköy Ağız Diş Sağlığı' : null, isFlexible: false, estimatedBudget: dentist ? 1500 : 0 }) })
}

// UI giriş → Takvim
await page.goto(WEB + '/login')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')
await page.getByRole('link', { name: 'Takvim' }).first().click()
await page.waitForURL('**/calendar')
await page.locator('.fc-event', { hasText: 'Diş hekimi' }).waitFor()
await page.waitForTimeout(700)
await shot(page, '01-hafta')

const find = async t => (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items.find(a => a.title === t)

// Sürükle: Diş hekimi Çar 08:00 → Per 10:00
const ev = page.locator('.fc-timegrid-event', { hasText: 'Diş hekimi' }).first()
const eb = await ev.boundingBox()
const col = await page.locator('.fc-timegrid-col[data-date="2026-10-08"]').boundingBox()
const slot = await page.locator('.fc-timegrid-slot-lane[data-time="10:00:00"]').first().boundingBox()
await page.mouse.move(eb.x + eb.width / 2, eb.y + 6)
await page.mouse.down()
await page.mouse.move(eb.x + eb.width / 2 + 10, eb.y + 20, { steps: 4 })
await page.mouse.move(col.x + col.width / 2, slot.y + 6, { steps: 20 })
await page.waitForTimeout(300)
await shot(page, '02-surukleniyor')
await page.mouse.up()
await page.waitForTimeout(1500)
await shot(page, '03-birakildi')
const afterDrop = await find('Diş hekimi')

// Süre uzat: alt kenardan 2 slot (30 dk) aşağı
const ev2 = page.locator('.fc-timegrid-event', { hasText: 'Diş hekimi' }).first()
await ev2.hover()
const b2 = await ev2.boundingBox()
const slotH = (await page.locator('.fc-timegrid-slot-lane').nth(1).boundingBox()).height
await page.mouse.move(b2.x + b2.width / 2, b2.y + b2.height - 3)
await page.mouse.down()
await page.mouse.move(b2.x + b2.width / 2, b2.y + b2.height - 3 + slotH * 2 + 2, { steps: 12 })
await page.mouse.up()
await page.waitForTimeout(1500)
await shot(page, '04-sure-uzatildi')
const afterResize = await find('Diş hekimi')

// Detay paneli
await page.locator('.fc-timegrid-event', { hasText: 'Diş hekimi' }).first().click()
await page.waitForTimeout(900)
await shot(page, '05-detay')
await page.keyboard.press('Escape')
await page.waitForTimeout(400)

// Ay görünümü + koyu tema
await page.getByText('Ay', { exact: true }).first().click()
await page.waitForTimeout(800)
await shot(page, '06-ay')
await page.getByText('Hafta', { exact: true }).first().click()
await page.waitForTimeout(400)
await page.getByRole('button', { name: /tema|koyu|dark/i }).first().click().catch(() => {})
await page.waitForTimeout(500)
await shot(page, '07-koyu-hafta')
await page.getByRole('button', { name: /tema|koyu|dark|açık|light/i }).first().click().catch(() => {})

// Pano senkronu
await page.getByRole('link', { name: 'Pano' }).first().click()
await page.waitForURL(WEB + '/')
await page.waitForTimeout(800)
await shot(page, '08-pano-senkron')
await page.getByText('Diş hekimi').first().click()
await page.waitForTimeout(800)
await shot(page, '09-pano-kart-detay')

console.log(JSON.stringify({ afterDrop: [afterDrop?.start, afterDrop?.end], afterResize: [afterResize?.start, afterResize?.end], consoleErrors: errors }, null, 2))
await browser.close()
