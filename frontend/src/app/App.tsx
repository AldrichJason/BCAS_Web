import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '@/features/auth/AuthProvider';
import { ForgotPasswordPage } from '@/features/auth/ForgotPasswordPage';
import { LoginPage } from '@/features/auth/LoginPage';
import { ResetPasswordPage } from '@/features/auth/ResetPasswordPage';
import { DashboardPage } from '@/routes/DashboardPage';
import { ProtectedRoute } from '@/routes/ProtectedRoute';

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />

          <Route
            path="/admin"
            element={
              <ProtectedRoute allow={['SUPER_ADMIN']}>
                <DashboardPage title="Administration" />
              </ProtectedRoute>
            }
          />
          <Route
            path="/department"
            element={
              <ProtectedRoute allow={['ACADEMIC_HEAD']}>
                <DashboardPage title="Department Dashboard" />
              </ProtectedRoute>
            }
          />
          <Route
            path="/registrar"
            element={
              <ProtectedRoute allow={['REGISTRAR']}>
                <DashboardPage title="Admissions & Registrar" />
              </ProtectedRoute>
            }
          />
          <Route
            path="/operations"
            element={
              <ProtectedRoute allow={['VP_OPERATIONS']}>
                <DashboardPage title="Operations Overview" />
              </ProtectedRoute>
            }
          />

          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
