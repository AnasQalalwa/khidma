import type { ReactNode } from 'react'
import { WorkspaceNav } from './WorkspaceNav'

export function WorkspaceLayout({
  role,
  children,
}: {
  role: string
  children: ReactNode
}) {
  return (
    <div className="workspace">
      <WorkspaceNav role={role} />
      {children}
    </div>
  )
}
