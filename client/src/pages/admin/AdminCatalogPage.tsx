import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import {
  createCategory,
  createService,
  deleteCategory,
  deleteService,
  getCatalogUsage,
  updateCategory,
  updateService,
  uploadCategoryImage,
  uploadServiceImage,
  type CatalogServiceUsage,
} from '../../api/admin'
import {
  categoryImageUrl,
  getCategories,
  getServices,
  serviceImageUrl,
  type CatalogService,
  type Category,
} from '../../api/catalog'
import { ApiError, fieldError } from '../../api/client'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { FormField } from '../../components/FormField'
import { AdminPageHeader } from '../../components/admin/AdminPageHeader'
import { categoryVisual } from '../../components/icons'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { getServiceVisual } from '../../utils/serviceVisuals'

function useObjectUrl(file: File | null) {
  const [url, setUrl] = useState<string | null>(null)
  useEffect(() => {
    if (!file) {
      setUrl(null)
      return
    }

    const next = URL.createObjectURL(file)
    setUrl(next)
    return () => URL.revokeObjectURL(next)
  }, [file])
  return url
}

function imageProblem(file: File | null, required: boolean) {
  if (!file) {
    return required ? 'Add a JPEG or PNG image.' : null
  }

  const named = /\.(jpe?g|png)$/i.test(file.name)
  const typed = file.type === 'image/jpeg' || file.type === 'image/png' || file.type === ''
  if (!named || !typed) {
    return 'Images must be a JPEG or PNG.'
  }

  if (file.size > 5 * 1024 * 1024) {
    return 'Images must be 5 MB or smaller.'
  }

  return null
}

function formatNameList(names: string[]) {
  if (names.length <= 1) {
    return names[0] ?? ''
  }
  if (names.length === 2) {
    return `${names[0]} and ${names[1]}`
  }
  if (names.length === 3) {
    return `${names[0]}, ${names[1]}, and ${names[2]}`
  }
  return `${names[0]}, ${names[1]}, and ${names.length - 2} more`
}

function categoryDeleteReason(servicesInCategory: CatalogService[]) {
  if (servicesInCategory.length === 0) {
    return null
  }

  const names = servicesInCategory
    .map((service) => service.name)
    .sort((left, right) => left.localeCompare(right, undefined, { sensitivity: 'base' }))
  const noun = names.length === 1 ? 'service' : 'services'
  const those = names.length === 1 ? 'that service' : 'those services'
  return `This category cannot be deleted because it still has ${names.length} ${noun}: ${formatNameList(names)}. Move or delete ${those} first.`
}

type DeleteTarget = { type: 'category' | 'service'; id: number; name: string }

export function AdminCatalogPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [services, setServices] = useState<CatalogService[]>([])
  const [usage, setUsage] = useState<CatalogServiceUsage[]>([])
  const [usageState, setUsageState] = useState<'loading' | 'ready' | 'failed'>('loading')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [categoryFieldErrors, setCategoryFieldErrors] = useState<Record<string, string[]>>({})
  const [editCategoryErrors, setEditCategoryErrors] = useState<Record<string, string[]>>({})
  const [serviceFieldErrors, setServiceFieldErrors] = useState<Record<string, string[]>>({})
  const [editServiceErrors, setEditServiceErrors] = useState<Record<string, string[]>>({})
  const [categoryName, setCategoryName] = useState('')
  const [categoryDescription, setCategoryDescription] = useState('')
  const [categoryImage, setCategoryImage] = useState<File | null>(null)
  const [categoryFileKey, setCategoryFileKey] = useState(0)
  const categoryPreview = useObjectUrl(categoryImage)
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [editCategoryName, setEditCategoryName] = useState('')
  const [editCategoryDescription, setEditCategoryDescription] = useState('')
  const [editCategoryImage, setEditCategoryImage] = useState<File | null>(null)
  const [editCategoryFileKey, setEditCategoryFileKey] = useState(0)
  const editCategoryPreview = useObjectUrl(editCategoryImage)
  const [serviceName, setServiceName] = useState('')
  const [serviceDescription, setServiceDescription] = useState('')
  const [serviceImage, setServiceImage] = useState<File | null>(null)
  const [serviceFileKey, setServiceFileKey] = useState(0)
  const servicePreview = useObjectUrl(serviceImage)
  const [serviceCategoryId, setServiceCategoryId] = useState('')
  const [editingService, setEditingService] = useState<CatalogService | null>(null)
  const [editServiceName, setEditServiceName] = useState('')
  const [editServiceDescription, setEditServiceDescription] = useState('')
  const [editServiceCategoryId, setEditServiceCategoryId] = useState('')
  const [editServiceImage, setEditServiceImage] = useState<File | null>(null)
  const [editServiceFileKey, setEditServiceFileKey] = useState(0)
  const editServicePreview = useObjectUrl(editServiceImage)
  const [busy, setBusy] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const categoryEditRef = useRef<HTMLLIElement>(null)
  const serviceEditRef = useRef<HTMLLIElement>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    const usageTask = getCatalogUsage()
      .then((rows) => {
        setUsage(rows)
        setUsageState('ready')
      })
      .catch(() => {
        setUsage([])
        setUsageState('failed')
      })
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
    await usageTask
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    categoryEditRef.current?.scrollIntoView?.({ block: 'nearest' })
  }, [editingCategory?.id])

  useEffect(() => {
    serviceEditRef.current?.scrollIntoView?.({ block: 'nearest' })
  }, [editingService?.id])

  function beginCategoryEdit(category: Category) {
    setEditingCategory(category)
    setEditCategoryName(category.name)
    setEditCategoryDescription(category.description.trim() || categoryVisual(category.name).description)
    setEditCategoryImage(null)
    setEditCategoryFileKey((key) => key + 1)
    setEditCategoryErrors({})
    setActionError(null)
    setSuccess(null)
  }

  function cancelCategoryEdit() {
    setEditingCategory(null)
    setEditCategoryName('')
    setEditCategoryDescription('')
    setEditCategoryImage(null)
    setEditCategoryFileKey((key) => key + 1)
    setEditCategoryErrors({})
  }

  function beginServiceEdit(service: CatalogService) {
    setEditingService(service)
    setEditServiceName(service.name)
    setEditServiceDescription(service.description.trim() || getServiceVisual(service.name).description)
    setEditServiceCategoryId(String(service.categoryId))
    setEditServiceImage(null)
    setEditServiceFileKey((key) => key + 1)
    setEditServiceErrors({})
    setActionError(null)
    setSuccess(null)
  }

  function cancelServiceEdit() {
    setEditingService(null)
    setEditServiceName('')
    setEditServiceDescription('')
    setEditServiceCategoryId('')
    setEditServiceImage(null)
    setEditServiceFileKey((key) => key + 1)
    setEditServiceErrors({})
  }

  async function handleCategorySubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    setCategoryFieldErrors({})
    const imageError = imageProblem(categoryImage, false)
    if (imageError) {
      setCategoryFieldErrors({ image: [imageError] })
      setBusy(false)
      return
    }

    try {
      const created = await createCategory({
        name: categoryName.trim(),
        description: categoryDescription.trim(),
      })
      if (categoryImage) {
        await uploadCategoryImage(created.id, categoryImage)
      }
      setSuccess('Category created.')
      setCategoryName('')
      setCategoryDescription('')
      setCategoryImage(null)
      setCategoryFileKey((key) => key + 1)
      await load()
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message)
        setCategoryFieldErrors(err.validationErrors)
      } else {
        setActionError('Could not save category.')
      }
    } finally {
      setBusy(false)
    }
  }

  async function handleCategoryEditSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || !editingCategory) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    setEditCategoryErrors({})
    const imageError = imageProblem(editCategoryImage, false)
    if (imageError) {
      setEditCategoryErrors({ image: [imageError] })
      setBusy(false)
      return
    }

    try {
      await updateCategory(editingCategory.id, {
        name: editCategoryName.trim(),
        description: editCategoryDescription.trim(),
      })
      if (editCategoryImage) {
        await uploadCategoryImage(editingCategory.id, editCategoryImage)
      }
      setSuccess('Category updated.')
      cancelCategoryEdit()
      await load()
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message)
        setEditCategoryErrors(err.validationErrors)
      } else {
        setActionError('Could not save category.')
      }
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
    setServiceFieldErrors({})
    const imageError = imageProblem(serviceImage, true)
    if (imageError) {
      setServiceFieldErrors({ image: [imageError] })
      setBusy(false)
      return
    }

    try {
      const created = await createService({
        name: serviceName.trim(),
        description: serviceDescription.trim(),
        categoryId: Number(serviceCategoryId),
      })
      if (serviceImage) {
        await uploadServiceImage(created.id, serviceImage)
      }
      setSuccess('Service created.')
      setServiceName('')
      setServiceDescription('')
      setServiceImage(null)
      setServiceFileKey((key) => key + 1)
      await load()
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message)
        setServiceFieldErrors(err.validationErrors)
      } else {
        setActionError('Could not save service.')
      }
    } finally {
      setBusy(false)
    }
  }

  async function handleServiceEditSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || !editingService) {
      return
    }

    setBusy(true)
    setActionError(null)
    setSuccess(null)
    setEditServiceErrors({})
    const imageError = imageProblem(editServiceImage, false)
    if (imageError) {
      setEditServiceErrors({ image: [imageError] })
      setBusy(false)
      return
    }

    try {
      await updateService(editingService.id, {
        name: editServiceName.trim(),
        description: editServiceDescription.trim(),
        categoryId: Number(editServiceCategoryId),
      })
      if (editServiceImage) {
        await uploadServiceImage(editingService.id, editServiceImage)
      }
      setSuccess('Service updated.')
      cancelServiceEdit()
      await load()
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(err.message)
        setEditServiceErrors(err.validationErrors)
      } else {
        setActionError('Could not save service.')
      }
    } finally {
      setBusy(false)
    }
  }

  async function confirmDelete() {
    if (!deleteTarget) {
      return
    }

    setBusy(true)
    setDeleteError(null)
    setActionError(null)
    setSuccess(null)
    try {
      if (deleteTarget.type === 'category') {
        await deleteCategory(deleteTarget.id)
      } else {
        await deleteService(deleteTarget.id)
      }
      setSuccess(`${deleteTarget.name} deleted.`)
      if (editingCategory?.id === deleteTarget.id && deleteTarget.type === 'category') {
        cancelCategoryEdit()
      }
      if (editingService?.id === deleteTarget.id && deleteTarget.type === 'service') {
        cancelServiceEdit()
      }
      setDeleteTarget(null)
      await load()
    } catch (err) {
      setDeleteError(err instanceof ApiError ? err.message : 'Could not delete this item.')
    } finally {
      setBusy(false)
    }
  }

  function openDelete(target: DeleteTarget) {
    setDeleteError(null)
    setDeleteTarget(target)
  }

  const dialog = deleteDialog(
    deleteTarget,
    services,
    usage,
    usageState,
    deleteError,
  )

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <AdminPageHeader
        title="Catalog"
        subtitle="Add the name, the short description, and the photo customers see in the catalog."
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
              <FormField label="New category" error={fieldError(categoryFieldErrors, 'name')}>
                <input
                  value={categoryName}
                  onChange={(event) => setCategoryName(event.target.value)}
                  required
                  minLength={2}
                  maxLength={80}
                />
              </FormField>
              <FormField
                label="Category description"
                hint="Shown on the home page. 8 to 160 characters."
                error={fieldError(categoryFieldErrors, 'description')}
              >
                <textarea
                  value={categoryDescription}
                  onChange={(event) => setCategoryDescription(event.target.value)}
                  required
                  minLength={8}
                  maxLength={160}
                  rows={3}
                />
              </FormField>
              <FormField
                label="Category image"
                hint="Optional JPEG or PNG, up to 5 MB. Shown on the home page."
                error={fieldError(categoryFieldErrors, 'image')}
              >
                <input
                  key={categoryFileKey}
                  type="file"
                  accept="image/jpeg,image/png,.jpg,.jpeg,.png"
                  onChange={(event) => setCategoryImage(event.target.files?.[0] ?? null)}
                />
              </FormField>
              {categoryPreview ? (
                <img className="catalog-form-preview" src={categoryPreview} alt="" />
              ) : null}
              <div className="dashboard-actions">
                <Button
                  type="submit"
                  loading={busy && deleteTarget === null && editingCategory === null && editingService === null}
                >
                  Add category
                </Button>
              </div>
            </form>
            {categories.length === 0 ? (
              <EmptyState title="No categories" description="Add a category to get started." />
            ) : (
              <ul className="plain-list">
                {categories.map((category) => {
                  const editing = editingCategory?.id === category.id
                  return (
                    <li
                      key={category.id}
                      ref={editing ? categoryEditRef : undefined}
                      className={editing ? 'plain-row is-editing' : 'plain-row'}
                    >
                      {editing ? (
                        <form
                          className="catalog-inline-form"
                          onSubmit={(event) => void handleCategoryEditSubmit(event)}
                        >
                          <h3 className="catalog-edit-title">Editing {category.name}</h3>
                          <FormField
                            label="Rename category"
                            error={fieldError(editCategoryErrors, 'name')}
                          >
                            <input
                              value={editCategoryName}
                              onChange={(event) => setEditCategoryName(event.target.value)}
                              required
                              minLength={2}
                              maxLength={80}
                            />
                          </FormField>
                          <FormField
                            label="Home page description"
                            hint="Shown on the home page. 8 to 160 characters."
                            error={fieldError(editCategoryErrors, 'description')}
                          >
                            <textarea
                              value={editCategoryDescription}
                              onChange={(event) => setEditCategoryDescription(event.target.value)}
                              required
                              minLength={8}
                              maxLength={160}
                              rows={3}
                            />
                          </FormField>
                          <FormField
                            label="Replace category image"
                            hint="Leave empty to keep the current photo. JPEG or PNG, up to 5 MB."
                            error={fieldError(editCategoryErrors, 'image')}
                          >
                            <input
                              key={editCategoryFileKey}
                              type="file"
                              accept="image/jpeg,image/png,.jpg,.jpeg,.png"
                              onChange={(event) => setEditCategoryImage(event.target.files?.[0] ?? null)}
                            />
                          </FormField>
                          {editCategoryPreview || category.hasImage ? (
                            <img
                              className="catalog-form-preview"
                              src={editCategoryPreview ?? categoryImageUrl(category.id)}
                              alt=""
                            />
                          ) : null}
                          <div className="dashboard-actions">
                            <Button type="submit" loading={busy}>
                              Save category
                            </Button>
                            <Button type="button" variant="ghost" onClick={cancelCategoryEdit}>
                              Cancel
                            </Button>
                          </div>
                        </form>
                      ) : (
                        <CatalogRow
                          title={category.name}
                          detail={category.description}
                          imageUrl={category.hasImage ? categoryImageUrl(category.id) : null}
                          onEdit={() => beginCategoryEdit(category)}
                          onDelete={() =>
                            openDelete({ type: 'category', id: category.id, name: category.name })
                          }
                        />
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
          </section>
          <section className="dashboard-panel">
            <h2>Services</h2>
            <form className="form" onSubmit={(event) => void handleServiceSubmit(event)}>
              <FormField label="Category" error={fieldError(serviceFieldErrors, 'categoryId')}>
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
              <FormField label="New service" error={fieldError(serviceFieldErrors, 'name')}>
                <input
                  value={serviceName}
                  onChange={(event) => setServiceName(event.target.value)}
                  required
                  minLength={2}
                  maxLength={80}
                />
              </FormField>
              <FormField
                label="Service description"
                hint="Shown on the catalog card. 8 to 240 characters."
                error={fieldError(serviceFieldErrors, 'description')}
              >
                <textarea
                  value={serviceDescription}
                  onChange={(event) => setServiceDescription(event.target.value)}
                  required
                  minLength={8}
                  maxLength={240}
                  rows={3}
                />
              </FormField>
              <FormField
                label="Service image"
                hint="Shown on the catalog card. JPEG or PNG, up to 5 MB."
                error={fieldError(serviceFieldErrors, 'image')}
              >
                <input
                  key={serviceFileKey}
                  type="file"
                  accept="image/jpeg,image/png,.jpg,.jpeg,.png"
                  onChange={(event) => setServiceImage(event.target.files?.[0] ?? null)}
                />
              </FormField>
              {servicePreview ? (
                <img className="catalog-form-preview" src={servicePreview} alt="" />
              ) : null}
              <div className="dashboard-actions">
                <Button
                  type="submit"
                  loading={busy && deleteTarget === null && editingService === null && editingCategory === null}
                >
                  Add service
                </Button>
              </div>
            </form>
            {services.length === 0 ? (
              <EmptyState title="No services" description="Add a service under a category." />
            ) : (
              <ul className="plain-list">
                {services.map((service) => {
                  const editing = editingService?.id === service.id
                  return (
                    <li
                      key={service.id}
                      ref={editing ? serviceEditRef : undefined}
                      className={editing ? 'plain-row is-editing' : 'plain-row'}
                    >
                      {editing ? (
                        <form
                          className="catalog-inline-form"
                          onSubmit={(event) => void handleServiceEditSubmit(event)}
                        >
                          <h3 className="catalog-edit-title">Editing {service.name}</h3>
                          <FormField
                            label="Service category"
                            error={fieldError(editServiceErrors, 'categoryId')}
                          >
                            <select
                              value={editServiceCategoryId}
                              onChange={(event) => setEditServiceCategoryId(event.target.value)}
                              required
                            >
                              {categories.map((category) => (
                                <option key={category.id} value={category.id}>
                                  {category.name}
                                </option>
                              ))}
                            </select>
                          </FormField>
                          <FormField label="Rename service" error={fieldError(editServiceErrors, 'name')}>
                            <input
                              value={editServiceName}
                              onChange={(event) => setEditServiceName(event.target.value)}
                              required
                              minLength={2}
                              maxLength={80}
                            />
                          </FormField>
                          <FormField
                            label="Catalog description"
                            hint="Shown on the catalog card. 8 to 240 characters."
                            error={fieldError(editServiceErrors, 'description')}
                          >
                            <textarea
                              value={editServiceDescription}
                              onChange={(event) => setEditServiceDescription(event.target.value)}
                              required
                              minLength={8}
                              maxLength={240}
                              rows={3}
                            />
                          </FormField>
                          <FormField
                            label="Replace service image"
                            hint="Leave empty to keep the current photo. JPEG or PNG, up to 5 MB."
                            error={fieldError(editServiceErrors, 'image')}
                          >
                            <input
                              key={editServiceFileKey}
                              type="file"
                              accept="image/jpeg,image/png,.jpg,.jpeg,.png"
                              onChange={(event) => setEditServiceImage(event.target.files?.[0] ?? null)}
                            />
                          </FormField>
                          <img
                            className="catalog-form-preview"
                            src={
                              editServicePreview ??
                              (service.hasImage
                                ? serviceImageUrl(service.id)
                                : getServiceVisual(service.name).image)
                            }
                            alt=""
                          />
                          <div className="dashboard-actions">
                            <Button type="submit" loading={busy}>
                              Save service
                            </Button>
                            <Button type="button" variant="ghost" onClick={cancelServiceEdit}>
                              Cancel
                            </Button>
                          </div>
                        </form>
                      ) : (
                        <CatalogRow
                          title={service.name}
                          detail={`${service.categoryName}${service.description ? ` · ${service.description}` : ''}`}
                          imageUrl={
                            service.hasImage
                              ? serviceImageUrl(service.id)
                              : getServiceVisual(service.name).image
                          }
                          onEdit={() => beginServiceEdit(service)}
                          onDelete={() =>
                            openDelete({ type: 'service', id: service.id, name: service.name })
                          }
                        />
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
          </section>
        </div>
      ) : null}
      <ConfirmDialog
        open={dialog !== null}
        title={dialog?.title ?? ''}
        description={dialog?.description ?? ''}
        confirmLabel="Delete"
        closeLabel={dialog?.blocked ? 'Close' : 'Cancel'}
        allowConfirm={dialog ? !dialog.blocked : false}
        tone={dialog?.tone ?? 'default'}
        danger
        busy={busy}
        onClose={() => {
          setDeleteTarget(null)
          setDeleteError(null)
        }}
        onConfirm={() => void confirmDelete()}
      />
    </WorkspaceLayout>
  )
}

function CatalogRow({
  title,
  detail,
  imageUrl,
  onEdit,
  onDelete,
}: {
  title: string
  detail: string
  imageUrl: string | null
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <>
      <div className="catalog-admin-copy">
        {imageUrl ? <img className="catalog-admin-thumb" src={imageUrl} alt="" /> : null}
        <div>
          <strong>{title}</strong>
          <p className="muted">{detail}</p>
        </div>
      </div>
      <div className="dashboard-actions">
        <Button size="sm" variant="secondary" onClick={onEdit}>
          Edit
        </Button>
        <Button size="sm" variant="ghost" onClick={onDelete}>
          Delete
        </Button>
      </div>
    </>
  )
}

function deleteDialog(
  target: DeleteTarget | null,
  services: CatalogService[],
  usage: CatalogServiceUsage[],
  usageState: 'loading' | 'ready' | 'failed',
  deleteError: string | null,
): { title: string; description: string; blocked: boolean; tone: 'default' | 'danger' } | null {
  if (!target) {
    return null
  }

  if (deleteError) {
    return {
      title: `${target.name} cannot be deleted`,
      description: deleteError,
      blocked: true,
      tone: 'danger',
    }
  }

  if (target.type === 'category') {
    const reason = categoryDeleteReason(services.filter((service) => service.categoryId === target.id))
    if (reason) {
      return {
        title: `${target.name} cannot be deleted`,
        description: reason,
        blocked: true,
        tone: 'danger',
      }
    }

    return {
      title: `Delete ${target.name}?`,
      description: 'This removes the category from the catalog.',
      blocked: false,
      tone: 'default',
    }
  }

  if (usageState === 'loading') {
    return {
      title: `Delete ${target.name}?`,
      description: 'Checking whether this service can be deleted.',
      blocked: true,
      tone: 'default',
    }
  }

  const reason = usage.find((item) => item.serviceId === target.id)?.deleteBlockReason
  if (reason) {
    return {
      title: `${target.name} cannot be deleted`,
      description: reason,
      blocked: true,
      tone: 'danger',
    }
  }

  return {
    title: `Delete ${target.name}?`,
    description: 'This removes the service from the catalog.',
    blocked: false,
    tone: 'default',
  }
}
