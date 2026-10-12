// Raycast tarzı yeniden tasarım kabulü: tüm ekranlar + ⌘K komut paleti + davranışların bozulmadığı (sürükle-bırak).
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

// Giriş ve kayıt ekranları (koyu varsayılan)
await page.goto(WEB + '/login'); await page.waitForLoadState('networkidle'); await page.waitForTimeout(400); await shot(page, '01-giris')
await page.goto(WEB + '/register'); await page.waitForLoadState('networkidle'); await page.waitForTimeout(300); await shot(page, '02-kayit')

// Hesap + örnek veri (API)
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
const monday = new Date('2026-10-05T00:00:00+03:00')
const at = (d, h) => new Date(monday.getTime() + (d * 24 + h) * 3600000)
const plan = [[0, 9, 1, 'Sabah koşusu', 'High'], [0, 14, 2, 'Proje toplantısı', 'Medium'], [1, 19, 2, 'Kitap kulübü', 'Low'],
  [2, 8, 1, 'Diş hekimi', 'High'], [3, 18, 2, 'Akşam yemeği - Ayşe', 'Medium'], [4, 10, 1, 'Haftalık planlama', 'Low'],
  [5, 11, 2, 'Kahvaltı hazırlığı', 'Medium'], [5, 16, 3, 'Sinema', 'Low'], [6, 12, 1.5, 'Pazar yürüyüşü', 'Medium']]
for (const [d, h, dur, title, priority] of plan) {
  await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({
    categoryId: cats[(d * 3 + h) % cats.length].id, title, description: '', status: 'Planned', priority, energyLevel: 'Medium',
    start: at(d, h).toISOString(), end: at(d, h + dur).toISOString(), isAllDay: false, location: null, isFlexible: false, estimatedBudget: 0 }) })
}

// Arayüzden giriş → Pano
await page.goto(WEB + '/login')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')
await page.getByText('Sabah koşusu').waitFor()
await page.waitForTimeout(600)
await shot(page, '03-pano')

// Pano sürükle-bırak hâlâ çalışıyor mu: Diş hekimi (Çar) → Cuma
const card = page.getByText('Diş hekimi', { exact: true })
const from = await card.boundingBox()
const friday = await page.getByText('Cuma', { exact: true }).boundingBox()
await page.mouse.move(from.x + 20, from.y + 8); await page.mouse.down()
await page.mouse.move(from.x + 40, from.y + 30, { steps: 5 })
await page.mouse.move(friday.x + 30, friday.y + 120, { steps: 20 })
await page.waitForTimeout(300); await shot(page, '04-pano-surukleniyor')
await page.mouse.up(); await page.waitForTimeout(1500)
const moved = (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items.find(a => a.title === 'Diş hekimi')

// Takvim
await page.getByRole('link', { name: 'Takvim' }).first().click()
await page.waitForURL('**/calendar')
await page.locator('.fc-event', { hasText: 'Sabah koşusu' }).waitFor()
await page.waitForTimeout(700)
await shot(page, '05-takvim')

// ⌘K komut paleti
await page.keyboard.press('Control+K'); await page.waitForTimeout(500); await shot(page, '06-komut-paleti')
await page.keyboard.type('ay g'); await page.waitForTimeout(400); await shot(page, '07-komut-arama')
await page.keyboard.press('Enter'); await page.waitForTimeout(900); await shot(page, '08-komutla-ay-gorunumu')
const monthViewActive = await page.locator('.fc-dayGridMonth-view').count()

// Profil
await page.keyboard.press('Control+K'); await page.waitForTimeout(300)
await page.keyboard.type('profil'); await page.waitForTimeout(300); await page.keyboard.press('Enter')
await page.waitForURL('**/profile'); await page.waitForTimeout(600); await shot(page, '09-profil')

// Açık tema
await page.keyboard.press('Control+K'); await page.waitForTimeout(300)
await page.keyboard.type('tema'); await page.waitForTimeout(300); await page.keyboard.press('Enter'); await page.waitForTimeout(500)
await page.getByRole('link', { name: 'Pano' }).first().click(); await page.waitForURL(WEB + '/'); await page.waitForTimeout(700)
await shot(page, '10-pano-acik-tema')
await page.getByRole('link', { name: 'Takvim' }).first().click(); await page.waitForTimeout(900)
await shot(page, '11-takvim-acik-tema')

console.log(JSON.stringify({ movedStart: moved?.start, monthViewActive, consoleErrors: errors }, null, 2))
await browser.close()
