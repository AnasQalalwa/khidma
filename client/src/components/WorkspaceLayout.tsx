import type { ReactNode } from 'react'
import { Roles } from '../auth/roles'
import { WorkspaceNav } from './WorkspaceNav'

export function WorkspaceLayout({
  role,
  children,
}: {
  role: string
  children: ReactNode
}) {
  return (
    <div className={role === Roles.Admin ? 'workspace admin-page' : 'workspace'}>
      <WorkspaceNav role={role} />
      {children}
    </div>
  )
}
