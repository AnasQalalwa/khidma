import { useState, type FormEvent } from 'react'
import { ApiError, fieldError } from '../api/client'
import { submitOffer } from '../api/offers'
import { futureDateTimeLocal, fromDateTimeLocal } from '../utils/format'
import { Button } from './Button'
import { FormField } from './FormField'

export function OfferForm({
  requestId,
  onSubmitted,
}: {
  requestId: number
  onSubmitted: () => void
}) {
  const [price, setPrice] = useState('')
  const [message, setMessage] = useState('')
  const [estimatedDate, setEstimatedDate] = useState(futureDateTimeLocal(3))
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    setSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      await submitOffer(requestId, {
        price: Number(price),
        message,
        estimatedDate: fromDateTimeLocal(estimatedDate),
      })
      onSubmitted()
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.validationErrors)
      } else {
        setError('Could not submit the offer.')
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
      <FormField label="Price" error={fieldError(fieldErrors, 'price')}>
        <input
          type="number"
          min="0.01"
          step="0.01"
          value={price}
          onChange={(event) => setPrice(event.target.value)}
          required
        />
      </FormField>
      <FormField
        label="Estimated date"
        error={fieldError(fieldErrors, 'estimatedDate')}
      >
        <input
          type="datetime-local"
          value={estimatedDate}
          onChange={(event) => setEstimatedDate(event.target.value)}
          required
        />
      </FormField>
      <FormField label="Message" error={fieldError(fieldErrors, 'message')}>
        <textarea
          rows={4}
          value={message}
          onChange={(event) => setMessage(event.target.value)}
          required
        />
      </FormField>
      <Button type="submit" loading={submitting}>
        {submitting ? 'Submitting…' : 'Submit offer'}
      </Button>
    </form>
  )
}
