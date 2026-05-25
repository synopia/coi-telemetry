import { forwardRef, type HTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'text' | 'circular' | 'rectangular' | 'rounded'
  width?: string | number
  height?: string | number
  animation?: 'pulse' | 'wave' | 'none'
}

const Skeleton = forwardRef<HTMLDivElement, SkeletonProps>(
  (
    {
      variant = 'text',
      width,
      height,
      animation = 'pulse',
      className,
      style,
      ...props
    },
    ref
  ) => {
    const variantStyles = {
      text: 'rounded',
      circular: 'rounded-full',
      rectangular: 'rounded-none',
      rounded: 'rounded-lg',
    }

    const animationStyles = {
      pulse: 'animate-pulse',
      wave: 'animate-shimmer bg-gradient-to-r from-secondary-200 via-secondary-100 to-secondary-200 dark:from-secondary-700 dark:via-secondary-600 dark:to-secondary-700 bg-[length:200%_100%]',
      none: '',
    }

    // Default heights for different variants
    const defaultHeight = {
      text: '1em',
      circular: width || '2.5rem',
      rectangular: '2.5rem',
      rounded: '2.5rem',
    }

    return (
      <div
        ref={ref}
        role="status"
        aria-label="Loading"
        className={cn(
          'bg-secondary-200 dark:bg-secondary-700',
          variantStyles[variant],
          animation !== 'wave' && animationStyles[animation],
          animation === 'wave' && 'overflow-hidden relative',
          className
        )}
        style={{
          width: width,
          height: height || defaultHeight[variant],
          ...style,
        }}
        {...props}
      >
        {animation === 'wave' && (
          <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/20 dark:via-white/10 to-transparent animate-shimmer" />
        )}
        <span className="sr-only">Loading...</span>
      </div>
    )
  }
)

Skeleton.displayName = 'Skeleton'

// Pre-built skeleton patterns for common use cases
export function SkeletonText({ lines = 3, className }: { lines?: number; className?: string }) {
  return (
    <div className={cn('space-y-2', className)}>
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton
          key={i}
          variant="text"
          width={i === lines - 1 ? '70%' : '100%'}
          height="0.75rem"
        />
      ))}
    </div>
  )
}

export function SkeletonCard({ className }: { className?: string }) {
  return (
    <div className={cn('space-y-3', className)}>
      <Skeleton variant="rounded" height="12rem" />
      <Skeleton variant="text" width="60%" />
      <SkeletonText lines={2} />
    </div>
  )
}

export function SkeletonAvatar({ size = '2.5rem', className }: { size?: string | number; className?: string }) {
  return (
    <Skeleton
      variant="circular"
      width={size}
      height={size}
      className={className}
    />
  )
}

export function SkeletonTable({ rows = 5, columns = 4, className }: { rows?: number; columns?: number; className?: string }) {
  return (
    <div className={cn('space-y-3', className)}>
      {/* Header */}
      <div className="flex gap-4">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} variant="text" height="1rem" className="flex-1" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex gap-4">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} variant="text" height="0.875rem" className="flex-1" />
          ))}
        </div>
      ))}
    </div>
  )
}

export default Skeleton
