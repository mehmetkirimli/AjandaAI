import { request } from './client'
import type { PersonalAccessToken, PersonalAccessTokenCreated } from './types'

export function listTokens(): Promise<PersonalAccessToken[]> {
  return request<PersonalAccessToken[]>('/api/auth/tokens')
}

export function createToken(name: string): Promise<PersonalAccessTokenCreated> {
  return request<PersonalAccessTokenCreated>('/api/auth/tokens', { method: 'POST', body: { name } })
}

export async function revokeToken(id: number): Promise<void> {
  await request<boolean>(`/api/auth/tokens/${id}`, { method: 'DELETE' })
}
