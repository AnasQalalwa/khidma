import { useCallback, useEffect, useState } from 'react'
import { getMySchedule, saveMyWorkingHours } from '../../api/schedule'
import { ApiError } from '../../api/client'
import type { ProviderSchedule, WorkingHour } from '../../api/types'
import { Roles } from '../../auth/roles'
import { PageHeader } from '../../components/PageHeader'
import { WeeklyHoursEditor } from '../../components/schedule/WeeklyHoursEditor'
import { WeekScheduleGrid } from '../../components/schedule/WeekScheduleGrid'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { addDays, startOfWeek, toDateInput } from '../../utils/hours'

export function ProviderSchedulePage() {
  const [tab, setTab] = useState<'schedule' | 'hours'>('schedule')
  const [anchor, setAnchor] = useState(() => new Date())
  const [data, setData] = useState<ProviderSchedule | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(async () => {
    const week = startOfWeek(anchor)
    setLoading(true)
    setError(null)
    try {
      setData(await getMySchedule(toDateInput(week), toDateInput(addDays(week, 6))))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the schedule.')
    } finally {
      setLoading(false)
    }
  }, [anchor])

  useEffect(() => {
    void load()
  }, [load])

  async function saveHours(hours: WorkingHour[]) {
    setSaving(true)
    setNotice(null)
    setError(null)
    try {
      const saved = await saveMyWorkingHours(hours)
      setData((current) =>
        current
          ? { ...current, workingHours: saved.hours }
          : { workingHours: saved.hours, items: [] },
      )
      setNotice('Working hours saved.')
      setTab('schedule')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not save working hours.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Provider}>
      <PageHeader
        eyebrow="Provider"
        title="Schedule"
        description="Set the days you work, then watch accepted jobs fill the week."
      />
      <div className="catalog-pills" role="tablist" aria-label="Schedule views">
        <button
          type="button"
          className="catalog-pill"
          role="tab"
          aria-selected={tab === 'schedule'}
          onClick={() => setTab('schedule')}
        >
          Schedule
        </button>
        <button
          type="button"
          className="catalog-pill"
          role="tab"
          aria-selected={tab === 'hours'}
          onClick={() => setTab('hours')}
        >
          Working hours
        </button>
      </div>
      {notice ? <div className="alert alert-success">{notice}</div> : null}
      {error ? <ErrorState title="Schedule unavailable" description={error} onRetry={() => void load()} /> : null}
      {loading && !data ? <LoadingState label="Loading schedule" /> : null}
      {data && tab === 'schedule' ? (
        <WeekScheduleGrid
          anchor={anchor}
          hours={data.workingHours}
          items={data.items}
          onAnchorChange={setAnchor}
        />
      ) : null}
      {data && tab === 'hours' ? (
        <WeeklyHoursEditor
          key={data.workingHours.map((hour) => `${hour.dayOfWeek}-${hour.hour}`).join('|') || 'empty'}
          hours={data.workingHours}
          saving={saving}
          onSave={(hours) => void saveHours(hours)}
        />
      ) : null}
    </WorkspaceLayout>
  )
}
