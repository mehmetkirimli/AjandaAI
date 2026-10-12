// F3b ekran kabulü: (1) Krem sür — takvimde 21:00 seç → Tekrarla: Her gün, 12 Nis 2027, 15 dk hatırlatma → 183;
// (2) Her Pazartesi iş — 12 Eki → 30 Kas tek aktivite gir → uzun süre uyarısı → Tekrarla'ya geç → 8;
// (3) Pano/takvim görünümü, seri ikonu; (4) Drawer: seri bilgisi + "Bu ve sonrakileri durdur".
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
const all = async () => {
  const items = []
  for (let p = 1; p < 10; p++) {
    const r = (await api(`/api/activities?pageSize=100&page=${p}&from=2026-10-01T00:00:00Z&to=2027-05-01T00:00:00Z`, { headers: auth })).body.data
    items.push(...r.items); if (!r.hasNext) break
  }
  return items
}

// Bir DatePickerInput'ta ay ilerleyip gün seç (açık dropdown içinde).
const pickDate = async (ariaLabel, monthsAhead, day) => {
  await page.getByRole('button', { name: ariaLabel }).first().click().catch(async () => page.getByLabel(ariaLabel).first().click())
  await page.waitForTimeout(250)
  const dd = page.locator('.mantine-Popover-dropdown:visible').last()
  for (let i = 0; i < monthsAhead; i++) { await dd.locator('button[data-direction="next"]').click(); await page.waitForTimeout(80) }
  await dd.locator('button:not([data-outside])', { hasText: new RegExp(`^${day}$`) }).first().click()
  await page.waitForTimeout(250)
}

await page.goto(WEB + '/login')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')

// (1) Takvimde Pzt 12 Eki 21:00–21:15 seç
await page.getByRole('link', { name: 'Takvim' }).first().click()
await page.waitForURL('**/calendar')
await page.locator('.fc-timegrid-col[data-date="2026-10-12"]').waitFor()
await page.locator('.fc-scroller').last().evaluate(el => { el.scrollTop = el.scrollHeight })
await page.waitForTimeout(400)
const col = await page.locator('.fc-timegrid-col[data-date="2026-10-12"]').boundingBox()
const slot = await page.locator('.fc-timegrid-slot-lane[data-time="21:00:00"]').first().boundingBox()
await page.mouse.move(col.x + col.width / 2, slot.y + 2); await page.mouse.down()
await page.mouse.move(col.x + col.width / 2, slot.y + slot.height / 2 - 2, { steps: 6 }); await page.mouse.up()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.getByPlaceholder('Aktivite başlığı').fill('Krem sür')
await page.getByLabel('Tekrarla').check()
await page.waitForTimeout(300)
await page.getByText('Her gün', { exact: true }).first().click()
await pickDate('Tekrar bitiş tarihi', 5, 12)
await page.getByText('15 dk önce', { exact: true }).first().click()
await page.waitForTimeout(300)
await shot(page, '01-krem-form-onizleme')
result.kremPreview = await page.getByText(/aktivite oluştur/).first().textContent()
await page.getByRole('button', { name: /aktivite oluştur/ }).click()
await page.waitForTimeout(2500)
await shot(page, '02-krem-olustu-takvim')
let items = await all()
const krem = items.filter(a => a.title === 'Krem sür')
result.kremCount = krem.length
result.kremSeriesIds = [...new Set(krem.map(a => a.seriesId))]
const firstKrem = krem.sort((a, b) => a.start.localeCompare(b.start))[0]
result.kremFirst = firstKrem?.start
result.kremReminders = (await api(`/api/reminders?activityId=${firstKrem.id}&pageSize=10`, { headers: auth })).body.data.items.map(r => r.remindAt)

// (2) Her Pazartesi iş — yanlış giriş → uyarı → Tekrarla'ya geç
await page.getByRole('link', { name: 'Pano' }).first().click()
await page.waitForURL(WEB + '/')
await page.waitForTimeout(600)
const mon = page.getByText('12 Pzt', { exact: true })
await mon.locator('xpath=ancestor::*[.//button[contains(., "Aktivite Ekle")]][1]').getByRole('button', { name: /Aktivite Ekle/ }).click()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.getByPlaceholder('Aktivite başlığı').fill('İş - Ofisten çalışma')
await pickDate('Bitiş tarihi', 1, 30)
await page.waitForTimeout(400)
await shot(page, '03-uzun-sure-uyarisi')
result.warning = await page.getByText(/tekrarlamak mı istedin/).count()
await page.getByRole('button', { name: /Tekrarla'ya geç/ }).click()
await page.waitForTimeout(500)
await shot(page, '04-tekrarla-ya-gecildi')
result.isPreview = await page.getByText(/aktivite oluştur/).first().textContent()
await page.getByRole('button', { name: /aktivite oluştur/ }).click()
await page.waitForTimeout(2000)
items = await all()
const is = items.filter(a => a.title === 'İş - Ofisten çalışma')
result.isCount = is.length
result.isDays = [...new Set(is.map(a => new Date(a.start).toLocaleDateString('tr-TR', { weekday: 'short', timeZone: 'Europe/Istanbul' })))]
await page.waitForTimeout(400)
await shot(page, '05-pano-seriler')

// (3) Takvim hafta
await page.getByRole('link', { name: 'Takvim' }).first().click(); await page.waitForTimeout(1000)
await shot(page, '06-takvim-seriler')

// (4) Drawer: 14 Eki krem → Bu ve sonrakileri durdur
await page.getByRole('link', { name: 'Pano' }).first().click(); await page.waitForURL(WEB + '/'); await page.waitForTimeout(600)
const wed = page.getByText('14 Çar', { exact: true }).locator('xpath=ancestor::*[.//button[contains(., "Aktivite Ekle")]][1]')
await wed.getByText(/Krem sür/).first().click()
await page.waitForTimeout(900)
await shot(page, '07-detay-seri')
await page.getByRole('button', { name: /Bu ve sonrakileri durdur/ }).click()
await page.waitForTimeout(400)
await shot(page, '08-durdur-onay')
await page.getByRole('dialog').getByRole('button', { name: /durdur/i }).last().click()
await page.waitForTimeout(2000)
await shot(page, '09-durduruldu-pano')
items = await all()
result.kremActiveAfterStop = items.filter(a => a.title === 'Krem sür').length

console.log(JSON.stringify({ ...result, consoleErrors: errors }, null, 2))
await browser.close()
