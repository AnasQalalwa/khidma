export function Badge({
  children,
  tone = 'default',
}: {
  children: string
  tone?: 'default' | 'customer' | 'provider' | 'admin' | 'muted'
}) {
  const className = tone === 'default' ? 'badge' : `badge badge-${tone}`
  return <span className={className}>{children}</span>
}

export function RoleBadge({ role }: { role: string }) {
  const tone =
    role === 'Admin' ? 'admin' : role === 'Provider' ? 'provider' : 'customer'

  return <Badge tone={tone}>{role}</Badge>
}
