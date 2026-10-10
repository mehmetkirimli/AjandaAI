import { request } from './client'
import type { ActivityDetail, ActivityListItem, ActivityUpdateRequest, Category, PagedResult } from './types'

export async function fetchCategories(): Promise<Category[]> {
  const page = await request<PagedResult<Category>>('/api/categories', { query: { pageSize: 100 } })
  return page.items
}

// Haftalık aralık Start alanına göre filtrelenir; 100'den fazla kayıt varsa sayfalar sırayla çekilir.
export async function fetchActivities(fromIso: string, toIso: string): Promise<ActivityListItem[]> {
  const all: ActivityListItem[] = []
  for (let page = 1; page <= 20; page++) {
    const result = await request<PagedResult<ActivityListItem>>('/api/activities', {
      query: { from: fromIso, to: toIso, page, pageSize: 100 },
    })
    all.push(...result.items)
    if (!result.hasNext) break
  }
  return all
}

export function fetchActivity(id: number): Promise<ActivityDetail> {
  return request<ActivityDetail>(`/api/activities/${id}`)
}

export function updateActivity(id: number, body: ActivityUpdateRequest): Promise<ActivityDetail> {
  return request<ActivityDetail>(`/api/activities/${id}`, { method: 'PUT', body })
}

// Sürükleme: önce detayı al, yalnızca Start/End'i değiştirip tüm alanlarla PUT et.
export async function rescheduleActivity(id: number, start: string, end: string): Promise<ActivityDetail> {
  const d = await fetchActivity(id)
  return updateActivity(id, {
    categoryId: d.categoryId,
    title: d.title,
    description: d.description,
    status: d.status,
    priority: d.priority,
    energyLevel: d.energyLevel,
    start,
    end,
    isAllDay: d.isAllDay,
    location: d.location,
    isFlexible: d.isFlexible,
    estimatedBudget: d.estimatedBudget,
    rating: d.rating,
    wouldRepeat: d.wouldRepeat,
  })
}
