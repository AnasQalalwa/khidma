import { ArrowRight, MapPin } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { RequestSummaryForProvider, ServiceRequestSummary } from '../api/types'
import { formatBudget, formatDate } from '../utils/format'
import { Button } from './Button'
import { StatusBadge } from './StatusBadge'

export function RequestCard({
  request,
  href,
  showOffers = false,
}: {
  request: ServiceRequestSummary | RequestSummaryForProvider
  href: string
  showOffers?: boolean
}) {
  const offerCount = 'offerCount' in request ? request.offerCount : undefined

  return (
    <article className="request-card">
      <div className="request-card-head">
        <div>
          <p className="meta">
            {request.categoryName} · {request.serviceName}
          </p>
          <h3>
            <Link to={href}>{request.title}</Link>
          </h3>
        </div>
        <StatusBadge status={request.status} />
      </div>
      <p className="muted request-city">
        <MapPin size={14} aria-hidden="true" /> {request.city}
      </p>
      <dl className="request-meta">
        <div>
          <dt>Preferred date</dt>
          <dd>{formatDate(request.preferredDate)}</dd>
        </div>
        <div>
          <dt>Budget</dt>
          <dd>{formatBudget(request.budgetMin, request.budgetMax)}</dd>
        </div>
        {showOffers && offerCount !== undefined ? (
          <div>
            <dt>Offers</dt>
            <dd>{offerCount}</dd>
          </div>
        ) : null}
      </dl>
      <Button to={href} variant="secondary" size="sm" iconRight={ArrowRight}>
        View
      </Button>
    </article>
  )
}
