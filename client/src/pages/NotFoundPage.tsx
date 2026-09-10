import { Button } from '../components/Button'
import { PageHeader } from '../components/PageHeader'

export function NotFoundPage() {
  return (
    <section className="status-block empty-state">
      <PageHeader
        title="Page not found"
        description="That route is not part of the Khidma application."
      />
      <Button to="/">Return home</Button>
    </section>
  )
}
