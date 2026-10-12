import { Center, Loader } from '@mantine/core'
import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { AdminRoute } from './auth/AdminRoute'
import { ProtectedRoute, PublicOnlyRoute } from './auth/ProtectedRoute'
import { AppLayout } from './components/AppLayout'
import { AdminActivitiesPage } from './pages/admin/AdminActivitiesPage'
import { AdminCategoriesPage } from './pages/admin/AdminCategoriesPage'
import { AdminUsersPage } from './pages/admin/AdminUsersPage'
import { BoardPage } from './pages/BoardPage'
import { CheckEmailPage } from './pages/CheckEmailPage'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { LoginPage } from './pages/LoginPage'
import { ProfilePage } from './pages/ProfilePage'
import { RegisterPage } from './pages/RegisterPage'
import { VerifyEmailPage } from './pages/VerifyEmailPage'

// FullCalendar ağır olduğu için takvim sayfası ilk açılışta yüklenmez.
const CalendarPage = lazy(() => import('./pages/CalendarPage'))

export default function App() {
  return (
    <Routes>
      {/* Doğrulama linki oturumdan bağımsız açılabilmeli. */}
      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/check-email" element={<CheckEmailPage />} />
      </Route>
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<BoardPage />} />
          <Route
            path="/calendar"
            element={
              <Suspense
                fallback={
                  <Center h="50vh">
                    <Loader />
                  </Center>
                }
              >
                <CalendarPage />
              </Suspense>
            }
          />
          <Route path="/profile" element={<ProfilePage />} />
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<Navigate to="/admin/users" replace />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/categories" element={<AdminCategoriesPage />} />
            <Route path="/admin/activities" element={<AdminActivitiesPage />} />
          </Route>
          <Route path="*" element={<ComingSoonPage title="Sayfa bulunamadı" />} />
        </Route>
      </Route>
    </Routes>
  )
}
