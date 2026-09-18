import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAuditLog, getAuditLogs, getAuditSummary } from '../../api/audit'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminAuditPage } from './AdminAuditPage'

vi.mock('../../api/audit', () => ({
  getAuditLogs: vi.fn(),
  getAuditLog: vi.fn(),
  getAuditSummary: vi.fn(),
}))

const mockedGetAuditLogs = vi.mocked(getAuditLogs)
const mockedGetAuditLog = vi.mocked(getAuditLog)
const mockedGetAuditSummary = vi.mocked(getAuditSummary)

describe('AdminAuditPage', () => {
  beforeEach(() => {
    mockedGetAuditLogs.mockReset()
    mockedGetAuditLog.mockReset()
    mockedGetAuditSummary.mockReset()
    mockedGetAuditSummary.mockResolvedValue({
      eventsToday: 4,
      deniedActions: 1,
      adminActions: 2,
      providerVerificationEvents: 1,
    })
    mockedGetAuditLogs.mockResolvedValue({
      items: [
        {
          id: 88,
          createdAt: '2026-09-16T08:00:00Z',
          actorUserId: 'admin-1',
          actorEmail: 'admin@khidma.test',
          actorRole: 'Admin',
          category: 'Verification',
          action: 'ProviderApproved',
          entityType: 'ProviderProfile',
          entityId: '4',
          outcome: 'Success',
          message: 'Provider approved',
          ipAddress: '127.0.0.1',
        },
      ],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    })
  })

  it('applies filters when the form is submitted', async () => {
    const user = userEvent.setup()

    renderWithRouter(<AdminAuditPage />, {
      route: '/admin/audit',
      path: '/admin/audit',
      auth: { user: adminUser(), authenticated: true },
    })

    expect(await screen.findAllByText('ProviderApproved')).not.toHaveLength(0)
    await user.type(screen.getByLabelText('Action'), 'ProviderApproved')
    await user.selectOptions(screen.getByLabelText('Outcome'), 'Denied')
    await user.click(screen.getByRole('button', { name: 'Apply filters' }))

    expect(mockedGetAuditLogs).toHaveBeenLastCalledWith(
      expect.objectContaining({
        action: 'ProviderApproved',
        outcome: 'Denied',
      }),
    )
  })

  it('opens a detail drawer for an audit event', async () => {
    mockedGetAuditLog.mockResolvedValue({
      id: 88,
      createdAt: '2026-09-16T08:00:00Z',
      actorUserId: 'admin-1',
      actorEmail: 'admin@khidma.test',
      actorRole: 'Admin',
      category: 'Verification',
      action: 'ProviderApproved',
      entityType: 'ProviderProfile',
      entityId: '4',
      outcome: 'Success',
      message: 'Provider approved',
      ipAddress: '127.0.0.1',
      detailsJson: '{"approvedDocumentId":9}',
      userAgent: 'vitest',
      correlationId: 'corr-1',
    })
    const user = userEvent.setup()

    renderWithRouter(<AdminAuditPage />, {
      route: '/admin/audit',
      path: '/admin/audit',
      auth: { user: adminUser(), authenticated: true },
    })

    await user.click((await screen.findAllByText('ProviderApproved'))[0])
    expect(await screen.findByText('corr-1')).toBeInTheDocument()
    expect(screen.getByText(/approvedDocumentId/)).toBeInTheDocument()
  })
})
