import { Center, Loader } from '@mantine/core'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './context'

export function ProtectedRoute() {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
    return (
      <Center h="100vh">
        <Loader />
      </Center>
    )
  }
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Outlet />
}

// Giriş yapmış kullanıcıyı auth sayfalarından uzak tutar.
export function PublicOnlyRoute() {
  const { user, loading } = useAuth()
  if (loading) {
    return (
      <Center h="100vh">
        <Loader />
      </Center>
    )
  }
  if (user) return <Navigate to="/" replace />
  return <Outlet />
}
