import { useState, type FormEvent } from 'react'
import { ApiError, fieldError } from '../api/client'
import { createReview } from '../api/bookings'
import { Button } from './Button'
import { FormField } from './FormField'
import { RatingInput } from './RatingInput'

export function ReviewForm({
  bookingId,
  onSubmitted,
}: {
  bookingId: number
  onSubmitted: () => void
}) {
  const [rating, setRating] = useState(0)
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    if (rating < 1 || rating > 5) {
      setFieldErrors({ rating: ['Choose a rating from 1 to 5.'] })
      return
    }

    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      await createReview(bookingId, {
        rating,
        comment: comment.trim() || undefined,
      })
      onSubmitted()
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setError('Could not submit the review.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="form" onSubmit={(event) => void handleSubmit(event)}>
      {error ? (
        <div className="alert" role="alert">
          {error}
        </div>
      ) : null}
      <RatingInput
        value={rating}
        onChange={setRating}
        error={fieldError(fieldErrors, 'rating')}
        disabled={submitting}
      />
      <FormField label="Comment (optional)" error={fieldError(fieldErrors, 'comment')}>
        <textarea
          rows={4}
          value={comment}
          onChange={(event) => setComment(event.target.value)}
        />
      </FormField>
      <Button type="submit" loading={submitting}>
        {submitting ? 'Submitting…' : 'Submit review'}
      </Button>
    </form>
  )
}
