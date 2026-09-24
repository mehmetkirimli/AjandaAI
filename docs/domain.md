# Domain

## Proje Kapsamı
AjandaAI: kişisel asistan / ajanda uygulaması. Kullanıcılar sosyal
hayatlarını planlar, aktivite kaydeder, tamamlananları puanlar.

## v1 Varlıkları
- User
- Category (lookup table, admin panelinden yönetilebilir)
- Activity
- Reminder

## Enum'lar (PostgreSQL'de STRING olarak saklanır)
- ActivityStatus: Planned, InProgress, Completed, Cancelled
- Priority: Low, Medium, High
- EnergyLevel: Low, Medium, High

## Activity alanları
Id, UserId, CategoryId
Title, Description
Status, Priority, EnergyLevel
Start, End, IsAllDay
Location (nullable string)
IsFlexible (bool)
EstimatedBudget
Rating (1-10, nullable), WouldRepeat (nullable bool)
CreatedAt, UpdatedAt

Aktivitenin sahibi değiştirilemez. Bir aktiviteye birden çok
kullanıcının katılması v2'deki davet sistemiyle gelecek
(ActivityParticipant tablosu). Kimin ne değiştirdiği bilgisi
v2'de audit log ile tutulacak.

## v2'ye ertelenenler
- Rating entity (analiz ajanı tarafından üretilecek)
- Plan / aktivite gruplama
- Ortak aktivite + davet sistemi (davet kodu ile)
- AiNote alanı
- Bildirim altyapısı
- MCP entegrasyonu
- Koordinat / harita

## v3'e ertelenenler
- Aktivite fotoğrafları

## Kapsam Dışı (v1)
- Frontend yok. API Swagger üzerinden test edilir.
