import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createReview } from '../api/bookings'
import { ReviewForm } from './ReviewForm'

vi.mock('../api/bookings', () => ({
  createReview: vi.fn(),
}))

const mockedCreateReview = vi.mocked(createReview)

describe('ReviewForm', () => {
  beforeEach(() => {
    mockedCreateReview.mockReset()
  })

  it('requires a rating from 1 to 5 before calling the API', async () => {
    render(<ReviewForm bookingId={7} onSubmitted={vi.fn()} />)

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Submit review' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Choose a rating from 1 to 5.',
    )
    expect(mockedCreateReview).not.toHaveBeenCalled()
  })

  it('submits after a rating is chosen', async () => {
    mockedCreateReview.mockResolvedValue({
      id: 1,
      rating: 5,
      comment: 'Excellent work.',
      createdAt: new Date().toISOString(),
      customerDisplayName: 'Test',
    })
    const onSubmitted = vi.fn()
    render(<ReviewForm bookingId={7} onSubmitted={onSubmitted} />)

    const user = userEvent.setup()
    await user.click(screen.getByRole('radio', { name: '5 stars' }))
    await user.type(screen.getByLabelText('Comment (optional)'), 'Excellent work.')
    await user.click(screen.getByRole('button', { name: 'Submit review' }))

    expect(mockedCreateReview).toHaveBeenCalledWith(7, {
      rating: 5,
      comment: 'Excellent work.',
    })
    expect(onSubmitted).toHaveBeenCalledTimes(1)
  })
})
