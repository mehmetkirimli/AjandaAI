// API sözleşmesi tipleri (backend DTO'larının karşılığı). Enum'lar string gelir.

export interface ApiEnvelope<T> {
  success: boolean
  data: T | null
  message: string
  errors: string[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNext: boolean
}

export interface TokenResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export type UserRole = 'User' | 'Admin'

export interface Me {
  id: number
  email: string
  displayName: string
  timeZoneId: string
  role: UserRole
  createdAt: string
}

export interface RegisterRequest {
  email: string
  displayName: string
  timeZoneId: string
  password: string
}

export type Priority = 'Low' | 'Medium' | 'High'

export interface Category {
  id: number
  name: string
  isActive: boolean
  sortOrder: number
}

export interface AdminUser {
  id: number
  email: string
  displayName: string
  timeZoneId: string
  role: UserRole
  isActive: boolean
  emailConfirmedAt: string | null
  createdAt: string
  updatedAt: string
}

export interface AdminUserCreateRequest {
  email: string
  displayName: string
  timeZoneId: string
  password: string
  role: UserRole
}

export interface AdminUserUpdateRequest {
  email: string
  displayName: string
  timeZoneId: string
  role: UserRole
  // true = yeniden aktif et, false = pasife al (DELETE ile aynı); verilmezse değişmez (ADMIN-12).
  isActive?: boolean
}

// Veri minimizasyonu (ADR 0018 Karar 6): başlık/açıklama yok, yalnızca üstveri.
export interface AdminActivity {
  id: number
  userId: number
  categoryId: number
  status: string
  priority: Priority
  start: string
  end: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface ActivityListItem {
  id: number
  categoryId: number
  title: string
  status: string
  priority: Priority
  start: string
  end: string
  isAllDay: boolean
  seriesId?: number | null
}

export interface ActivityDetail {
  id: number
  userId: number
  categoryId: number
  title: string
  description: string
  status: string
  priority: Priority
  energyLevel: string
  start: string
  end: string
  isAllDay: boolean
  location: string | null
  isFlexible: boolean
  estimatedBudget: number
  rating: number | null
  wouldRepeat: boolean | null
  isActive: boolean
  createdAt: string
  updatedAt: string
  series?: SeriesSummary | null
}

// PUT /api/activities/{id}: kısmi değil, tam gövde ister.
export interface ActivityUpdateRequest {
  categoryId: number
  title: string
  description: string
  status: string
  priority: Priority
  energyLevel: string
  start: string
  end: string
  isAllDay: boolean
  location: string | null
  isFlexible: boolean
  estimatedBudget: number
  rating: number | null
  wouldRepeat: boolean | null
}

export type ActivityStatus = 'Planned' | 'InProgress' | 'Completed' | 'Cancelled'
export type Level = 'Low' | 'Medium' | 'High'

export interface Reminder {
  id: number
  activityId: number
  activityTitle: string
  remindAt: string
  isSent: boolean
  sentAt: string | null
  note: string | null
  createdAt: string
}

export interface ReminderListItem {
  id: number
  activityId: number
  remindAt: string
  isSent: boolean
}

export interface SeriesSummary {
  id: number
  frequency: string
  daysOfWeek: number[]
  startDate: string
  endDate: string
}

export interface SeriesCreateRequest {
  categoryId: number
  title: string
  description: string
  status: string
  priority: Priority
  energyLevel: string
  start: string
  end: string
  isAllDay: boolean
  location: string | null
  isFlexible: boolean
  estimatedBudget: number
  recurrence: { frequency: string; daysOfWeek: number[]; until: string }
  reminderMinutesBefore: number | null
}

export interface SeriesCreated {
  seriesId: number
  createdCount: number
  firstStart: string
  lastStart: string
}
