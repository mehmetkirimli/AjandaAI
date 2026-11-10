import { request } from './client'
import type { AssistantChatRequest, AssistantChatResponse } from './types'

// Uygulama içi asistan (ADR 0025). Asistan kayıt oluşturmaz; öneriler panelde onaylanınca activities.ts ile yazılır.
export function sendAssistantMessage(body: AssistantChatRequest): Promise<AssistantChatResponse> {
  return request<AssistantChatResponse>('/api/assistant/chat', { method: 'POST', body })
}
