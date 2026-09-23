import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  createCategory,
  createService,
  deleteCategory,
  deleteService,
  getCatalogUsage,
  updateCategory,
  updateService,
} from '../../api/admin'
import { getCategories, getServices } from '../../api/catalog'
import { ApiError } from '../../api/client'
import { adminUser, renderWithRouter } from '../../test/render'
import { AdminCatalogPage } from './AdminCatalogPage'

vi.mock('../../api/catalog', () => ({
  getCategories: vi.fn(),
  getServices: vi.fn(),
}))

vi.mock('../../api/admin', () => ({
  createCategory: vi.fn(),
  createService: vi.fn(),
  deleteCategory: vi.fn(),
  deleteService: vi.fn(),
  getCatalogUsage: vi.fn(),
  updateCategory: vi.fn(),
  updateService: vi.fn(),
}))

const mockedGetCategories = vi.mocked(getCategories)
const mockedGetServices = vi.mocked(getServices)
const mockedCreateCategory = vi.mocked(createCategory)
const mockedCreateService = vi.mocked(createService)

describe('AdminCatalogPage', () => {
  beforeEach(() => {
    mockedGetCategories.mockReset()
    mockedGetServices.mockReset()
    mockedCreateCategory.mockReset()
    mockedCreateService.mockReset()
    vi.mocked(deleteCategory).mockReset()
    vi.mocked(deleteService).mockReset()
    vi.mocked(updateCategory).mockReset()
    vi.mocked(updateService).mockReset()
    vi.mocked(getCatalogUsage).mockReset()
    vi.mocked(getCatalogUsage).mockResolvedValue([])

    mockedGetCategories.mockResolvedValue([
      { id: 1, name: 'Plumbing', description: 'Pipe and drain help.', hasImage: false },
    ])
    mockedGetServices.mockResolvedValue([])
  })

  it('maps category validation errors onto the name field', async () => {
    mockedCreateCategory.mockRejectedValue(
      new ApiError('One or more validation errors occurred.', 400, {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          Name: ['Category name is required.'],
        },
      }),
    )

    renderWithRouter(<AdminCatalogPage />, {
      route: '/admin/catalog',
      path: '/admin/catalog',
      auth: { user: adminUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await user.type(await screen.findByLabelText('New category'), 'Duplicate')
    await user.type(screen.getByLabelText('Category description'), 'Outdoor seasonal help.')
    await user.click(screen.getByRole('button', { name: 'Add category' }))

    expect(await screen.findByText('Category name is required.')).toBeInTheDocument()
    expect(screen.getByLabelText('New category')).toHaveAttribute('aria-invalid', 'true')
  })

  it('maps service validation errors onto name and category fields', async () => {
    mockedCreateService.mockRejectedValue(
      new ApiError('One or more validation errors occurred.', 400, {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          Name: ['Service name is required.'],
          CategoryId: ['Category is required.'],
        },
      }),
    )

    renderWithRouter(<AdminCatalogPage />, {
      route: '/admin/catalog',
      path: '/admin/catalog',
      auth: { user: adminUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await user.type(await screen.findByLabelText('New service'), 'Leak repair')
    await user.type(screen.getByLabelText('Service description'), 'Fixes dripping taps and leaks.')
    await user.upload(
      screen.getByLabelText('Service image'),
      new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], 'tap.png', { type: 'image/png' }),
    )
    await user.click(screen.getByRole('button', { name: 'Add service' }))

    expect(await screen.findByText('Service name is required.')).toBeInTheDocument()
    expect(screen.getByText('Category is required.')).toBeInTheDocument()
    expect(screen.getByLabelText('New service')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Category')).toHaveAttribute('aria-invalid', 'true')
  })

  it('edits a category in its row', async () => {
    renderWithRouter(<AdminCatalogPage />, {
      route: '/admin/catalog',
      path: '/admin/catalog',
      auth: { user: adminUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Edit' }))

    const rename = screen.getByLabelText('Rename category')
    expect(rename).toHaveValue('Plumbing')
    expect(screen.getByLabelText('New category')).toHaveValue('')
    expect(rename.closest('li')).toContainElement(screen.getByRole('button', { name: 'Save category' }))
    expect(screen.getByRole('heading', { name: 'Editing Plumbing' })).toBeInTheDocument()
  })

  it('explains why a category with services cannot be deleted', async () => {
    mockedGetServices.mockResolvedValue([
      {
        id: 9,
        name: 'Leak repair',
        description: 'Fixes dripping taps.',
        categoryId: 1,
        categoryName: 'Plumbing',
        hasImage: false,
      },
    ])

    renderWithRouter(<AdminCatalogPage />, {
      route: '/admin/catalog',
      path: '/admin/catalog',
      auth: { user: adminUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await screen.findByText('Leak repair')
    await user.click(screen.getAllByRole('button', { name: 'Delete' })[0])

    const dialog = await screen.findByRole('dialog')
    expect(dialog).toHaveTextContent('Plumbing cannot be deleted')
    expect(dialog).toHaveTextContent(
      'This category cannot be deleted because it still has 1 service: Leak repair. Move or delete that service first.',
    )
    expect(within(dialog).queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: 'Close' })).toBeInTheDocument()
  })

  it('explains why a service in use cannot be deleted', async () => {
    mockedGetServices.mockResolvedValue([
      {
        id: 4,
        name: 'Phone Repair',
        description: 'Screen repair, battery replacement and more.',
        categoryId: 1,
        categoryName: 'Plumbing',
        hasImage: false,
      },
    ])
    vi.mocked(getCatalogUsage).mockResolvedValue([
      {
        serviceId: 4,
        providerCount: 2,
        bookingCount: 1,
        deleteBlockReason:
          'This service cannot be deleted because 2 providers offer it and 1 booking uses it.',
      },
    ])

    renderWithRouter(<AdminCatalogPage />, {
      route: '/admin/catalog',
      path: '/admin/catalog',
      auth: { user: adminUser(), authenticated: true },
    })

    const user = userEvent.setup()
    await screen.findByText('Phone Repair')
    await user.click(screen.getAllByRole('button', { name: 'Delete' })[1])

    expect(
      await screen.findByText(
        'This service cannot be deleted because 2 providers offer it and 1 booking uses it.',
      ),
    ).toBeInTheDocument()
    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveTextContent('Phone Repair cannot be deleted')
    expect(within(dialog).queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
  })
})
