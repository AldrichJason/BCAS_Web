import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AnnouncementDetailPage } from './pages/AnnouncementDetailPage'
import { AnnouncementsPage } from './pages/AnnouncementsPage'
import { ContentPageView } from './pages/ContentPageView'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProgramDetailPage } from './pages/ProgramDetailPage'
import { ProgramsPage } from './pages/ProgramsPage'
import { AnnouncementsAdminPage } from './pages/admin/AnnouncementsAdminPage'
import { DashboardPage } from './pages/admin/DashboardPage'
import { FaqsAdminPage } from './pages/admin/FaqsAdminPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="announcements" element={<AnnouncementsPage />} />
        <Route path="announcements/:slug" element={<AnnouncementDetailPage />} />
        <Route path="programs" element={<ProgramsPage />} />
        <Route path="programs/:slug" element={<ProgramDetailPage />} />
        <Route path="pages/:slug" element={<ContentPageView />} />
        <Route path="login" element={<LoginPage />} />

        <Route
          path="admin"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/announcements"
          element={
            <ProtectedRoute>
              <AnnouncementsAdminPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/faqs"
          element={
            <ProtectedRoute>
              <FaqsAdminPage />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
