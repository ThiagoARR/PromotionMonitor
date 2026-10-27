import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { Layout } from '@/components/Layout'
import { DashboardPage } from '@/pages/DashboardPage'
import { LoginPage } from '@/pages/LoginPage'
import { MonitoringDetailPage } from '@/pages/MonitoringDetailPage'
import { MonitoringFormPage } from '@/pages/MonitoringFormPage'
import { RegisterPage } from '@/pages/RegisterPage'

function RequireAuth() {
  const token = localStorage.getItem('pm_token')
  if (!token) return <Navigate to="/login" replace />
  return <Outlet />
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<Layout />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/monitorings/new" element={<MonitoringFormPage />} />
            <Route path="/monitorings/:id" element={<MonitoringDetailPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
