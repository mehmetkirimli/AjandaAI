// F1 devamı ekran kabulü: kompakt tasarım + profil (bilgi düzenleme, şifre değiştirme, tekrar giriş).
import { chromium } from 'playwright'
import fs from 'node:fs'

const API = process.env.API ?? 'http://localhost:5250', WEB = process.env.WEB ?? 'http://localhost:5173'
const LOG = process.env.API_LOG, OUT = process.env.OUT
const email = `demo${Date.now()}@ajanda.dev`, password = 'mavi-kayik-sahilde-77', newPassword = 'yesil-dagda-sabah-yuruyusu-42'
const shot = (p, n) => p.screenshot({ path: `${OUT}/${n}.png` })
const api = async (path, opts = {}) => {
  const r = await fetch(API + path, { ...opts, headers: { 'Content-Type': 'application/json', ...(opts.headers ?? {}) } })
  return { status: r.status, body: await r.json().catch(() => null) }
}

const browser = await chromium.launch({ channel: 'chrome' })
const page = await browser.newPage({ viewport: { width: 1536, height: 864 }, locale: 'tr-TR', timezoneId: 'Europe/Istanbul' })
const errors = []
page.on('console', m => m.type() === 'error' && errors.push(m.text()))

await page.goto(WEB + '/login'); await page.waitForLoadState('networkidle'); await shot(page, '01-giris')
await page.goto(WEB + '/register'); await page.waitForLoadState('networkidle')
await page.getByLabel('E-posta').fill(email); await page.getByLabel('Görünen ad').fill('Mehmet Demo')
await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /kayıt/i }).click(); await page.waitForURL('**/check-email')

let link
for (let i = 0; i < 20 && !link; i++) {
  await new Promise(r => setTimeout(r, 500))
  link = [...fs.readFileSync(LOG, 'utf8').matchAll(/(http:\/\/localhost:\d+\/verify-email\?token=[\w%-]+)/g)].pop()?.[1]
}
await page.goto(link); await page.waitForLoadState('networkidle')

await page.goto(WEB + '/login'); await page.getByLabel('E-posta').fill(email); await page.getByLabel('Şifre').fill(password)
await page.getByRole('button', { name: /giriş/i }).click(); await page.waitForURL(WEB + '/'); await page.waitForTimeout(800)
await shot(page, '02-bos-pano')

const login = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
const auth = { Authorization: `Bearer ${login.body.data.accessToken}` }
const cats = (await api('/api/categories?pageSize=100', { headers: auth })).body.data.items
const monday = new Date('2026-10-05T00:00:00+03:00')
const at = (d, h) => new Date(monday.getTime() + (d * 24 + h) * 3600000)
for (const [d, h, dur, title, priority] of [[0, 9, 1, 'Sabah koşusu', 'High'], [0, 14, 2, 'Proje toplantısı', 'Medium'],
  [1, 19, 3, 'Kitap kulübü', 'Low'], [2, 8, 1, 'Diş hekimi', 'High'], [3, 18, 2, 'Akşam yemeği - Ayşe', 'Medium'],
  [4, 10, 1, 'Haftalık planlama', 'Low'], [5, 11, 2, 'Pazar kahvaltısı hazırlığı', 'Medium'], [5, 16, 3, 'Sinema', 'Low']]) {
  await api('/api/activities', { method: 'POST', headers: auth, body: JSON.stringify({ categoryId: cats[(d + h) % cats.length].id,
    title, description: '', status: 'Planned', priority, energyLevel: 'Medium', start: at(d, h).toISOString(),
    end: at(d, h + dur).toISOString(), isAllDay: false, location: null, isFlexible: false, estimatedBudget: 0 }) })
}
await page.reload(); await page.getByText('Sabah koşusu').waitFor(); await page.waitForTimeout(500); await shot(page, '03-pano')

// Profil
await page.getByText('Mehmet Demo').first().click(); await page.getByText('Profilim').click()
await page.waitForURL('**/profile'); await page.waitForTimeout(600); await shot(page, '04-profil')
const name = page.getByLabel('Görünen ad'); await name.fill('Mehmet Hilmi')
await page.getByRole('button', { name: /kaydet/i }).click(); await page.waitForTimeout(1000); await shot(page, '05-profil-kaydedildi')
const headerHasNewName = await page.locator('header').getByText('Mehmet Hilmi').count()

// Şifre: önce yanlış mevcut şifre
await page.getByLabel('Mevcut şifre').fill('yanlis-mevcut-sifre-123')
await page.getByLabel('Yeni şifre', { exact: true }).fill(newPassword)
await page.getByLabel(/tekrar/i).fill(newPassword)
await page.locator('button[type=submit]', { hasText: 'Şifreyi değiştir' }).click(); await page.waitForTimeout(900)
await shot(page, '06-yanlis-mevcut-sifre')
// Sonra doğru
await page.getByLabel('Mevcut şifre').fill(password)
await page.locator('button[type=submit]', { hasText: 'Şifreyi değiştir' }).click()
await page.waitForURL('**/login'); await page.waitForTimeout(600); await shot(page, '07-sifre-degisti-giris')
await page.getByLabel('E-posta').fill(email); await page.getByLabel('Şifre').fill(newPassword)
await page.getByRole('button', { name: /giriş/i }).click(); await page.waitForURL(WEB + '/'); await page.waitForTimeout(600)
await shot(page, '08-yeni-sifreyle-giris')
const oldPw = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })

console.log(JSON.stringify({ email, headerHasNewName, oldPasswordLogin: oldPw.status, consoleErrors: errors }, null, 2))
await browser.close()
