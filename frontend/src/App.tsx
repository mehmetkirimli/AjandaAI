import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute, PublicOnlyRoute } from './auth/ProtectedRoute'
import { AppLayout } from './components/AppLayout'
import { BoardPage } from './pages/BoardPage'
import { CheckEmailPage } from './pages/CheckEmailPage'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { VerifyEmailPage } from './pages/VerifyEmailPage'

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
          <Route path="/admin" element={<ComingSoonPage title="Admin" />} />
          <Route path="*" element={<ComingSoonPage title="Sayfa bulunamadı" />} />
        </Route>
      </Route>
    </Routes>
  )
}
