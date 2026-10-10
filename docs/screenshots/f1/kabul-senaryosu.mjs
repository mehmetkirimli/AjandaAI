// F1 ekran kabulü: kayıt → doğrulama → giriş → pano → sürükle-bırak → yenileme → çıkış.
import { chromium } from 'playwright'
import fs from 'node:fs'

const API = 'http://localhost:5250', WEB = 'http://localhost:5173'
const LOG = process.env.API_LOG, OUT = process.env.OUT
const email = `demo${Date.now()}@ajanda.dev`, password = 'mavi-kayik-sahilde-77'
const shot = (p, n) => p.screenshot({ path: `${OUT}/${n}.png`, fullPage: true })
const api = async (path, opts = {}) => {
  const r = await fetch(API + path, { ...opts, headers: { 'Content-Type': 'application/json', ...(opts.headers ?? {}) } })
  return { status: r.status, body: await r.json().catch(() => null) }
}

const browser = await chromium.launch({ channel: 'chrome' })
const page = await browser.newPage({ viewport: { width: 1440, height: 900 }, locale: 'tr-TR', timezoneId: 'Europe/Istanbul' })
const errors = []
page.on('console', m => m.type() === 'error' && errors.push(m.text()))

// 1-2. Giriş ve kayıt ekranları
await page.goto(WEB + '/login'); await page.waitForLoadState('networkidle'); await shot(page, '01-giris')
await page.goto(WEB + '/register'); await page.waitForLoadState('networkidle')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Görünen ad').fill('Mehmet Demo')
await page.getByLabel('Şifre').fill(password)
await shot(page, '02-kayit-dolu')
await page.getByRole('button', { name: /kayıt|hesap/i }).click()
await page.waitForURL('**/check-email'); await shot(page, '03-eposta-kontrol')

// 3. Log'daki doğrulama linki
let link
for (let i = 0; i < 20 && !link; i++) {
  await new Promise(r => setTimeout(r, 500))
  const m = [...fs.readFileSync(LOG, 'utf8').matchAll(/(http:\/\/localhost:5173\/verify-email\?token=[\w%-]+)/g)].pop()
  link = m?.[1]
}
if (!link) throw new Error('doğrulama linki logda yok')
await page.goto(link); await page.waitForLoadState('networkidle'); await page.waitForTimeout(800); await shot(page, '04-dogrulandi')

// 4. Bu haftaya örnek aktiviteler (API ile; ekleme formu F3'te)
const login = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
const auth = { Authorization: `Bearer ${login.body.data.accessToken}` }
const cats = (await api('/api/categories?pageSize=100', { headers: auth })).body.data.items
const monday = new Date('2026-10-05T00:00:00+03:00')
const at = (day, h, m = 0) => new Date(monday.getTime() + ((day * 24 + h) * 60 + m) * 60000)
const plan = [
  [0, 9, 1, 'Sabah koşusu', 'High'], [0, 14, 2, 'Proje toplantısı', 'Medium'], [1, 19, 3, 'Kitap kulübü', 'Low'],
  [2, 8, 4, 'Diş hekimi', 'High'], [3, 18, 5, 'Akşam yemeği - Ayşe', 'Medium'], [4, 10, 1, 'Haftalık planlama', 'Low'],
  [5, 11, 2, 'Pazar kahvaltısı hazırlığı', 'Medium'], [5, 16, 3, 'Sinema', 'Low']
]
for (const [d, h, dur, title, priority] of plan) {
  const r = await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({
    categoryId: cats[(d + h) % cats.length].id, title, description: '', status: 'Planned', priority, energyLevel: 'Medium',
    start: at(d, h).toISOString(), end: at(d, h + dur).toISOString(), isAllDay: false, location: null,
    isFlexible: false, estimatedBudget: 0, rating: null, wouldRepeat: null }) })
  if (r.status !== 201) throw new Error(`aktivite ${title}: ${r.status} ${JSON.stringify(r.body)}`)
}

// 5. Arayüzden giriş → pano
await page.goto(WEB + '/login'); await page.waitForLoadState('networkidle')
await page.getByLabel('E-posta').fill(email); await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/'); await page.getByText('Sabah koşusu').waitFor(); await page.waitForTimeout(500)
await shot(page, '05-pano')

// 6. Sürükle-bırak: "Diş hekimi" (Çarşamba) → Cuma
const card = page.getByText('Diş hekimi', { exact: true })
const from = await card.boundingBox()
const friday = await page.getByText('Cuma', { exact: true }).boundingBox()
await page.mouse.move(from.x + 20, from.y + 10); await page.mouse.down()
await page.mouse.move(from.x + 40, from.y + 30, { steps: 5 })
await page.mouse.move(friday.x + 30, friday.y + 160, { steps: 20 })
await page.waitForTimeout(300); await shot(page, '06-surukleniyor')
await page.mouse.up(); await page.waitForTimeout(1500); await shot(page, '07-birakildi')

// 7. Yenileme: oturum ve yeni yer kalıcı mı?
await page.reload(); await page.getByText('Diş hekimi').waitFor(); await page.waitForTimeout(800)
await shot(page, '08-yenileme-sonrasi')
const moved = (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items.find(a => a.title === 'Diş hekimi')

// 8. Koyu tema + çıkış
await page.getByRole('button', { name: /tema|koyu|dark/i }).first().click().catch(() => {})
await page.waitForTimeout(400); await shot(page, '09-koyu-tema')
await page.getByText('Mehmet Demo').first().click(); await page.waitForTimeout(300)
await page.getByText('Çıkış').first().click(); await page.waitForURL('**/login'); await shot(page, '10-cikis')

console.log(JSON.stringify({ email, movedStart: moved?.start, consoleErrors: errors }, null, 2))
await browser.close()
