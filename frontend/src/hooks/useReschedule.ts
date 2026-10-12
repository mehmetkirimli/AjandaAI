import { notifications } from '@mantine/notifications'
import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query'
import { rescheduleActivity } from '../api/activities'
import { ApiError } from '../api/client'
import type { ActivityListItem } from '../api/types'

export interface RescheduleVars {
  item: ActivityListItem
  start: string
  end: string
  successMessage: string
}

// Pano ve Takvim ortak: ['activities', ...] öneki altındaki TÜM listeler iyimser güncellenir,
// hatada geri alınır, bitince önek geçersiz kılınır. Böylece iki ekran birbirini yansıtır.
export function useReschedule() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (vars: RescheduleVars) => rescheduleActivity(vars.item.id, vars.start, vars.end),
    onMutate: async (vars) => {
      await queryClient.cancelQueries({ queryKey: ['activities'] })
      const previous: [QueryKey, ActivityListItem[] | undefined][] = queryClient.getQueriesData<ActivityListItem[]>({
        queryKey: ['activities'],
      })
      queryClient.setQueriesData<ActivityListItem[]>({ queryKey: ['activities'] }, (old) =>
        old?.map((a) => (a.id === vars.item.id ? { ...a, start: vars.start, end: vars.end } : a)),
      )
      return { previous }
    },
    onError: (error, _vars, context) => {
      context?.previous.forEach(([key, data]) => queryClient.setQueryData(key, data))
      notifications.show({
        color: 'red',
        title: 'Aktivite taşınamadı',
        message: error instanceof ApiError ? (error.errors[0] ?? error.message) : 'Beklenmeyen bir hata oluştu.',
      })
    },
    onSuccess: (_data, vars) => {
      notifications.show({ color: 'teal', title: 'Kaydedildi', message: vars.successMessage, autoClose: 2500 })
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: ['activities'] })
      void queryClient.invalidateQueries({ queryKey: ['activity'] })
    },
  })
}
