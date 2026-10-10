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
