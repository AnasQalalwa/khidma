import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAdminOverview } from '../../api/admin'
import { getAuditLogs } from '../../api/audit'
import { ApiError } from '../../api/client'
import type { AdminOverview } from '../../api/types'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminOverviewPage } from './AdminOverviewPage'

vi.mock('../../api/admin', () => ({
  getAdminOverview: vi.fn(),
}))

vi.mock('../../api/audit', () => ({
  getAuditLogs: vi.fn(),
}))

const mockedGetAdminOverview = vi.mocked(getAdminOverview)
const mockedGetAuditLogs = vi.mocked(getAuditLogs)

function kpi(value: number, previous: number | null = 0): AdminOverview['kpis']['bookingValue'] {
  return {
    value,
    previous,
    series: [{ date: '2026-09-18T00:00:00Z', value }],
  }
}

function overview(overrides: Partial<AdminOverview> = {}): AdminOverview {
  return {
    range: '7d',
    from: '2026-09-11T00:00:00Z',
    to: '2026-09-18T00:00:00Z',
    bucket: 'day',
    kpis: {
      bookingValue: kpi(1200, 800),
      bookingsActive: { value: 3, previous: null, series: [{ date: '2026-09-18T00:00:00Z', value: 3 }] },
      bookingsCompleted: kpi(8, 5),
      conversionRate: kpi(0.4, 0.25),
      avgProviderRating: kpi(4.6, 4.2),
    },
    bookingsSeries: [
      { date: '2026-09-17T00:00:00Z', created: 2, completed: 1, cancelled: 0 },
    ],
    funnel: {
      requestsCreated: 10,
      requestsWithOffer: 7,
      booked: 4,
      completed: 2,
    },
    attention: {
      pendingVerifications: 2,
      staleOpenRequests: 1,
      overdueBookings: 0,
      suspendedProviders: 0,
    },
    supplyDemand: [
      {
        city: 'Ramallah',
        serviceId: 1,
        serviceName: 'Plumbing',
        openRequests: 3,
        eligibleProviders: 0,
      },
    ],
    topProviders: [
      {
        id: 1,
        name: 'Demo Provider',
        city: 'Ramallah',
        rating: 4.8,
        reviewCount: 6,
        completedJobs: 12,
      },
    ],
    security24h: {
      failedLogins: 1,
      deniedActions: 2,
      csrfRejections: 0,
    },
    ...overrides,
  }
}

describe('AdminOverviewPage', () => {
  beforeEach(() => {
    mockedGetAdminOverview.mockReset()
    mockedGetAuditLogs.mockReset()
    mockedGetAuditLogs.mockResolvedValue({
      items: [
        {
          id: 1,
          createdAt: '2026-09-18T10:00:00Z',
          actorUserId: 'admin-1',
          actorEmail: 'admin@khidma.test',
          actorRole: 'Admin',
          category: 'Admin',
          action: 'Admin.ProviderRejected',
          entityType: 'ProviderProfile',
          entityId: '2',
          outcome: 'Success',
          message: null,
          summary: 'Admin rejected provider Demo Provider Two',
          ipAddress: null,
        },
      ],
      page: 1,
      pageSize: 8,
      totalCount: 1,
      totalPages: 1,
    })
    mockedGetAdminOverview.mockResolvedValue(overview())
  })

  it('renders KPIs from mocked overview data', async () => {
    renderWithRouter(<AdminOverviewPage />, {
      route: '/admin?range=7d',
      path: '/admin',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('Platform overview')).toBeInTheDocument()
    expect(screen.getByText('Booking value')).toBeInTheDocument()
    expect(screen.getByText('$1,200.00')).toBeInTheDocument()
    expect(screen.getByText('Active bookings')).toBeInTheDocument()
    expect(screen.getByText('Completed bookings')).toBeInTheDocument()
    expect(screen.getByText('Conversion rate')).toBeInTheDocument()
    expect(screen.getByText('40%')).toBeInTheDocument()
    expect(screen.getByText('+15 pts')).toBeInTheDocument()
    expect(screen.getByText('Pending verifications')).toBeInTheDocument()
    expect(screen.getByText('No coverage')).toBeInTheDocument()
    expect(screen.getByText('Admin rejected provider Demo Provider Two')).toBeInTheDocument()
  })

  it('shows empty states when the overview has no activity', async () => {
    mockedGetAdminOverview.mockResolvedValue(
      overview({
        funnel: { requestsCreated: 0, requestsWithOffer: 0, booked: 0, completed: 0 },
        bookingsSeries: [],
        supplyDemand: [],
        topProviders: [],
      }),
    )
    mockedGetAuditLogs.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 8,
      totalCount: 0,
      totalPages: 0,
    })

    renderWithRouter(<AdminOverviewPage />, {
      route: '/admin',
      path: '/admin',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('No requests were created in this range.')).toBeInTheDocument()
    expect(screen.getByText('There are no open requests right now.')).toBeInTheDocument()
    expect(screen.getByText('No providers to rank yet.')).toBeInTheDocument()
    expect(screen.getByText('No recent activity.')).toBeInTheDocument()
  })

  it('shows a success line when nothing needs attention', async () => {
    mockedGetAdminOverview.mockResolvedValue(
      overview({
        attention: {
          pendingVerifications: 0,
          staleOpenRequests: 0,
          overdueBookings: 0,
          suspendedProviders: 0,
        },
      }),
    )

    renderWithRouter(<AdminOverviewPage />, {
      route: '/admin',
      path: '/admin',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('Nothing needs attention')).toBeInTheDocument()
    expect(screen.queryByText('Pending verifications')).not.toBeInTheDocument()
  })

  it('retries after an error', async () => {
    mockedGetAdminOverview
      .mockRejectedValueOnce(new ApiError('Overview failed', 500))
      .mockResolvedValueOnce(overview())
    const user = userEvent.setup()

    renderWithRouter(<AdminOverviewPage />, {
      route: '/admin',
      path: '/admin',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findAllByRole('button', { name: 'Try again' })).not.toHaveLength(0)
    await user.click(screen.getAllByRole('button', { name: 'Try again' })[0])
    expect(await screen.findByText('Booking value')).toBeInTheDocument()
    expect(mockedGetAdminOverview).toHaveBeenCalledTimes(2)
  })

  it('refetches when the range changes', async () => {
    const user = userEvent.setup()

    renderWithRouter(<AdminOverviewPage />, {
      route: '/admin?range=7d',
      path: '/admin',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findByText('Platform overview')).toBeInTheDocument()
    expect(mockedGetAdminOverview).toHaveBeenLastCalledWith('7d')
    await user.click(screen.getByRole('radio', { name: '30 days' }))
    expect(mockedGetAdminOverview).toHaveBeenLastCalledWith('30d')
  })
})
