import { SearchX } from 'lucide-react'
import { Button } from '../components/Button'
import { IconTile } from '../components/icons'

export function NotFoundPage() {
  return (
    <section className="status-page status-block">
      <IconTile icon={SearchX} accent="technology" size={24} large />
      <p className="status-code">404</p>
      <h1>Page not found</h1>
      <p className="muted">
        That route is not part of the Khidma application.
      </p>
      <div className="status-actions">
        <Button to="/">Back Home</Button>
      </div>
    </section>
  )
}
