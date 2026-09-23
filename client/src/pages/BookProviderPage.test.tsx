import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthContext } from '../auth/AuthContext'
import { createBooking } from '../api/bookings'
import { getService } from '../api/catalog'
import { getPublicProvider } from '../api/providers'
import { createAuthValue, customerUser } from '../test/render'
import { BookProviderPage } from './BookProviderPage'

vi.mock('../api/bookings', () => ({
  createBooking: vi.fn(),
}))

vi.mock('../api/catalog', () => ({
  getService: vi.fn(),
}))

vi.mock('../api/providers', () => ({
  getPublicProvider: vi.fn(),
  providerPhotoUrl: () => '/photo',
}))

const mockedCreateBooking = vi.mocked(createBooking)
const mockedGetService = vi.mocked(getService)
const mockedGetPublicProvider = vi.mocked(getPublicProvider)

describe('BookProviderPage', () => {
  beforeEach(() => {
    mockedCreateBooking.mockReset()
    mockedGetService.mockReset()
    mockedGetPublicProvider.mockReset()
    mockedGetService.mockResolvedValue({
      id: 2,
      name: 'Plumbing',
      description: 'Fix leaks, installations and more.',
      categoryId: 1,
      categoryName: 'Home Services',
      hasImage: false,
    })
    mockedGetPublicProvider.mockResolvedValue({
      id: 4,
      fullName: 'Sami Provider',
      city: 'Ramallah',
      yearsOfExperience: 6,
      bio: null,
      isVerified: true,
      averageRating: 5,
      reviewCount: 1,
      services: [
        {
          id: 2,
          name: 'Plumbing',
          description: 'Fix leaks, installations and more.',
          categoryId: 1,
          categoryName: 'Home Services',
          hasImage: false,
        },
      ],
      recentReviews: [],
      hasPhoto: false,
      workingHours: [],
    })
  })

  it('sends a booking request and opens the new booking', async () => {
    mockedCreateBooking.mockResolvedValue({
      id: 15,
    } as Awaited<ReturnType<typeof createBooking>>)
    const user = userEvent.setup()

    render(
      <MemoryRouter initialEntries={['/book/4/2']}>
        <AuthContext.Provider
          value={createAuthValue({ user: customerUser(), authenticated: true })}
        >
          <Routes>
            <Route path="/book/:providerId/:serviceId" element={<BookProviderPage />} />
            <Route path="/account/bookings/:id" element={<div>Booking detail</div>} />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Book Plumbing' })).toBeInTheDocument()
    await user.type(screen.getByLabelText('Notes'), 'Kitchen tap')
    await user.click(screen.getByRole('button', { name: 'Send booking request' }))

    expect(mockedCreateBooking).toHaveBeenCalledWith(
      expect.objectContaining({
        providerProfileId: 4,
        serviceId: 2,
        notes: 'Kitchen tap',
      }),
    )
    expect(await screen.findByText('Booking detail')).toBeInTheDocument()
  })
})
