import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  itemsPerPage?: number
  totalItems?: number
  showFirstLast?: boolean
  showItemCount?: boolean
  className?: string
}

export default function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  itemsPerPage,
  totalItems,
  showFirstLast = true,
  showItemCount = true,
  className,
}: PaginationProps) {
  const getPageNumbers = () => {
    const delta = 2
    const range = []
    const rangeWithDots = []

    for (
      let i = Math.max(2, currentPage - delta);
      i <= Math.min(totalPages - 1, currentPage + delta);
      i++
    ) {
      range.push(i)
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, '...')
    } else {
      rangeWithDots.push(1)
    }

    rangeWithDots.push(...range)

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('...', totalPages)
    } else if (totalPages > 1) {
      rangeWithDots.push(totalPages)
    }

    return rangeWithDots
  }

  const pageNumbers = getPageNumbers()

  const handlePageClick = (page: number | string) => {
    if (typeof page === 'number' && page !== currentPage) {
      onPageChange(page)
    }
  }

  const itemStart = totalItems ? (currentPage - 1) * (itemsPerPage || 0) + 1 : 0
  const itemEnd = totalItems
    ? Math.min(currentPage * (itemsPerPage || 0), totalItems)
    : 0

  return (
    <div className={cn('flex flex-col sm:flex-row items-center justify-between gap-4', className)}>
      {/* Item count */}
      {showItemCount && totalItems && itemsPerPage && (
        <div className="text-sm text-secondary-600 dark:text-secondary-400">
          Showing <span className="font-medium text-secondary-900 dark:text-white">{itemStart}</span> to{' '}
          <span className="font-medium text-secondary-900 dark:text-white">{itemEnd}</span> of{' '}
          <span className="font-medium text-secondary-900 dark:text-white">{totalItems}</span> results
        </div>
      )}

      {/* Pagination buttons */}
      <div className="flex items-center gap-2">
        {/* First page */}
        {showFirstLast && (
          <button
            onClick={() => handlePageClick(1)}
            disabled={currentPage === 1}
            className={cn(
              'p-2 rounded-lg transition-colors',
              currentPage === 1
                ? 'text-secondary-400 dark:text-secondary-600 cursor-not-allowed'
                : 'text-secondary-700 dark:text-secondary-300 hover:bg-secondary-100 dark:hover:bg-secondary-800'
            )}
          >
            <ChevronsLeft className="w-5 h-5" />
          </button>
        )}

        {/* Previous page */}
        <button
          onClick={() => handlePageClick(currentPage - 1)}
          disabled={currentPage === 1}
          className={cn(
            'p-2 rounded-lg transition-colors',
            currentPage === 1
              ? 'text-secondary-400 dark:text-secondary-600 cursor-not-allowed'
              : 'text-secondary-700 dark:text-secondary-300 hover:bg-secondary-100 dark:hover:bg-secondary-800'
          )}
        >
          <ChevronLeft className="w-5 h-5" />
        </button>

        {/* Page numbers */}
        {pageNumbers.map((page, index) => (
          <button
            key={index}
            onClick={() => handlePageClick(page)}
            disabled={page === '...'}
            className={cn(
              'min-w-[40px] h-10 px-3 rounded-lg text-sm font-medium transition-colors',
              page === currentPage
                ? 'bg-primary-600 text-white'
                : page === '...'
                  ? 'text-secondary-400 dark:text-secondary-600 cursor-default'
                  : 'text-secondary-700 dark:text-secondary-300 hover:bg-secondary-100 dark:hover:bg-secondary-800'
            )}
          >
            {page}
          </button>
        ))}

        {/* Next page */}
        <button
          onClick={() => handlePageClick(currentPage + 1)}
          disabled={currentPage === totalPages}
          className={cn(
            'p-2 rounded-lg transition-colors',
            currentPage === totalPages
              ? 'text-secondary-400 dark:text-secondary-600 cursor-not-allowed'
              : 'text-secondary-700 dark:text-secondary-300 hover:bg-secondary-100 dark:hover:bg-secondary-800'
          )}
        >
          <ChevronRight className="w-5 h-5" />
        </button>

        {/* Last page */}
        {showFirstLast && (
          <button
            onClick={() => handlePageClick(totalPages)}
            disabled={currentPage === totalPages}
            className={cn(
              'p-2 rounded-lg transition-colors',
              currentPage === totalPages
                ? 'text-secondary-400 dark:text-secondary-600 cursor-not-allowed'
                : 'text-secondary-700 dark:text-secondary-300 hover:bg-secondary-100 dark:hover:bg-secondary-800'
            )}
          >
            <ChevronsRight className="w-5 h-5" />
          </button>
        )}
      </div>
    </div>
  )
}
