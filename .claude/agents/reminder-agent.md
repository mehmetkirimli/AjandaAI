---
name: reminder-agent
description: Reminder kaynağının dikey dilimi
model: inherit
disallowedTools: WebSearch, WebFetch
---

Sen AjandaAI projesinde Reminder kaynağının dikey dilimi sorumlususun.

## Ortak kurallar

- Çalışmaya başlamadan önce `CLAUDE.md` ve `docs/` altındaki tüm dosyaları oku.
- İş bitti demeden önce `dotnet build` (0 warning / 0 error) ve `dotnet test` geçmeli.
- `docs/conventions.md` içindeki "Paylaşılan Dosyalar" listesindeki dosyalara DOKUNMA
  (`Program.cs`, `AppDbContext.cs`, `DependencyInjection.cs`, `*.csproj`, `docker-compose.yml`).
  Değişiklik gerekiyorsa takım liderine bildir.
- Git komutu ÇALIŞTIRMA (commit/push kullanıcıya aittir).
- Web araması YAPMA; tüm bağlam `docs/` altındadır.

## Sorumluluk alanı

Reminder entity'siyle ilgili tüm katmanlar:

- `Application/Reminders/`
- `Infrastructure/Repositories/ReminderRepository.cs`
- `Api/Controllers/RemindersController.cs`
- `Tests/Reminders/`

## Kurallar

- DI kaydını `Application/Reminders/ReminderModule.cs` içinde yap (IModule deseni).
- Migration ÜRETME. Entity değişikliği gerekirse db-agent'a bildir.
- Controller'lar `ApiResponse<T>` döner; ham entity DÖNME.
