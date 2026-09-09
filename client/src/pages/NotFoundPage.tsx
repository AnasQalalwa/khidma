import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <section className="card">
      <h1 className="page-title">Page not found</h1>
      <p>
        <Link to="/">Return home</Link>
      </p>
    </section>
  )
}
