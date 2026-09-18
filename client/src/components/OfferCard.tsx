import { Link } from 'react-router-dom'
import type { OfferForCustomer, OfferMine, OfferSnapshot } from '../api/types'
import { formatDate, formatMoney, formatRating } from '../utils/format'
import { Button } from './Button'
import { StatusBadge } from './StatusBadge'

export function OfferCard({
  offer,
  onAccept,
  accepting = false,
}: {
  offer: OfferForCustomer
  onAccept?: (id: number) => void
  accepting?: boolean
}) {
  return (
    <article className="offer-card">
      <div className="offer-card-head">
        <div>
          <Link to={`/providers/${offer.providerProfileId}`}>
            {offer.providerDisplayName}
          </Link>
          <p className="muted">
            {formatRating(offer.providerAverageRating, offer.providerReviewCount)}
          </p>
        </div>
        <StatusBadge status={offer.status} />
      </div>
      <p className="offer-price">{formatMoney(offer.price)}</p>
      <p>{offer.message}</p>
      <p className="meta">Estimated {formatDate(offer.estimatedDate)}</p>
      {offer.canAccept && onAccept ? (
        <Button onClick={() => onAccept(offer.id)} loading={accepting}>
          Accept offer
        </Button>
      ) : null}
    </article>
  )
}

export function OfferSnapshotCard({ offer }: { offer: OfferSnapshot | OfferMine }) {
  return (
    <article className="offer-card">
      <div className="offer-card-head">
        <strong>{formatMoney(offer.price)}</strong>
        <StatusBadge status={offer.status} />
      </div>
      <p>{offer.message}</p>
      <p className="meta">Estimated {formatDate(offer.estimatedDate)}</p>
    </article>
  )
}
