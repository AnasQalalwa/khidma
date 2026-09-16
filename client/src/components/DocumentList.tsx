import type { ReactNode } from 'react'
import type { VerificationDocument } from '../api/types'
import { formatBytes, formatDate, statusLabel } from '../utils/format'
import { StatusBadge } from './StatusBadge'

export function DocumentList({
  documents,
  empty,
  renderActions,
}: {
  documents: VerificationDocument[]
  empty: string
  renderActions?: (document: VerificationDocument) => ReactNode
}) {
  if (documents.length === 0) {
    return <p className="muted">{empty}</p>
  }

  return (
    <ul className="doc-list">
      {documents.map((document) => (
        <li className="doc-card" key={document.id}>
          <div className="request-card-head">
            <div>
              <strong>{document.originalFileName}</strong>
              <p className="muted">
                {statusLabel(document.documentType)} · {formatBytes(document.fileSizeBytes)} ·{' '}
                {formatDate(document.uploadedAt)}
              </p>
            </div>
            <StatusBadge status={document.reviewStatus} />
          </div>
          {document.reviewNote ? <p>Admin note: {document.reviewNote}</p> : null}
          {renderActions ? <div className="doc-actions">{renderActions(document)}</div> : null}
        </li>
      ))}
    </ul>
  )
}
