import { Link } from 'react-router-dom'

export function ForbiddenPage() {
  return (
    <section className="card">
      <h1 className="page-title">Forbidden</h1>
      <p>You do not have access to that page.</p>
      <p>
        <Link to="/">Return home</Link>
      </p>
    </section>
  )
}
