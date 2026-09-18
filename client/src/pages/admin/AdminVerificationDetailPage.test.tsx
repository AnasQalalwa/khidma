import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProviderVerification } from '../../api/types'
import { getAdminVerification } from '../../api/verification'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminVerificationDetailPage } from './AdminVerificationDetailPage'

vi.mock('../../api/verification', () => ({
  getAdminVerification: vi.fn(),
  reviewVerificationDocument: vi.fn(),
  decideProviderVerification: vi.fn(),
}))

vi.mock('../../api/admin', () => ({
  downloadAdminDocument: vi.fn(),
}))

const mockedGetAdminVerification = vi.mocked(getAdminVerification)

function verification(
  overrides: Partial<ProviderVerification> = {},
): ProviderVerification {
  return {
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
    services: ['Plumbing'],
    documents: [],
    hasApprovedDocument: false,
    ...overrides,
  }
}

describe('AdminVerificationDetailPage', () => {
  beforeEach(() => {
    mockedGetAdminVerification.mockReset()
  })

  it('disables provider approval until a document is approved', async () => {
    mockedGetAdminVerification.mockResolvedValue(
      verification({
        hasApprovedDocument: false,
        documents: [
          {
            id: 9,
            documentType: 'ProfessionalLicense',
            originalFileName: 'license.pdf',
            contentType: 'application/pdf',
            fileSizeBytes: 1200,
            uploadedAt: '2026-09-16T08:00:00Z',
            reviewStatus: 'Pending',
            reviewNote: null,
            reviewedAt: null,
          },
        ],
      }),
    )

    renderWithRouter(<AdminVerificationDetailPage />, {
      route: '/admin/verifications/4',
      path: '/admin/verifications/:providerId',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(
      await screen.findByText(
        'Approve is unavailable until at least one professional document is approved.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Approve provider' })).toBeDisabled()
  })

  it('enables provider approval after an approved document exists', async () => {
    mockedGetAdminVerification.mockResolvedValue(
      verification({
        hasApprovedDocument: true,
        documents: [
          {
            id: 9,
            documentType: 'ProfessionalLicense',
            originalFileName: 'license.pdf',
            contentType: 'application/pdf',
            fileSizeBytes: 1200,
            uploadedAt: '2026-09-16T08:00:00Z',
            reviewStatus: 'Approved',
            reviewNote: null,
            reviewedAt: '2026-09-16T09:00:00Z',
          },
        ],
      }),
    )

    renderWithRouter(<AdminVerificationDetailPage />, {
      route: '/admin/verifications/4',
      path: '/admin/verifications/:providerId',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByRole('button', { name: 'Approve provider' })).toBeEnabled()
  })

  it('requires a reason before rejecting a provider', async () => {
    mockedGetAdminVerification.mockResolvedValue(verification({ hasApprovedDocument: true }))
    const user = userEvent.setup()

    renderWithRouter(<AdminVerificationDetailPage />, {
      route: '/admin/verifications/4',
      path: '/admin/verifications/:providerId',
      auth: { user: adminUser(), authenticated: true },
    })

    await user.click(await screen.findByRole('button', { name: 'Reject provider' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Reject provider' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('A reason is required.')
  })
})
