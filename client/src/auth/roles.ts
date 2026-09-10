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
