import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getServices } from '../../api/catalog'
import { getMyProviderProfile } from '../../api/providers'
import { getMyVerification } from '../../api/verification'
import { providerUser, renderWithRouter } from '../../test/render'
import { ProviderProfilePage } from './ProviderProfilePage'

vi.mock('../../api/catalog', () => ({
  getServices: vi.fn(),
}))

vi.mock('../../api/providers', () => ({
  getMyProviderProfile: vi.fn(),
  updateMyProviderProfile: vi.fn(),
  replaceMyServices: vi.fn(),
  requestLocationChange: vi.fn(),
  requestServiceAddition: vi.fn(),
  uploadMyPhoto: vi.fn(),
  providerPhotoUrl: (id: number) => `/api/providers/${id}/photo`,
}))

vi.mock('../../components/LocationMap', () => ({
  LocationMap: ({
    city,
    onChange,
  }: {
    city: string
    latitude: number | null
    longitude: number | null
    onChange?: (next: { city: string; latitude: number; longitude: number }) => void
    readOnly?: boolean
  }) => (
    <div>
      <p>Map for {city}</p>
      <button
        type="button"
        onClick={() =>
          onChange?.({ city: 'Ramallah', latitude: 31.9, longitude: 35.2 })
        }
      >
        Use my current location
      </button>
    </div>
  ),
}))

vi.mock('../../api/verification', () => ({
  getMyVerification: vi.fn(),
  uploadVerificationDocument: vi.fn(),
  deleteVerificationDocument: vi.fn(),
  downloadMyVerificationDocument: vi.fn(),
}))

const mockedGetServices = vi.mocked(getServices)
const mockedGetMyProviderProfile = vi.mocked(getMyProviderProfile)
const mockedGetMyVerification = vi.mocked(getMyVerification)

describe('ProviderProfilePage', () => {
  beforeEach(() => {
    mockedGetServices.mockReset()
    mockedGetMyProviderProfile.mockReset()
    mockedGetMyVerification.mockReset()
    mockedGetServices.mockResolvedValue([])
    mockedGetMyProviderProfile.mockResolvedValue({
      id: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      phoneNumber: '0593333333',
      city: 'Ramallah',
      latitude: 31.9038,
      longitude: 35.2034,
      yearsOfExperience: 5,
      bio: null,
      verificationStatus: 'PendingReview',
      isSuspended: false,
      suspensionReason: null,
      verificationRejectionReason: null,
      averageRating: 0,
      reviewCount: 0,
      services: [],
      hasPhoto: false,
      canEditLocation: true,
      canEditServices: true,
      pendingChanges: [],
    })
  })

  it('renders document review statuses and admin notes', async () => {
    mockedGetMyVerification.mockResolvedValue({
      providerProfileId: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      city: 'Ramallah',
      latitude: 31.9038,
      longitude: 35.2034,
      yearsOfExperience: 5,
      bio: null,
      verificationStatus: 'PendingReview',
      verificationRejectionReason: null,
      verificationReviewedAt: null,
      isSuspended: false,
      suspensionReason: null,
      suspendedAt: null,
      averageRating: 0,
      reviewCount: 0,
      services: [],
      hasApprovedDocument: true,
      hasPhoto: false,
      pendingChanges: [],
      documents: [
        {
          id: 1,
          documentType: 'ProfessionalLicense',
          originalFileName: 'license.pdf',
          contentType: 'application/pdf',
          fileSizeBytes: 2048,
          uploadedAt: '2026-09-16T08:00:00Z',
          reviewStatus: 'Approved',
          reviewNote: null,
          reviewedAt: '2026-09-16T09:00:00Z',
          serviceId: null,
          serviceName: null,
        },
        {
          id: 2,
          documentType: 'TrainingCertificate',
          originalFileName: 'training.pdf',
          contentType: 'application/pdf',
          fileSizeBytes: 1024,
          uploadedAt: '2026-09-16T08:10:00Z',
          reviewStatus: 'Pending',
          reviewNote: null,
          reviewedAt: null,
          serviceId: null,
          serviceName: null,
        },
        {
          id: 3,
          documentType: 'PortfolioEvidence',
          originalFileName: 'portfolio.jpg',
          contentType: 'image/jpeg',
          fileSizeBytes: 4096,
          uploadedAt: '2026-09-16T08:20:00Z',
          reviewStatus: 'Rejected',
          reviewNote: 'Photo is too blurry.',
          reviewedAt: '2026-09-16T09:30:00Z',
          serviceId: null,
          serviceName: null,
        },
      ],
    })

    renderWithRouter(<ProviderProfilePage />, {
      route: '/provider/profile',
      path: '/provider/profile',
      auth: { user: providerUser(), authenticated: true },
    })

    expect(await screen.findByText('license.pdf')).toBeInTheDocument()
    expect(screen.getByText('training.pdf')).toBeInTheDocument()
    expect(screen.getByText('portfolio.jpg')).toBeInTheDocument()
    expect(screen.getByText('Approved')).toBeInTheDocument()
    expect(screen.getAllByText('Pending').length).toBeGreaterThan(0)
    expect(screen.getByText('Rejected')).toBeInTheDocument()
    expect(screen.getByText('Admin note: Photo is too blurry.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    expect(
      screen.getByText(/This does not add a service/i),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Upload for account review' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Request this service' })).not.toBeInTheDocument()
    expect(screen.getByLabelText('City')).toHaveDisplayValue('Ramallah')
    expect(screen.getByRole('button', { name: 'Use my current location' })).toBeInTheDocument()
  })

  it('locks city and services after the provider is approved', async () => {
    mockedGetMyProviderProfile.mockResolvedValue({
      id: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      phoneNumber: '0593333333',
      city: 'Ramallah',
      latitude: 31.9038,
      longitude: 35.2034,
      yearsOfExperience: 5,
      bio: 'Local cleaner',
      verificationStatus: 'Approved',
      isSuspended: false,
      suspensionReason: null,
      verificationRejectionReason: null,
      averageRating: 4.8,
      reviewCount: 12,
      services: [
        {
          id: 1,
          name: 'Home Cleaning',
          description: 'Reliable home cleaning services for a cleaner space.',
          categoryId: 3,
          categoryName: 'Cleaning',
          hasImage: false,
        },
      ],
      hasPhoto: true,
      canEditLocation: false,
      canEditServices: false,
      pendingChanges: [],
    })
    mockedGetMyVerification.mockResolvedValue({
      providerProfileId: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      city: 'Ramallah',
      latitude: 31.9038,
      longitude: 35.2034,
      yearsOfExperience: 5,
      bio: 'Local cleaner',
      verificationStatus: 'Approved',
      verificationRejectionReason: null,
      verificationReviewedAt: '2026-09-16T09:00:00Z',
      isSuspended: false,
      suspensionReason: null,
      suspendedAt: null,
      averageRating: 4.8,
      reviewCount: 12,
      services: ['Home Cleaning'],
      hasApprovedDocument: true,
      hasPhoto: true,
      pendingChanges: [],
      documents: [],
    })

    renderWithRouter(<ProviderProfilePage />, {
      route: '/provider/profile',
      path: '/provider/profile',
      auth: { user: providerUser(), authenticated: true },
    })

    expect(await screen.findByRole('button', { name: 'Request a new location' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Request this service' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Upload for account review' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('City')).not.toBeInTheDocument()
    expect(screen.getByText('Home Cleaning')).toBeInTheDocument()
  })
})
