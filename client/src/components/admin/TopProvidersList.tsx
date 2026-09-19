import { Link } from 'react-router-dom'
import type { AdminOverview } from '../../api/types'
import { formatRating } from '../../utils/format'

export function TopProvidersList({ providers }: { providers: AdminOverview['topProviders'] }) {
  return (
    <ul className="admin-list">
      {providers.map((provider) => (
        <li key={provider.id} className="admin-list-row">
          <div>
            <Link to={`/admin/users`}>{provider.name}</Link>
            <p className="muted">
              {provider.city} · {provider.completedJobs} completed
            </p>
          </div>
          <strong>{formatRating(provider.rating, provider.reviewCount)}</strong>
        </li>
      ))}
    </ul>
  )
}
