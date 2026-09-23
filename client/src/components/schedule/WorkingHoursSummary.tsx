import type { WorkingHour } from '../../api/types'
import { summarizeWorkingHours } from '../../utils/hours'

export function WorkingHoursSummary({ hours }: { hours: WorkingHour[] }) {
  return <p className="hours-summary">{summarizeWorkingHours(hours)}</p>
}
