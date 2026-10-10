import { ApiError } from '../api/client'

// Backend doğrulama mesajları alan adıyla başlar ("Email formatı geçersiz.", "Şifreniz ...").
// Eşleşen mesajlar ilgili alanın altına, kalanlar genel hata olarak döner.
export function splitErrors<K extends string>(
  error: unknown,
  prefixes: Record<K, string[]>,
): { fields: Partial<Record<K, string>>; general: string[] } {
  const fields: Partial<Record<K, string>> = {}
  const general: string[] = []
  if (!(error instanceof ApiError)) {
    general.push('Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.')
    return { fields, general }
  }
  if (error.errors.length === 0) {
    general.push(error.message)
    return { fields, general }
  }
  for (const message of error.errors) {
    const lower = message.toLocaleLowerCase('tr')
    const key = (Object.keys(prefixes) as K[]).find((k) => prefixes[k].some((p) => lower.startsWith(p)))
    if (key) fields[key] = fields[key] ? `${fields[key]} ${message}` : message
    else general.push(message)
  }
  return { fields, general }
}
