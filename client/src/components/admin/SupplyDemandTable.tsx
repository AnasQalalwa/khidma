import type { AdminOverview } from '../../api/types'
import { formatCount } from '../../utils/format'

export function SupplyDemandTable({ rows }: { rows: AdminOverview['supplyDemand'] }) {
  return (
    <div className="table-wrap">
      <table className="data-table">
        <thead>
          <tr>
            <th>City</th>
            <th>Service</th>
            <th>Open requests</th>
            <th>Eligible providers</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={`${row.city}-${row.serviceId}`}>
              <td>{row.city}</td>
              <td>{row.serviceName}</td>
              <td>{formatCount(row.openRequests)}</td>
              <td>
                {formatCount(row.eligibleProviders)}{' '}
                {row.eligibleProviders === 0 ? <span className="coverage-badge">No coverage</span> : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
