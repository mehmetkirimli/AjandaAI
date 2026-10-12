// Yönetim uçları (ADR 0023 Karar 4). Yalnızca Admin rolü çağırabilir.
import { fetchCategoryPages } from './activities'
import { request } from './client'
import type {
  AdminActivity,
  AdminUser,
  AdminUserCreateRequest,
  AdminUserUpdateRequest,
  Category,
  PagedResult,
} from './types'

export function fetchAdminUsers(params: { page: number; pageSize: number; search?: string }): Promise<PagedResult<AdminUser>> {
  return request<PagedResult<AdminUser>>('/api/admin/users', { query: params })
}

// Sahip adlarını eşlemek için tüm kullanıcılar (sayfa sayfa).
export async function fetchAllAdminUsers(): Promise<AdminUser[]> {
  const all: AdminUser[] = []
  for (let page = 1; page <= 20; page++) {
    const r = await fetchAdminUsers({ page, pageSize: 100 })
    all.push(...r.items)
    if (!r.hasNext) break
  }
  return all
}

export function createAdminUser(body: AdminUserCreateRequest): Promise<AdminUser> {
  return request<AdminUser>('/api/admin/users', { method: 'POST', body })
}

export function updateAdminUser(id: number, body: AdminUserUpdateRequest): Promise<AdminUser> {
  return request<AdminUser>(`/api/admin/users/${id}`, { method: 'PUT', body })
}

export function deactivateAdminUser(id: number): Promise<AdminUser> {
  return request<AdminUser>(`/api/admin/users/${id}`, { method: 'DELETE' })
}

export function reactivateAdminUser(u: AdminUser): Promise<AdminUser> {
  return updateAdminUser(u.id, {
    email: u.email,
    displayName: u.displayName,
    timeZoneId: u.timeZoneId,
    role: u.role,
    isActive: true,
  })
}

// Pasifler dahil tüm kategoriler (includeInactive yalnızca Admin için geçerli, ADMIN-11).
export async function fetchAllCategories(): Promise<Category[]> {
  const items = await fetchCategoryPages({ includeInactive: 'true' })
  return items.sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id)
}

export function createCategory(name: string): Promise<Category> {
  return request<Category>('/api/categories', { method: 'POST', body: { name } })
}

export function updateCategory(id: number, name: string, isActive: boolean): Promise<Category> {
  return request<Category>(`/api/categories/${id}`, { method: 'PUT', body: { name, isActive } })
}

export async function reorderCategories(categoryIds: number[]): Promise<void> {
  await request<unknown>('/api/categories/order', { method: 'PUT', body: { categoryIds } })
}

export function fetchAdminActivities(params: {
  page: number
  pageSize: number
  userId?: number
  status?: string
  isActive?: string
}): Promise<PagedResult<AdminActivity>> {
  return request<PagedResult<AdminActivity>>('/api/admin/activities', { query: params })
}

export function deactivateAdminActivity(id: number): Promise<AdminActivity> {
  return request<AdminActivity>(`/api/admin/activities/${id}`, { method: 'DELETE' })
}
