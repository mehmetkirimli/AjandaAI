// "Cyber-Executive" tasarım kabulü: navbar, 5+2 pano, yarı saydam kartlar, dropzone (tıkla → form),
// pano sürükle-bırak (boş güne + hafta sonuna), navbar Hafta/Ay/Gün, takvim + sürükle, drawer, açık tema.
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
const result = {}

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
const monday = new Date('2026-10-12T00:00:00+03:00')
const at = (d, h) => new Date(monday.getTime() + (d * 24 + h) * 3600000)
const plan = [
  [0, 9.5, 1, 'Yapay Zeka Sunumu', 0, false, 'High'], [0, 11, 1, 'Ekip Buluşması', 1, false, 'Medium'],
  [1, 9.5, 1, 'Yapay Zeka Sunumu', 2, false, 'Medium'],
  [2, 11, 1, 'Ekip Toplantısı', 3, false, 'Medium'], [2, 14, 1, 'Kod Gözden Geçirme', 4, false, 'Low'],
  [4, 14, 1, 'Kod Gözden Geçirme', 4, false, 'Low'],
  [5, 0, 24, 'Hafta sonu dinlenmesi', 5, true, 'Low']
]
for (const [d, h, dur, title, c, allDay, priority] of plan) {
  await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({
    categoryId: cats[c % cats.length].id, title, description: '', status: 'Planned', priority, energyLevel: 'Medium',
    start: at(d, h).toISOString(), end: at(d, h + dur).toISOString(), isAllDay: allDay, location: null, isFlexible: false, estimatedBudget: 0 }) })
}
const list = async () => (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items

await page.goto(WEB + '/login'); await page.waitForLoadState('networkidle'); await shot(page, '00-giris')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')
await page.getByText('Ekip Buluşması').first().waitFor()
await page.waitForTimeout(800)
await shot(page, '01-pano')

// Sürükle: Çar "Ekip Toplantısı" → boş Perşembe dropzone
const drag = async (text, targetHeader, name) => {
  const from = await page.getByText(text).first().boundingBox()
  const to = await page.getByText(targetHeader, { exact: true }).boundingBox()
  await page.mouse.move(from.x + 10, from.y + from.height / 2); await page.mouse.down()
  await page.mouse.move(from.x + 30, from.y + 25, { steps: 5 })
  await page.mouse.move(to.x + 60, to.y + 90, { steps: 20 })
  await page.waitForTimeout(300); if (name) await shot(page, name)
  await page.mouse.up(); await page.waitForTimeout(1500)
}
await drag('Ekip Toplantısı', '15 Per', '02-surukleniyor')
result.toThu = (await list()).find(a => a.title === 'Ekip Toplantısı')?.start
await drag('Hafta sonu dinlenmesi', '18 Paz')
result.allDayToSun = (await list()).find(a => a.title === 'Hafta sonu dinlenmesi')?.start
await shot(page, '03-tasindi')

// Dropzone'a tıkla → o günün formu (Çarşamba artık sadece Kod Gözden Geçirme; Salı dolu; Cumartesi boş)
await page.getByText('Boş · eklemek için tıkla').first().click()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.waitForTimeout(400)
await shot(page, '04-dropzone-form')
result.formDate = await page.getByLabel('Başlangıç tarihi').first().inputValue().catch(() => null) ?? await page.getByRole('button', { name: 'Başlangıç tarihi' }).first().textContent().catch(() => null)
await page.keyboard.press('Escape'); await page.waitForTimeout(300)

// Drawer
await page.getByText('Ekip Buluşması').first().click(); await page.waitForTimeout(800); await shot(page, '05-detay')
await page.keyboard.press('Escape'); await page.waitForTimeout(300)

// Navbar: Ay → takvime ay görünümüyle
await page.getByText('Ay', { exact: true }).first().click()
await page.waitForURL(u => u.pathname === "/calendar"); await page.waitForTimeout(900)
result.monthView = await page.locator('.fc-dayGridMonth-view').count()
await shot(page, '06-ay')
await page.getByText('Hafta', { exact: true }).first().click(); await page.waitForTimeout(800)
await shot(page, '07-takvim-hafta')

// Takvim sürükle: "Kod Gözden Geçirme" (Cum 14:00) → Cum 16:00
const ev = page.locator('.fc-timegrid-event', { hasText: 'Kod Gözden Geçirme' }).last()
const eb = await ev.boundingBox()
const slot = await page.locator('.fc-timegrid-slot-lane[data-time="16:00:00"]').first().boundingBox()
await page.mouse.move(eb.x + eb.width / 2, eb.y + 6); await page.mouse.down()
await page.mouse.move(eb.x + eb.width / 2, eb.y + 20, { steps: 4 })
await page.mouse.move(eb.x + eb.width / 2, slot.y + 6, { steps: 15 })
await page.mouse.up(); await page.waitForTimeout(1500)
result.calMoved = (await list()).filter(a => a.title === 'Kod Gözden Geçirme').map(a => a.start).sort()

// Açık tema
await page.keyboard.press('Control+K'); await page.waitForTimeout(300)
await page.keyboard.type('tema'); await page.waitForTimeout(300); await page.keyboard.press('Enter'); await page.waitForTimeout(600)
await shot(page, '08-takvim-acik')
await page.goto(WEB + '/'); await page.getByText('Ekip Buluşması').first().waitFor(); await page.waitForTimeout(600)
await shot(page, '09-pano-acik')

console.log(JSON.stringify({ ...result, consoleErrors: errors }, null, 2))
await browser.close()
