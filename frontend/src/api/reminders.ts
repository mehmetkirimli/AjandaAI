import { request } from './client'
import type { PagedResult, Reminder, ReminderListItem } from './types'

// Liste ucu not döndürmez; küçük sayıda hatırlatma için her biri detaydan tamamlanır.
export async function fetchReminders(activityId: number): Promise<Reminder[]> {
  const page = await request<PagedResult<ReminderListItem>>('/api/reminders', {
    query: { activityId, pageSize: 100 },
  })
  const details = await Promise.all(page.items.map((r) => request<Reminder>(`/api/reminders/${r.id}`)))
  return details.sort((a, b) => new Date(a.remindAt).getTime() - new Date(b.remindAt).getTime())
}

export function createReminder(activityId: number, remindAt: string, note: string | null): Promise<Reminder> {
  return request<Reminder>('/api/reminders', { method: 'POST', body: { activityId, remindAt, note } })
}

export async function deleteReminder(id: number): Promise<void> {
  await request<boolean>(`/api/reminders/${id}`, { method: 'DELETE' })
}
