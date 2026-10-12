// F3 ekran kabulü: Pano "+ Aktivite Ekle" → form → oluştur; takvimde aralık seç → form → oluştur;
// C kısayolu + boş başlık hatası; drawer Düzenle; hatırlatma ekle/sil; pasife al.
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
const list = async () => (await api('/api/activities?pageSize=100', { headers: auth })).body.data.items

await page.goto(WEB + '/login')
await page.getByLabel('E-posta').fill(email)
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click()
await page.waitForURL(WEB + '/')
await page.waitForTimeout(800)
await shot(page, '01-bos-pano')

// 1) Pano: Perşembe "+ Aktivite Ekle"
const thu = page.getByText('15 Per', { exact: true })
const thuCard = thu.locator('xpath=ancestor::*[.//button[contains(., "Aktivite Ekle")]][1]')
await thuCard.getByRole('button', { name: /Aktivite Ekle/ }).click()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.waitForTimeout(400)
await shot(page, '02-form-panodan')
await page.getByPlaceholder('Aktivite başlığı').fill('Spor salonu')
await page.getByPlaceholder('Açıklama ekle…').fill('Bacak günü, 45 dk ağırlık + 15 dk kardiyo.')
await page.getByRole('button', { name: /Orta/ }).first().click().catch(() => {})
await page.getByRole('menuitem', { name: /Yüksek/ }).first().click().catch(() => {})
await page.waitForTimeout(200)
await shot(page, '03-form-dolu')
await page.keyboard.press('Control+Enter')
await page.waitForTimeout(1500)
await shot(page, '04-panoda-olustu')
result.created = (await list()).find(a => a.title === 'Spor salonu')

// 2) C kısayolu + boş başlık hatası
await page.locator('body').click({ position: { x: 1300, y: 700 } })
await page.keyboard.press('c')
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.keyboard.press('Control+Enter')
await page.waitForTimeout(500)
await shot(page, '05-bos-baslik-hatasi')
result.titleError = await page.getByText('Başlık zorunludur.').count()
await page.keyboard.press('Escape'); await page.waitForTimeout(300)
const discard = page.getByRole('button', { name: /Evet|Kaybolsun|Vazgeç ve kapat|Çık/ })
if (await discard.count()) await discard.first().click()
await page.waitForTimeout(300)

// 3) Takvim: Cuma 14:00–15:30 aralığını sürükleyerek seç
await page.getByRole('link', { name: 'Takvim' }).first().click()
await page.waitForURL('**/calendar')
await page.locator('.fc-timegrid-col[data-date="2026-10-16"]').waitFor()
await page.waitForTimeout(600)
const col = await page.locator('.fc-timegrid-col[data-date="2026-10-16"]').boundingBox()
const s1 = await page.locator('.fc-timegrid-slot-lane[data-time="14:00:00"]').first().boundingBox()
const s2 = await page.locator('.fc-timegrid-slot-lane[data-time="15:00:00"]').first().boundingBox()
await page.mouse.move(col.x + col.width / 2, s1.y + 3); await page.mouse.down()
await page.mouse.move(col.x + col.width / 2, s2.y + s2.height - 3, { steps: 10 })
await page.waitForTimeout(200); await shot(page, '06-takvimde-secim')
await page.mouse.up()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.waitForTimeout(400)
await shot(page, '07-form-takvimden')
await page.getByPlaceholder('Aktivite başlığı').fill('Müşteri görüşmesi')
await page.keyboard.press('Control+Enter')
await page.waitForTimeout(1500)
await shot(page, '08-takvimde-olustu')
result.fromCalendar = (await list()).find(a => a.title === 'Müşteri görüşmesi')

// 4) Drawer: Düzenle
await page.locator('.fc-event', { hasText: 'Müşteri görüşmesi' }).first().click()
await page.waitForTimeout(800)
await shot(page, '09-detay')
await page.getByRole('button', { name: 'Düzenle' }).click()
await page.getByPlaceholder('Aktivite başlığı').waitFor()
await page.getByPlaceholder('Aktivite başlığı').fill('Müşteri görüşmesi - ABC Ltd.')
await page.keyboard.press('Control+Enter')
await page.waitForTimeout(1500)
result.edited = (await list()).find(a => a.title.startsWith('Müşteri görüşmesi'))?.title

// 5) Hatırlatma ekle (15 dk önce) + not, sonra sil
await page.locator('.fc-event', { hasText: 'ABC Ltd.' }).first().click()
await page.waitForTimeout(800)
await page.getByText('15 dk önce', { exact: true }).first().click()
await page.getByPlaceholder('Not (isteğe bağlı)').fill('Sunum dosyasını aç')
await page.getByRole('button', { name: 'Ekle', exact: true }).click()
await page.waitForTimeout(1200)
await shot(page, '10-hatirlatma-eklendi')
const id = result.fromCalendar?.id
result.reminders = (await api(`/api/reminders?activityId=${id}&pageSize=100`, { headers: auth })).body.data.items.map(r => r.remindAt)
await page.getByRole('button', { name: 'Hatırlatmayı sil' }).first().click()
await page.waitForTimeout(1200)
result.remindersAfterDelete = (await api(`/api/reminders?activityId=${id}&pageSize=100`, { headers: auth })).body.data.items.length

// 6) Pasife al
await page.getByRole('button', { name: 'Pasife al' }).first().click()
await page.waitForTimeout(500)
await shot(page, '11-pasife-al-onay')
await page.getByRole('dialog').getByRole('button', { name: /Pasife al/ }).last().click()
await page.waitForTimeout(1500)
await shot(page, '12-pasife-alindi')
result.stillListed = (await list()).some(a => a.title.startsWith('Müşteri görüşmesi'))

// Pano son hali
await page.getByRole('link', { name: 'Pano' }).first().click()
await page.waitForURL(WEB + '/'); await page.waitForTimeout(800)
await shot(page, '13-pano-son')

console.log(JSON.stringify({ ...result, created: result.created && [result.created.start, result.created.end, result.created.priority],
  fromCalendar: result.fromCalendar && [result.fromCalendar.start, result.fromCalendar.end], consoleErrors: errors }, null, 2))
await browser.close()
