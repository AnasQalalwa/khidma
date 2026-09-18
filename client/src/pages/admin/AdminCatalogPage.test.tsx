import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  createCategory,
  createService,
  deleteCategory,
  deleteService,
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

    mockedGetCategories.mockResolvedValue([{ id: 1, name: 'Plumbing' }])
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
    await user.click(screen.getByRole('button', { name: 'Add service' }))

    expect(await screen.findByText('Service name is required.')).toBeInTheDocument()
    expect(screen.getByText('Category is required.')).toBeInTheDocument()
    expect(screen.getByLabelText('New service')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Category')).toHaveAttribute('aria-invalid', 'true')
  })
})
