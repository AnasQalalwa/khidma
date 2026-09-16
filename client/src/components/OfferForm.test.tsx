import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { submitOffer } from '../api/offers'
import { renderWithRouter } from '../test/render'
import { OfferForm } from './OfferForm'

vi.mock('../api/offers', () => ({
  submitOffer: vi.fn(),
}))

const mockedSubmitOffer = vi.mocked(submitOffer)

describe('OfferForm', () => {
  beforeEach(() => {
    mockedSubmitOffer.mockReset()
    mockedSubmitOffer.mockResolvedValue({
      id: 11,
      price: 90,
      message: 'I can complete this job promptly.',
      estimatedDate: new Date(Date.now() + 3 * 86400000).toISOString(),
      status: 'Pending',
      createdAt: new Date().toISOString(),
    })
  })

  it('submits the offer payload to the API', async () => {
    const onSubmitted = vi.fn()
    renderWithRouter(<OfferForm requestId={42} onSubmitted={onSubmitted} />)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Price'), '90')
    await user.type(
      screen.getByLabelText('Message'),
      'I can complete this job promptly.',
    )
    await user.click(screen.getByRole('button', { name: 'Submit offer' }))

    expect(mockedSubmitOffer).toHaveBeenCalledTimes(1)
    expect(mockedSubmitOffer).toHaveBeenCalledWith(
      42,
      expect.objectContaining({
        price: 90,
        message: 'I can complete this job promptly.',
        estimatedDate: expect.any(String),
      }),
    )
    expect(onSubmitted).toHaveBeenCalledTimes(1)
  })
})
