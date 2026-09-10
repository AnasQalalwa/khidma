import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { RequireAuth } from './RequireAuth'
import { useAuth } from './useAuth'
import type { Role } from './roles'

export function RequireRole({
  role,
  children,
}: {
  role: Role
  children: ReactNode
}) {
  const { user } = useAuth()

  return (
    <RequireAuth>
      {user && user.role !== role ? (
        <Navigate to="/forbidden" replace />
      ) : (
        children
      )}
    </RequireAuth>
  )
}
