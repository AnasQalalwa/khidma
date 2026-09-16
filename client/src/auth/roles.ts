export const Roles = {
  Admin: 'Admin',
  Customer: 'Customer',
  Provider: 'Provider',
} as const

export type Role = (typeof Roles)[keyof typeof Roles]

export function dashboardPath(role: string): string {
  if (role === Roles.Admin) {
    return '/admin'
  }

  if (role === Roles.Provider) {
    return '/provider'
  }

  return '/customer'
}

export const CUSTOMER_WORKSPACE_LINKS = [
  { to: '/customer', label: 'Overview', end: true },
  { to: '/customer/requests', label: 'My Requests' },
  { to: '/customer/bookings', label: 'Bookings' },
] as const

export const PROVIDER_WORKSPACE_LINKS = [
  { to: '/provider', label: 'Overview', end: true },
  { to: '/provider/requests', label: 'Available Requests' },
  { to: '/provider/offers', label: 'My Offers' },
  { to: '/provider/bookings', label: 'Bookings' },
  { to: '/provider/profile', label: 'Profile' },
] as const

export const ADMIN_WORKSPACE_LINKS = [
  { to: '/admin', label: 'Overview', end: true },
  { to: '/admin/providers', label: 'Providers' },
  { to: '/admin/catalog', label: 'Catalog' },
] as const

export function workspaceLinks(role: string) {
  if (role === Roles.Provider) {
    return PROVIDER_WORKSPACE_LINKS
  }

  if (role === Roles.Admin) {
    return ADMIN_WORKSPACE_LINKS
  }

  return CUSTOMER_WORKSPACE_LINKS
}
