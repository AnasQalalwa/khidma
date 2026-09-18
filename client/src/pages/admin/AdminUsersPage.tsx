import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getAdminUsers } from '../../api/admin'
import { ApiError } from '../../api/client'
import type { AdminUser, PagedResult } from '../../api/types'
import { Roles } from '../../auth/roles'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate } from '../../utils/format'

const ROLE_FILTERS = [
  { value: '', label: 'All' },
  { value: 'Customer', label: 'Customer' },
  { value: 'Provider', label: 'Provider' },
  { value: 'Admin', label: 'Admin' },
]

export function AdminUsersPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [role, setRole] = useState('')
  const [search, setSearch] = useState('')
  const [submittedSearch, setSubmittedSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<PagedResult<AdminUser> | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(
        await getAdminUsers({
          page,
          pageSize: 12,
          role: role || undefined,
          search: submittedSearch || undefined,
        }),
      )
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load users.')
    } finally {
      setLoading(false)
    }
  }, [page, role, submittedSearch])

  useEffect(() => {
    void load()
  }, [load])

  function openUser(user: AdminUser) {
    void navigate(`/admin/users/${user.userId}`)
  }

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <PageHeader
        eyebrow="Admin"
        title="Users"
        description="Search accounts, filter by role, and inspect last login and provider status."
      />
      <form
        className="filter-bar"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          setSubmittedSearch(search.trim())
        }}
      >
        <label htmlFor="user-search">
          Search name or email
          <input
            id="user-search"
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </label>
        <label htmlFor="user-role">
          Role
          <select
            id="user-role"
            value={role}
            onChange={(event) => {
              setPage(1)
              setRole(event.target.value)
            }}
          >
            {ROLE_FILTERS.map((option) => (
              <option key={option.label} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <button className="btn btn-secondary btn-sm" type="submit">
          Search
        </button>
      </form>
      {loading ? <LoadingState label="Loading users" /> : null}
      {error ? (
        <ErrorState title="Unable to load users" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState title="No users" description="No accounts match the current filters." />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Role</th>
                  <th>Created</th>
                  <th>Last login</th>
                  <th>Provider status</th>
                  <th>Activity</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((user) => (
                  <tr
                    key={user.userId}
                    tabIndex={0}
                    onClick={() => openUser(user)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault()
                        openUser(user)
                      }
                    }}
                  >
                    <td>
                      <strong>{user.fullName}</strong>
                      <div className="muted">{user.email}</div>
                    </td>
                    <td>{user.role}</td>
                    <td>{formatDate(user.createdAt)}</td>
                    <td>{formatDate(user.lastLoginAt)}</td>
                    <td>
                      {user.verificationStatus ? (
                        <StatusBadge
                          status={user.isSuspended ? 'Suspended' : user.verificationStatus}
                        />
                      ) : (
                        '—'
                      )}
                    </td>
                    <td>{user.city ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-grid admin-card-grid">
            {data.items.map((user) => (
              <button
                type="button"
                className="click-row"
                key={`card-${user.userId}`}
                onClick={() => openUser(user)}
              >
                <strong>{user.fullName}</strong>
                <p className="muted">{user.email}</p>
                <p>
                  {user.role} · Last login {formatDate(user.lastLoginAt)}
                </p>
                {user.verificationStatus ? (
                  <StatusBadge status={user.isSuspended ? 'Suspended' : user.verificationStatus} />
                ) : null}
              </button>
            ))}
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </WorkspaceLayout>
  )
}
