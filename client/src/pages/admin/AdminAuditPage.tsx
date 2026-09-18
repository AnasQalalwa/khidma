import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Activity, ShieldAlert, ShieldCheck, Users } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { getAuditFilterOptions, getAuditLog, getAuditLogs, getAuditSummary } from '../../api/audit'
import { ApiError } from '../../api/client'
import type {
  AuditFilterOptions,
  AuditLogDetail,
  AuditLogItem,
  AuditSummary,
  PagedResult,
} from '../../api/types'
import { Roles } from '../../auth/roles'
import { AdminPageHeader } from '../../components/admin/AdminPageHeader'
import { DetailDrawer } from '../../components/DetailDrawer'
import { Pagination } from '../../components/Pagination'
import { StatCard } from '../../components/StatCard'
import { StatusBadge } from '../../components/StatusBadge'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate } from '../../utils/format'

function prettyJson(value: string | null) {
  if (!value) {
    return 'No additional details were stored for this event.'
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

function shortId(value: string | null): string | null {
  if (!value) {
    return null
  }

  return value.length <= 8 ? value : value.slice(0, 8)
}

function parseHideAuth(value: string | null): boolean {
  if (value === 'false' || value === '0') {
    return false
  }

  return true
}

export function AdminAuditPage() {
  const [params, setParams] = useSearchParams()
  const [page, setPage] = useState(1)
  const [category, setCategory] = useState(params.get('category') ?? '')
  const [action, setAction] = useState(params.get('action') ?? '')
  const [outcome, setOutcome] = useState(params.get('outcome') ?? '')
  const [actorEmail, setActorEmail] = useState(params.get('actorEmail') ?? '')
  const [search, setSearch] = useState(params.get('search') ?? '')
  const [from, setFrom] = useState(params.get('from') ?? '')
  const [to, setTo] = useState(params.get('to') ?? '')
  const [hideAuth, setHideAuth] = useState(parseHideAuth(params.get('hideAuth')))
  const [applied, setApplied] = useState({
    category: params.get('category') ?? '',
    action: params.get('action') ?? '',
    outcome: params.get('outcome') ?? '',
    actorEmail: params.get('actorEmail') ?? '',
    search: params.get('search') ?? '',
    from: params.get('from') ?? '',
    to: params.get('to') ?? '',
    hideAuth: parseHideAuth(params.get('hideAuth')),
  })
  const [options, setOptions] = useState<AuditFilterOptions | null>(null)
  const [summary, setSummary] = useState<AuditSummary | null>(null)
  const [data, setData] = useState<PagedResult<AuditLogItem> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<AuditLogDetail | null>(null)
  const [detailError, setDetailError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [counts, logs, filterOptions] = await Promise.all([
        getAuditSummary(),
        getAuditLogs({
          page,
          pageSize: 25,
          category: applied.category || undefined,
          action: applied.action || undefined,
          outcome: applied.outcome || undefined,
          actorEmail: applied.actorEmail || undefined,
          search: applied.search || undefined,
          from: applied.from || undefined,
          to: applied.to || undefined,
          hideAuth: applied.hideAuth,
        }),
        getAuditFilterOptions(),
      ])
      setSummary(counts)
      setData(logs)
      setOptions(filterOptions)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load audit logs.')
    } finally {
      setLoading(false)
    }
  }, [applied, page])

  useEffect(() => {
    void load()
  }, [load])

  function applyFilters(event: FormEvent) {
    event.preventDefault()
    const next = {
      category: category.trim(),
      action: action.trim(),
      outcome,
      actorEmail: actorEmail.trim(),
      search: search.trim(),
      from,
      to,
      hideAuth,
    }
    setPage(1)
    setApplied(next)
    const updated = new URLSearchParams()
    for (const [key, value] of Object.entries(next)) {
      if (typeof value === 'boolean') {
        if (!value) {
          updated.set(key, 'false')
        }
      } else if (value) {
        updated.set(key, value)
      }
    }
    setParams(updated)
  }

  async function openDetail(item: AuditLogItem) {
    setDetailError(null)
    try {
      setSelected(await getAuditLog(item.id))
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : 'Could not load this event.')
      setSelected({
        ...item,
        detailsJson: null,
        userAgent: null,
        correlationId: null,
      })
    }
  }

  const actionOptions = options?.actions.filter(
    (item) => !category || item.category === category,
  )

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <AdminPageHeader
        title="Audit logs"
        subtitle="Append-only security and marketplace events. Logs cannot be edited or deleted."
      />
      {summary ? (
        <div className="dash-grid">
          <StatCard title="Events today" value={summary.eventsToday} icon={Activity} accent="home" />
          <StatCard
            title="Denied actions"
            value={summary.deniedActions}
            icon={ShieldAlert}
            accent="technology"
          />
          <StatCard
            title="Admin actions"
            value={summary.adminActions}
            icon={ShieldCheck}
            accent="cleaning"
          />
          <StatCard
            title="Verification events"
            value={summary.providerVerificationEvents}
            icon={Users}
            accent="education"
          />
        </div>
      ) : null}
      <form className="filter-bar" onSubmit={applyFilters}>
        <label htmlFor="audit-from">
          From
          <input
            id="audit-from"
            type="date"
            value={from}
            onChange={(event) => setFrom(event.target.value)}
          />
        </label>
        <label htmlFor="audit-to">
          To
          <input id="audit-to" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        </label>
        <label htmlFor="audit-category">
          Category
          <select
            id="audit-category"
            value={category}
            onChange={(event) => {
              setCategory(event.target.value)
              setAction('')
            }}
          >
            <option value="">All</option>
            {options?.categories.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </label>
        <label htmlFor="audit-action">
          Action
          <select
            id="audit-action"
            value={action}
            onChange={(event) => setAction(event.target.value)}
          >
            <option value="">All</option>
            {actionOptions?.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </label>
        <label htmlFor="audit-outcome">
          Outcome
          <select
            id="audit-outcome"
            value={outcome}
            onChange={(event) => setOutcome(event.target.value)}
          >
            <option value="">All</option>
            <option value="Success">Success</option>
            <option value="Denied">Denied</option>
            <option value="Failed">Failed</option>
          </select>
        </label>
        <label htmlFor="audit-actor">
          Actor email
          <input
            id="audit-actor"
            type="search"
            value={actorEmail}
            onChange={(event) => setActorEmail(event.target.value)}
          />
        </label>
        <label htmlFor="audit-search">
          Search
          <input
            id="audit-search"
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </label>
        <label className="hide-auth" htmlFor="audit-hide-auth">
          <input
            id="audit-hide-auth"
            type="checkbox"
            checked={hideAuth}
            onChange={(event) => setHideAuth(event.target.checked)}
          />
          Hide login and logout
        </label>
        <button className="btn btn-secondary btn-sm" type="submit">
          Apply filters
        </button>
      </form>
      {loading ? <LoadingState label="Loading audit logs" /> : null}
      {error ? (
        <ErrorState title="Unable to load audit logs" description={error} onRetry={() => void load()} />
      ) : null}
      {!loading && !error && data && data.items.length === 0 ? (
        <EmptyState title="No audit events" description="No events match the current filters." />
      ) : null}
      {!loading && !error && data && data.items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>When</th>
                  <th>Actor</th>
                  <th>Summary</th>
                  <th>Outcome</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((item) => (
                  <tr
                    key={item.id}
                    tabIndex={0}
                    onClick={() => void openDetail(item)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault()
                        void openDetail(item)
                      }
                    }}
                  >
                    <td>{formatDate(item.createdAt)}</td>
                    <td>
                      {item.actorEmail ?? 'System'}
                      <div className="muted">{item.actorRole ?? '—'}</div>
                    </td>
                    <td>
                      <strong>{item.summary}</strong>
                      <div className="muted">{item.action}</div>
                    </td>
                    <td>
                      <StatusBadge status={item.outcome} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-grid admin-card-grid">
            {data.items.map((item) => (
              <button
                type="button"
                className="click-row"
                key={`card-${item.id}`}
                onClick={() => void openDetail(item)}
              >
                <strong>{item.summary}</strong>
                <p className="muted">
                  {formatDate(item.createdAt)} · {item.actorEmail ?? 'System'}
                </p>
                <StatusBadge status={item.outcome} />
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

      <DetailDrawer
        open={selected !== null}
        title={selected?.summary ?? 'Audit event'}
        onClose={() => setSelected(null)}
      >
        {selected ? (
          <>
            {detailError ? (
              <div className="alert" role="alert">
                {detailError}
              </div>
            ) : null}
            <ul className="audit-meta">
              <li>
                <strong>When</strong>
                <p>{formatDate(selected.createdAt)}</p>
              </li>
              <li>
                <strong>Actor</strong>
                <p>
                  {selected.actorEmail ?? 'System'} · {selected.actorRole ?? '—'}
                </p>
              </li>
              <li>
                <strong>Outcome</strong>
                <p>
                  <StatusBadge status={selected.outcome} />
                </p>
              </li>
              <li>
                <strong>Entity</strong>
                <p>
                  {selected.entityType ?? '—'}
                  {shortId(selected.entityId) ? ` · ${shortId(selected.entityId)}` : ''}
                </p>
              </li>
              <li>
                <strong>Action</strong>
                <p>{selected.action}</p>
              </li>
              <li>
                <strong>Message</strong>
                <p>{selected.message ?? '—'}</p>
              </li>
              <li>
                <strong>IP / user agent</strong>
                <p>
                  {selected.ipAddress ?? '—'}
                  <br />
                  {selected.userAgent ?? '—'}
                </p>
              </li>
              <li>
                <strong>Correlation</strong>
                <p>{selected.correlationId ?? '—'}</p>
              </li>
            </ul>
            <pre className="audit-json">
              <code>{prettyJson(selected.detailsJson)}</code>
            </pre>
          </>
        ) : null}
      </DetailDrawer>
    </WorkspaceLayout>
  )
}
