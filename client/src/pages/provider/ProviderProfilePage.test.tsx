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
      city: 'Ramallah',
      yearsOfExperience: 5,
      bio: null,
      verificationStatus: 'PendingReview',
      isSuspended: false,
      suspensionReason: null,
      verificationRejectionReason: null,
      averageRating: 0,
      reviewCount: 0,
      services: [],
    })
  })

  it('renders document review statuses and admin notes', async () => {
    mockedGetMyVerification.mockResolvedValue({
      providerProfileId: 4,
      userId: 'provider-1',
      fullName: 'Sami Provider',
      email: 'provider@khidma.test',
      city: 'Ramallah',
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
  })
})
