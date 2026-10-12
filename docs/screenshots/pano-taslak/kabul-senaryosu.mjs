// Kullanıcının Pano taslağı kabulü: iki sıra (hafta içi / hafta sonu), dolu renkli haplar, boş gün mesajları,
// "+ Aktivite Ekle", sürükle-bırak (boş hafta içi gününe ve hafta sonuna), drawer, açık tema, dar ekran.
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

await api('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, displayName: 'Mehmet Hilmi', timeZoneId: 'Europe/Istanbul', password }) })
let token
for (let i = 0; i < 20 && !token; i++) {
  await new Promise(r => setTimeout(r, 400))
  token = [...fs.readFileSync(LOG, 'utf8').matchAll(/verify-email\?token=([\w%-]+)/g)].pop()?.[1]
}
await api('/api/auth/verify-email', { method: 'POST', body: JSON.stringify({ token: decodeURIComponent(token) }) })
const login = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
const auth = { Authorization: `Bearer ${login.body.data.accessToken}` }
const cats = (await api('/api/categories?pageSize=100', { headers: auth })).body.data.items

// Bu hafta: 12–18 Ekim 2026 (Pzt). Taslaktaki aktiviteler.
const monday = new Date('2026-10-12T00:00:00+03:00')
const at = (d, h) => new Date(monday.getTime() + (d * 24 + h) * 3600000)
const cat = i => cats[i % cats.length].id
const plan = [
  [0, 9.5, 1, 'Yapay Zeka Sunumu', 0, false], [0, 11, 1, 'Ekip Buluşması', 0, false],
  [1, 9.5, 1, 'Yapay Zeka Sunumu', 3, false],
  [2, 11, 1, 'Ekip Toplantısı', 0, false], [2, 14, 1, 'Kod Gözden Geçirme', 6, false],
  [4, 14, 1, 'Kod Gözden Geçirme', 6, false],
  [5, 0, 24, 'Hafta sonu dinlenmesi', 8, true]
]
for (const [d, h, dur, title, c, allDay] of plan) {
  const r = await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({
    categoryId: cat(c), title, description: '', status: 'Planned', priority: 'Medium', energyLevel: 'Medium',
    start: at(d, h).toISOString(), end: at(d, h + dur).toISOString(), isAllDay: allDay, location: null, isFlexible: false, estimatedBudget: 0 }) })
  if (r.status !== 201) throw new Error(`${title}: ${r.status}`)
}

await page.goto(WEB + '/login')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')
await page.getByText(/Ekip Buluşması/).first().waitFor()
await page.waitForTimeout(800)
await shot(page, '01-pano')

const find = async t => (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items.filter(a => a.title === t)
const drag = async (text, targetHeader, name) => {
  const card = page.getByText(text).first()
  const from = await card.boundingBox()
  const to = await page.getByText(targetHeader, { exact: true }).boundingBox()
  await page.mouse.move(from.x + 30, from.y + from.height / 2); await page.mouse.down()
  await page.mouse.move(from.x + 50, from.y + 30, { steps: 5 })
  await page.mouse.move(to.x + 60, to.y + 120, { steps: 20 })
  await page.waitForTimeout(300); if (name) await shot(page, name)
  await page.mouse.up(); await page.waitForTimeout(1500)
}
// Çar "Ekip Toplantısı" → boş Perşembe
await drag(/Ekip Toplantısı/, '15 Per', '02-surukleniyor')
await shot(page, '03-persembeye-tasindi')
const toThu = (await find('Ekip Toplantısı'))[0]?.start
// Cum "Kod Gözden Geçirme" → Pazar (hafta sonu)
await drag(/Kod Gözden Geçirme.*14:00/, '18 Paz')
const kod = (await find('Kod Gözden Geçirme')).map(a => a.start).sort()
await shot(page, '04-pazara-tasindi')

// Drawer (karta tıklama)
await page.getByText(/Ekip Buluşması/).first().click(); await page.waitForTimeout(900); await shot(page, '05-detay')
await page.keyboard.press('Escape'); await page.waitForTimeout(300)
// + Aktivite Ekle
await page.getByRole('button', { name: /Aktivite Ekle/ }).first().click(); await page.waitForTimeout(500); await shot(page, '06-aktivite-ekle')

// Açık tema
await page.keyboard.press('Control+K'); await page.waitForTimeout(300)
await page.keyboard.type('tema'); await page.waitForTimeout(300); await page.keyboard.press('Enter'); await page.waitForTimeout(600)
await shot(page, '07-acik-tema')
await page.keyboard.press('Control+K'); await page.waitForTimeout(300)
await page.keyboard.type('tema'); await page.waitForTimeout(300); await page.keyboard.press('Enter'); await page.waitForTimeout(300)

// Dar ekran
await page.setViewportSize({ width: 820, height: 900 }); await page.waitForTimeout(700); await shot(page, '08-dar-ekran')

console.log(JSON.stringify({ toThu, kod, consoleErrors: errors }, null, 2))
await browser.close()
