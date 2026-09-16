import { Route, Routes } from 'react-router-dom'
import { RequireRole } from './auth/RequireRole'
import { Roles } from './auth/roles'
import { Layout } from './components/Layout'
import { AdminDashboard } from './pages/AdminDashboard'
import { AdminAuditPage } from './pages/admin/AdminAuditPage'
import { AdminCatalogPage } from './pages/admin/AdminCatalogPage'
import { AdminProvidersPage } from './pages/admin/AdminProvidersPage'
import { AdminUserDetailPage } from './pages/admin/AdminUserDetailPage'
import { AdminUsersPage } from './pages/admin/AdminUsersPage'
import { AdminVerificationDetailPage } from './pages/admin/AdminVerificationDetailPage'
import { AdminVerificationsPage } from './pages/admin/AdminVerificationsPage'
import { CatalogPage } from './pages/CatalogPage'
import { CustomerDashboard } from './pages/CustomerDashboard'
import { CustomerEditRequestPage } from './pages/customer/CustomerEditRequestPage'
import { CustomerNewRequestPage } from './pages/customer/CustomerNewRequestPage'
import { CustomerRequestDetailPage } from './pages/customer/CustomerRequestDetailPage'
import { CustomerRequestsPage } from './pages/customer/CustomerRequestsPage'
import { ForbiddenPage } from './pages/ForbiddenPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProviderDashboard } from './pages/ProviderDashboard'
import { ProviderOffersPage } from './pages/provider/ProviderOffersPage'
import { ProviderProfilePage } from './pages/provider/ProviderProfilePage'
import { ProviderRequestDetailPage } from './pages/provider/ProviderRequestDetailPage'
import { ProviderRequestsPage } from './pages/provider/ProviderRequestsPage'
import { PublicProviderPage } from './pages/PublicProviderPage'
import { RegisterPage } from './pages/RegisterPage'
import { BookingDetailPage } from './pages/shared/BookingDetailPage'
import { BookingsListPage } from './pages/shared/BookingsListPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route path="/providers/:id" element={<PublicProviderPage />} />
        <Route
          path="/customer"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerDashboard />
            </RequireRole>
          }
        />
        <Route
          path="/customer/requests"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerRequestsPage />
            </RequireRole>
          }
        />
        <Route
          path="/customer/requests/new"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerNewRequestPage />
            </RequireRole>
          }
        />
        <Route
          path="/customer/requests/:id"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerRequestDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/customer/requests/:id/edit"
          element={
            <RequireRole role={Roles.Customer}>
              <CustomerEditRequestPage />
            </RequireRole>
          }
        />
        <Route
          path="/customer/bookings"
          element={
            <RequireRole role={Roles.Customer}>
              <BookingsListPage role="Customer" />
            </RequireRole>
          }
        />
        <Route
          path="/customer/bookings/:id"
          element={
            <RequireRole role={Roles.Customer}>
              <BookingDetailPage role="Customer" />
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
          path="/provider/requests"
          element={
            <RequireRole role={Roles.Provider}>
              <ProviderRequestsPage />
            </RequireRole>
          }
        />
        <Route
          path="/provider/requests/:id"
          element={
            <RequireRole role={Roles.Provider}>
              <ProviderRequestDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/provider/offers"
          element={
            <RequireRole role={Roles.Provider}>
              <ProviderOffersPage />
            </RequireRole>
          }
        />
        <Route
          path="/provider/bookings"
          element={
            <RequireRole role={Roles.Provider}>
              <BookingsListPage role="Provider" />
            </RequireRole>
          }
        />
        <Route
          path="/provider/bookings/:id"
          element={
            <RequireRole role={Roles.Provider}>
              <BookingDetailPage role="Provider" />
            </RequireRole>
          }
        />
        <Route
          path="/provider/profile"
          element={
            <RequireRole role={Roles.Provider}>
              <ProviderProfilePage />
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
        <Route
          path="/admin/users"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminUsersPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/users/:userId"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminUserDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/verifications"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminVerificationsPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/verifications/:providerId"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminVerificationDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/providers"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminProvidersPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/audit"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminAuditPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/catalog"
          element={
            <RequireRole role={Roles.Admin}>
              <AdminCatalogPage />
            </RequireRole>
          }
        />
        <Route path="/forbidden" element={<ForbiddenPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
