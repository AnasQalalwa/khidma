import { Route, Routes } from 'react-router-dom'
import { RequireRole } from './auth/RequireRole'
import { Roles } from './auth/roles'
import { Layout } from './components/Layout'
import { AdminDashboard } from './pages/AdminDashboard'
import { CatalogPage } from './pages/CatalogPage'
import { CustomerDashboard } from './pages/CustomerDashboard'
import { ForbiddenPage } from './pages/ForbiddenPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProviderDashboard } from './pages/ProviderDashboard'
import { RegisterPage } from './pages/RegisterPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route
          path="/customer"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerDashboard />
            </RequireRole>
          }
        />
        <Route
          path="/provider"
          element={
            <RequireRole role={Roles.Provider}>
              <ProviderDashboard />
            </RequireRole>
          }
        />
        <Route
          path="/admin"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminDashboard />
            </RequireRole>
          }
        />
        <Route path="/forbidden" element={<ForbiddenPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
