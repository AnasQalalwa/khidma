import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getAuditFilterOptions, getAuditLog, getAuditLogs, getAuditSummary } from '../../api/audit'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminAuditPage } from './AdminAuditPage'

vi.mock('../../api/audit', () => ({
  getAuditLogs: vi.fn(),
  getAuditLog: vi.fn(),
  getAuditSummary: vi.fn(),
  getAuditFilterOptions: vi.fn(),
}))

const mockedGetAuditLogs = vi.mocked(getAuditLogs)
const mockedGetAuditLog = vi.mocked(getAuditLog)
const mockedGetAuditSummary = vi.mocked(getAuditSummary)
const mockedGetAuditFilterOptions = vi.mocked(getAuditFilterOptions)

describe('AdminAuditPage', () => {
  beforeEach(() => {
    mockedGetAuditLogs.mockReset()
    mockedGetAuditLog.mockReset()
    mockedGetAuditSummary.mockReset()
    mockedGetAuditFilterOptions.mockReset()
    mockedGetAuditSummary.mockResolvedValue({
      eventsToday: 4,
      deniedActions: 1,
      adminActions: 2,
      providerVerificationEvents: 1,
    })
    mockedGetAuditFilterOptions.mockResolvedValue({
      categories: ['Admin', 'Auth'],
      actions: [
        { value: 'Admin.ProviderApproved', label: 'Provider approved', category: 'Admin' },
        { value: 'Auth.LoginFailed', label: 'Login failed', category: 'Auth' },
      ],
    })
    mockedGetAuditLogs.mockResolvedValue({
      items: [
        {
          id: 88,
          createdAt: '2026-09-16T08:00:00Z',
          actorUserId: 'admin-1',
          actorEmail: 'admin@khidma.test',
          actorRole: 'Admin',
          category: 'Admin',
          action: 'Admin.ProviderApproved',
          entityType: 'ProviderProfile',
          entityId: '4',
          outcome: 'Success',
          message: 'Provider approved',
          summary: 'Admin approved provider Demo Provider Two',
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

    expect(await screen.findAllByText('Admin approved provider Demo Provider Two')).not.toHaveLength(0)
    await user.selectOptions(screen.getByLabelText('Action'), 'Admin.ProviderApproved')
    await user.selectOptions(screen.getByLabelText('Outcome'), 'Denied')
    await user.click(screen.getByRole('button', { name: 'Apply filters' }))

    expect(mockedGetAuditLogs).toHaveBeenLastCalledWith(
      expect.objectContaining({
        action: 'Admin.ProviderApproved',
        outcome: 'Denied',
        hideAuth: true,
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
      category: 'Admin',
      action: 'Admin.ProviderApproved',
      entityType: 'ProviderProfile',
      entityId: '4',
      outcome: 'Success',
      message: 'Provider approved',
      summary: 'Admin approved provider Demo Provider Two',
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

    await user.click((await screen.findAllByText('Admin approved provider Demo Provider Two'))[0])
    expect(await screen.findByText('corr-1')).toBeInTheDocument()
    expect(screen.getByText(/approvedDocumentId/)).toBeInTheDocument()
    expect(screen.getByText(/ProviderProfile · 4/)).toBeInTheDocument()
  })
})
