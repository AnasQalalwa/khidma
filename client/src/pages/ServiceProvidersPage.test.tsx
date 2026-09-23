import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getService, getServiceProviders } from '../api/catalog'
import { customerUser, renderWithRouter } from '../test/render'
import { ServiceProvidersPage } from './ServiceProvidersPage'

vi.mock('../api/catalog', () => ({
  getService: vi.fn(),
  getServiceProviders: vi.fn(),
}))

const mockedGetService = vi.mocked(getService)
const mockedGetServiceProviders = vi.mocked(getServiceProviders)

describe('ServiceProvidersPage', () => {
  beforeEach(() => {
    mockedGetService.mockReset()
    mockedGetServiceProviders.mockReset()
    mockedGetService.mockResolvedValue({
      id: 2,
      name: 'Plumbing',
      description: 'Fix leaks, installations and more.',
      categoryId: 1,
      categoryName: 'Home Services',
      hasImage: false,
    })
    mockedGetServiceProviders.mockResolvedValue([
      {
        id: 4,
        fullName: 'Sami Provider',
        city: 'Ramallah',
        yearsOfExperience: 6,
        bio: 'Fixes leaks.',
        isVerified: true,
        averageRating: 4.5,
        reviewCount: 2,
        hasPhoto: false,
        completedJobs: 8,
        workingHours: [{ dayOfWeek: 0, hour: 9 }],
      },
    ])
  })

  it('lists providers and links to booking', async () => {
    renderWithRouter(<ServiceProvidersPage />, {
      route: '/catalog/services/2',
      path: '/catalog/services/:id',
      auth: { user: customerUser(), authenticated: true },
    })

    expect(await screen.findByRole('heading', { name: 'Plumbing' })).toBeInTheDocument()
    expect(screen.getByText('Sami Provider')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Book' })).toHaveAttribute('href', '/book/4/2')
    expect(mockedGetServiceProviders).toHaveBeenCalledWith(2, 'Ramallah')
  })
})
