import { useCallback, useEffect, useState, type FormEvent } from 'react'
import {
  createCategory,
  createService,
  deleteCategory,
  deleteService,
  updateCategory,
  updateService,
} from '../../api/admin'
import { getCategories, getServices, type CatalogService, type Category } from '../../api/catalog'
import { ApiError } from '../../api/client'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { FormField } from '../../components/FormField'
import { PageHeader } from '../../components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'

export function AdminCatalogPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [categoryName, setCategoryName] = useState('')
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [serviceName, setServiceName] = useState('')
  const [serviceCategoryId, setServiceCategoryId] = useState('')
  const [editingService, setEditingService] = useState<CatalogService | null>(null)
  const [busy, setBusy] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<
    | { type: 'category'; id: number; name: string }
    | { type: 'service'; id: number; name: string }
    | null
  >(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [categoryData, serviceData] = await Promise.all([getCategories(), getServices()])
      setCategories(categoryData)
      setServices(serviceData)
      setServiceCategoryId((current) => current || (categoryData[0] ? String(categoryData[0].id) : ''))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load catalog.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function handleCategorySubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    try {
      if (editingCategory) {
        await updateCategory(editingCategory.id, categoryName)
        setSuccess('Category updated.')
      } else {
        await createCategory(categoryName)
        setSuccess('Category created.')
      }
      setCategoryName('')
      setEditingCategory(null)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not save category.')
    } finally {
      setBusy(false)
    }
  }

  async function handleServiceSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    try {
      const payload = {
        name: serviceName,
        categoryId: Number(serviceCategoryId),
      }
      if (editingService) {
        await updateService(editingService.id, payload)
        setSuccess('Service updated.')
      } else {
        await createService(payload)
        setSuccess('Service created.')
      }
      setServiceName('')
      setEditingService(null)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not save service.')
    } finally {
      setBusy(false)
    }
  }

  async function confirmDelete() {
    if (!deleteTarget) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    try {
      if (deleteTarget.type === 'category') {
        await deleteCategory(deleteTarget.id)
      } else {
        await deleteService(deleteTarget.id)
      }
      setSuccess(`${deleteTarget.name} deleted.`)
      setDeleteTarget(null)
      await load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not delete this item.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <PageHeader
        eyebrow="Admin"
        title="Catalog"
        description="Manage categories and services. Items used by requests or providers cannot be deleted."
      />
      {success ? (
        <div className="alert alert-success" role="status">
          {success}
        </div>
      ) : null}
      {actionError ? (
        <div className="alert" role="alert">
          {actionError}
        </div>
      ) : null}
      {loading ? <LoadingState label="Loading catalog" /> : null}
      {error ? (
        <ErrorState title="Unable to load catalog" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error ? (
        <div className="admin-split">
          <section className="dashboard-panel">
            <h2>Categories</h2>
            <form className="form" onSubmit={(event) => void handleCategorySubmit(event)}>
              <FormField label={editingCategory ? 'Rename category' : 'New category'}>
                <input
                  value={categoryName}
                  onChange={(event) => setCategoryName(event.target.value)}
                  required
                />
              </FormField>
              <div className="dashboard-actions">
                <Button type="submit" loading={busy}>
                  {editingCategory ? 'Save category' : 'Add category'}
                </Button>
                {editingCategory ? (
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => {
                      setEditingCategory(null)
                      setCategoryName('')
                    }}
                  >
                    Cancel
                  </Button>
                ) : null}
              </div>
            </form>
            {categories.length === 0 ? (
              <EmptyState title="No categories" description="Add a category to get started." />
            ) : (
              <ul className="plain-list">
                {categories.map((category) => (
                  <li key={category.id} className="plain-row">
                    <strong>{category.name}</strong>
                    <div className="dashboard-actions">
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => {
                          setEditingCategory(category)
                          setCategoryName(category.name)
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() =>
                          setDeleteTarget({
                            type: 'category',
                            id: category.id,
                            name: category.name,
                          })
                        }
                      >
                        Delete
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
          <section className="dashboard-panel">
            <h2>Services</h2>
            <form className="form" onSubmit={(event) => void handleServiceSubmit(event)}>
              <FormField label="Category">
                <select
                  value={serviceCategoryId}
                  onChange={(event) => setServiceCategoryId(event.target.value)}
                  required
                >
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label={editingService ? 'Rename service' : 'New service'}>
                <input
                  value={serviceName}
                  onChange={(event) => setServiceName(event.target.value)}
                  required
                />
              </FormField>
              <div className="dashboard-actions">
                <Button type="submit" loading={busy}>
                  {editingService ? 'Save service' : 'Add service'}
                </Button>
                {editingService ? (
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => {
                      setEditingService(null)
                      setServiceName('')
                    }}
                  >
                    Cancel
                  </Button>
                ) : null}
              </div>
            </form>
            {services.length === 0 ? (
              <EmptyState title="No services" description="Add a service under a category." />
            ) : (
              <ul className="plain-list">
                {services.map((service) => (
                  <li key={service.id} className="plain-row">
                    <div>
                      <strong>{service.name}</strong>
                      <p className="muted">{service.categoryName}</p>
                    </div>
                    <div className="dashboard-actions">
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => {
                          setEditingService(service)
                          setServiceName(service.name)
                          setServiceCategoryId(String(service.categoryId))
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() =>
                          setDeleteTarget({
                            type: 'service',
                            id: service.id,
                            name: service.name,
                          })
                        }
                      >
                        Delete
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      ) : null}
      <ConfirmDialog
        open={deleteTarget !== null}
        title={`Delete ${deleteTarget?.name ?? ''}?`}
        description="This is blocked if the item is already used by services, providers, or requests."
        confirmLabel="Delete"
        danger
        busy={busy}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void confirmDelete()}
      />
    </WorkspaceLayout>
  )
}
