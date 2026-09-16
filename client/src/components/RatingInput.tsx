import { Star } from 'lucide-react'
import { Icon } from './icons'

export function RatingInput({
  value,
  onChange,
  error,
  disabled = false,
}: {
  value: number
  onChange: (value: number) => void
  error?: string
  disabled?: boolean
}) {
  return (
    <fieldset className="rating-input" disabled={disabled}>
      <legend>Rating</legend>
      <div role="radiogroup" aria-label="Rating from 1 to 5" className="rating-stars">
        {[1, 2, 3, 4, 5].map((rating) => {
          const selected = value === rating
          const filled = rating <= value
          return (
            <button
              key={rating}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={`${rating} ${rating === 1 ? 'star' : 'stars'}`}
              className={filled ? 'rating-star is-filled' : 'rating-star'}
              onClick={() => onChange(rating)}
            >
              <Icon icon={Star} size={20} />
            </button>
          )
        })}
      </div>
      {error ? (
        <p className="field-error" role="alert">
          {error}
        </p>
      ) : null}
    </fieldset>
  )
}
