import { ChevronLeft, ChevronRight } from 'lucide-react'
import { IconButton } from './Button'

export function Pagination({
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  if (totalPages <= 1) {
    return totalCount > 0 ? (
      <p className="pagination-meta muted">
        {totalCount} {totalCount === 1 ? 'item' : 'items'}
      </p>
    ) : null
  }

  return (
    <nav className="pagination" aria-label="Pagination">
      <IconButton
        label="Previous page"
        icon={ChevronLeft}
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      />
      <p>
        Page {page} of {totalPages}
        <span className="muted"> · {totalCount} items</span>
      </p>
      <IconButton
        label="Next page"
        icon={ChevronRight}
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      />
    </nav>
  )
}
