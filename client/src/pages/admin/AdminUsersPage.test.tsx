import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAdminUsers } from '../../api/admin'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminUsersPage } from './AdminUsersPage'

vi.mock('../../api/admin', () => ({
  getAdminUsers: vi.fn(),
}))

const mockedGetAdminUsers = vi.mocked(getAdminUsers)

describe('AdminUsersPage', () => {
  beforeEach(() => {
    mockedGetAdminUsers.mockReset()
  })

  it('loads users into the table', async () => {
    mockedGetAdminUsers.mockResolvedValue({
      items: [
        {
          userId: 'customer-1',
          fullName: 'Lina Customer',
          email: 'customer@khidma.test',
          role: 'Customer',
          createdAt: '2026-01-01T10:00:00Z',
          lastLoginAt: '2026-09-16T08:00:00Z',
          providerProfileId: null,
          verificationStatus: null,
          isSuspended: null,
          averageRating: null,
          reviewCount: null,
          city: 'Ramallah',
        },
      ],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      totalPages: 1,
    })

    renderWithRouter(<AdminUsersPage />, {
      route: '/admin/users',
      path: '/admin/users',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findAllByText('Lina Customer')).not.toHaveLength(0)
    expect(screen.getAllByText('customer@khidma.test').length).toBeGreaterThan(0)
    expect(mockedGetAdminUsers).toHaveBeenCalled()
  })
})
