import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getAdminOverview } from '../../api/admin'
import { getAuditLogs } from '../../api/audit'
import { ApiError } from '../../api/client'
import type { AdminOverview, AdminOverviewRange, AuditLogItem } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { AdminPageHeader } from '../../components/admin/AdminPageHeader'
import { AttentionList } from '../../components/admin/AttentionList'
import { BookingsChart } from '../../components/admin/BookingsChart'
import { FunnelBars } from '../../components/admin/FunnelBars'
import { KpiCard } from '../../components/admin/KpiCard'
import { RangeControl } from '../../components/admin/RangeControl'
import { RecentActivityList } from '../../components/admin/RecentActivityList'
import { SectionCard } from '../../components/admin/SectionCard'
import { SecurityStrip } from '../../components/admin/SecurityStrip'
import { SupplyDemandTable } from '../../components/admin/SupplyDemandTable'
import { TopProvidersList } from '../../components/admin/TopProvidersList'
import { Skeleton } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatCount, formatMoney, formatPercent } from '../../utils/format'

function parseRange(value: string | null): AdminOverviewRange {
  if (value === 'today' || value === '7d' || value === '30d') {
    return value
  }

  return '7d'
}

export function AdminOverviewPage() {
  const { user } = useAuth()
  const [params, setParams] = useSearchParams()
  const range = parseRange(params.get('range'))
  const [data, setData] = useState<AdminOverview | null>(null)
  const [recent, setRecent] = useState<AuditLogItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [overview, logs] = await Promise.all([
        getAdminOverview(range),
        getAuditLogs({ page: 1, pageSize: 8, hideAuth: true }),
      ])
      setData(overview)
      setRecent(logs.items)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load admin overview.')
      setData(null)
    } finally {
      setLoading(false)
    }
  }, [range])

  useEffect(() => {
    void load()
  }, [load])

  const status = loading ? 'loading' : error ? 'error' : 'ready'

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <AdminPageHeader
        title="Platform overview"
        subtitle={`${user?.fullName ?? 'Admin'} — marketplace health, coverage, and security. Times are UTC.`}
        actions={
          <>
            <RangeControl
              value={range}
              onChange={(next) => {
                const updated = new URLSearchParams(params)
                updated.set('range', next)
                setParams(updated)
              }}
            />
            <Button to="/admin/verifications">Review verifications</Button>
            <Button to="/admin/audit" variant="secondary">
              View audit logs
            </Button>
          </>
        }
      />

      <div className="admin-kpis">
        {status === 'loading'
          ? [0, 1, 2, 3].map((item) => <Skeleton key={item} className="admin-skeleton" />)
          : null}
        {status === 'ready' && data ? (
          <>
            <KpiCard
              label="Booking value"
              kpi={data.kpis.bookingValue}
              format={formatMoney}
              deltaKind="currency"
              sparkLabel="Booking value over the selected range"
            />
            <KpiCard
              label="Active bookings"
              kpi={data.kpis.bookingsActive}
              format={formatCount}
              deltaKind="count"
              sparkLabel="Active bookings scheduled in the selected range"
            />
            <KpiCard
              label="Completed bookings"
              kpi={data.kpis.bookingsCompleted}
              format={formatCount}
              deltaKind="count"
              sparkLabel="Completed bookings over the selected range"
            />
            <KpiCard
              label="Conversion rate"
              kpi={data.kpis.conversionRate}
              format={formatPercent}
              deltaKind="pts"
              sparkLabel="Request conversion over the selected range"
            />
          </>
        ) : null}
      </div>

      <div className="admin-grid">
        <SectionCard
          className="admin-span-8"
          title="Bookings over time"
          status={status === 'ready' && data && data.bookingsSeries.length === 0 ? 'empty' : status}
          error={error}
          onRetry={() => void load()}
          empty="No booking activity in this range."
        >
          {data ? <BookingsChart series={data.bookingsSeries} /> : null}
        </SectionCard>
        <SectionCard
          className="admin-span-4"
          title="Marketplace funnel"
          status={
            status === 'ready' && data && data.funnel.requestsCreated === 0 ? 'empty' : status
          }
          error={error}
          onRetry={() => void load()}
          empty="No requests were created in this range."
        >
          {data ? <FunnelBars funnel={data.funnel} /> : null}
        </SectionCard>

        <SectionCard
          className="admin-span-5 admin-card-start"
          title="Needs attention"
          status={status}
          error={error}
          onRetry={() => void load()}
        >
          {data ? <AttentionList attention={data.attention} /> : null}
        </SectionCard>
        <SectionCard
          className="admin-span-7"
          title="Supply vs demand"
          status={status === 'ready' && data && data.supplyDemand.length === 0 ? 'empty' : status}
          error={error}
          onRetry={() => void load()}
          empty="There are no open requests right now."
        >
          {data ? <SupplyDemandTable rows={data.supplyDemand} /> : null}
        </SectionCard>

        <SectionCard
          className="admin-span-5"
          title="Top providers"
          status={status === 'ready' && data && data.topProviders.length === 0 ? 'empty' : status}
          error={error}
          onRetry={() => void load()}
          empty="No rated providers yet."
        >
          {data ? <TopProvidersList providers={data.topProviders} /> : null}
        </SectionCard>
        <SectionCard
          className="admin-span-7"
          title="Recent activity"
          status={status === 'ready' && recent.length === 0 ? 'empty' : status}
          error={error}
          onRetry={() => void load()}
          empty="No recent activity."
          action={
            <Link className="section-link" to="/admin/audit">
              View all
            </Link>
          }
        >
          <RecentActivityList events={recent} />
        </SectionCard>

        <div className="admin-span-12">
          {status === 'loading' ? <Skeleton className="admin-skeleton" /> : null}
          {status === 'ready' && data ? <SecurityStrip security={data.security24h} /> : null}
        </div>
      </div>
    </WorkspaceLayout>
  )
}
