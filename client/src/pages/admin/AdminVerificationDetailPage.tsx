import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { downloadAdminDocument } from '../../api/admin'
import { ApiError } from '../../api/client'
import {
  decideProviderVerification,
  getAdminVerification,
  reviewVerificationDocument,
} from '../../api/verification'
import type { ProviderVerification, VerificationDocument } from '../../api/types'
import { Roles } from '../../auth/roles'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { DocumentList } from '../../components/DocumentList'
import { AdminPageHeader } from '../../components/admin/AdminPageHeader'
import { ReasonDialog } from '../../components/ReasonDialog'
import { StatusBadge } from '../../components/StatusBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { WorkspaceLayout } from '../../components/WorkspaceLayout'
import { formatDate, formatRating } from '../../utils/format'

export function AdminVerificationDetailPage() {
  const { providerId } = useParams()
  const id = Number(providerId)
  const [data, setData] = useState<ProviderVerification | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [rejectTarget, setRejectTarget] = useState<'provider' | VerificationDocument | null>(null)
  const [approveProviderOpen, setApproveProviderOpen] = useState(false)
  const [approveDocument, setApproveDocument] = useState<VerificationDocument | null>(null)

  const load = useCallback(async () => {
    if (Number.isNaN(id)) {
      setError('Provider not found.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)
    try {
      setData(await getAdminVerification(id))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load verification.')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  async function approveProvider() {
    if (!data) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      setData(await decideProviderVerification(data.providerProfileId, { status: 'Approved' }))
      setApproveProviderOpen(false)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not approve this provider.')
    } finally {
      setBusy(false)
    }
  }

  async function rejectProvider(reason: string) {
    if (!data) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      setData(
        await decideProviderVerification(data.providerProfileId, {
          status: 'Rejected',
          reason,
        }),
      )
      setRejectTarget(null)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not reject this provider.')
    } finally {
      setBusy(false)
    }
  }

  async function approveDoc() {
    if (!approveDocument) {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      await reviewVerificationDocument(approveDocument.id, { status: 'Approved' })
      await load()
      setApproveDocument(null)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not approve this document.')
    } finally {
      setBusy(false)
    }
  }

  async function rejectDoc(reason: string) {
    if (!rejectTarget || rejectTarget === 'provider') {
      return
    }

    setBusy(true)
    setActionError(null)
    try {
      await reviewVerificationDocument(rejectTarget.id, { status: 'Rejected', note: reason })
      await load()
      setRejectTarget(null)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not reject this document.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <WorkspaceLayout role={Roles.Admin}>
      <AdminPageHeader
        title={data?.fullName ?? 'Verification review'}
        subtitle="Review professional proof, then approve or reject the provider account."
        actions={
          <Button to="/admin/verifications" variant="secondary">
            Back to queue
          </Button>
        }
      />
      {loading ? <LoadingState label="Loading verification" /> : null}
      {error ? (
        <ErrorState
          title="Unable to load verification"
          description={error}
          onRetry={() => void load()}
        />
      ) : null}
      {actionError ? (
        <div className="alert" role="alert">
          {actionError}
        </div>
      ) : null}
      {!loading && !error && data ? (
        <section className="dashboard-panel">
          <div className="inline-actions">
            <StatusBadge status={data.isSuspended ? 'Suspended' : data.verificationStatus} />
            <p className="muted">{data.email}</p>
          </div>
          <p>
            {data.city} · {data.yearsOfExperience} years ·{' '}
            {formatRating(data.averageRating, data.reviewCount)}
          </p>
          <p className="muted">{data.services.join(', ') || 'No services listed'}</p>
          {data.verificationRejectionReason ? (
            <p>Rejection reason: {data.verificationRejectionReason}</p>
          ) : null}
          {data.verificationReviewedAt ? (
            <p className="muted">Last reviewed {formatDate(data.verificationReviewedAt)}</p>
          ) : null}

          <h2>Documents</h2>
          <DocumentList
            documents={data.documents}
            empty="This provider has not uploaded professional proof yet."
            renderActions={(document) => (
              <>
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() =>
                    void downloadAdminDocument(document.id, document.originalFileName)
                  }
                >
                  View / download
                </Button>
                {document.reviewStatus === 'Pending' ? (
                  <>
                    <Button size="sm" onClick={() => setApproveDocument(document)}>
                      Approve document
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => setRejectTarget(document)}
                    >
                      Reject document
                    </Button>
                  </>
                ) : null}
              </>
            )}
          />

          <h2>Provider decision</h2>
          {!data.hasApprovedDocument ? (
            <p className="muted" role="note">
              Approve is unavailable until at least one professional document is approved.
            </p>
          ) : null}
          <div className="inline-actions">
            <Button
              disabled={!data.hasApprovedDocument || data.verificationStatus === 'Approved'}
              onClick={() => setApproveProviderOpen(true)}
            >
              Approve provider
            </Button>
            <Button
              variant="secondary"
              onClick={() => setRejectTarget('provider')}
              disabled={data.verificationStatus === 'Rejected'}
            >
              Reject provider
            </Button>
          </div>
        </section>
      ) : null}

      <ConfirmDialog
        open={approveProviderOpen}
        title="Approve this provider?"
        description="They will become eligible for matching work if they are not suspended."
        confirmLabel="Approve provider"
        busy={busy}
        onConfirm={() => void approveProvider()}
        onClose={() => setApproveProviderOpen(false)}
      />
      <ConfirmDialog
        open={approveDocument !== null}
        title="Approve this document?"
        description="Approved professional proof is required before the provider account can be approved."
        confirmLabel="Approve document"
        busy={busy}
        onConfirm={() => void approveDoc()}
        onClose={() => setApproveDocument(null)}
      />
      <ReasonDialog
        open={rejectTarget !== null}
        title={rejectTarget === 'provider' ? 'Reject this provider?' : 'Reject this document?'}
        description={
          rejectTarget === 'provider'
            ? 'The provider will stay ineligible until they resubmit and an admin approves them.'
            : 'A note is required so the provider knows what to correct.'
        }
        confirmLabel={rejectTarget === 'provider' ? 'Reject provider' : 'Reject document'}
        label="Reason"
        danger
        busy={busy}
        onConfirm={(reason) =>
          void (rejectTarget === 'provider' ? rejectProvider(reason) : rejectDoc(reason))
        }
        onClose={() => setRejectTarget(null)}
      />
    </WorkspaceLayout>
  )
}
