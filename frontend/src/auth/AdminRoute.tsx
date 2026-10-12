import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './context'

// Yönetim sayfaları yalnızca Admin rolüne açık; diğerleri ana sayfaya döner (ADMIN-20).
export function AdminRoute() {
  const { user } = useAuth()
  if (user?.role !== 'Admin') return <Navigate to="/" replace />
  return <Outlet />
}
