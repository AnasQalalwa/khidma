import { Button } from '../components/Button'
import { PageHeader } from '../components/PageHeader'

export function ForbiddenPage() {
  return (
    <section className="status-block empty-state">
      <PageHeader
        title="Forbidden"
        description="You do not have access to that page."
      />
      <Button to="/">Return home</Button>
    </section>
  )
}
