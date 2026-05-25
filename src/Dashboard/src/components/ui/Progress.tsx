import { forwardRef, type HTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export type ProgressVariant = 'primary' | 'success' | 'warning' | 'danger' | 'secondary'
export type ProgressSize = 'sm' | 'md' | 'lg'

const variantStyles = {
  primary: 'bg-primary-600 dark:bg-primary-500',
  success: 'bg-success-600 dark:bg-success-500',
  warning: 'bg-warning-600 dark:bg-warning-500',
  danger: 'bg-danger-600 dark:bg-danger-500',
  secondary: 'bg-secondary-600 dark:bg-secondary-500',
}

const sizeStyles = {
  sm: 'h-1',
  md: 'h-2',
  lg: 'h-3',
}

export interface ProgressProps extends Omit<HTMLAttributes<HTMLDivElement>, 'children'> {
  value: number
  max?: number
  variant?: ProgressVariant
  size?: ProgressSize
  showLabel?: boolean
  striped?: boolean
  animated?: boolean
}

const Progress = forwardRef<HTMLDivElement, ProgressProps>(
  (
    {
      value,
      max = 100,
      variant = 'primary',
      size = 'md',
      showLabel = false,
      striped = false,
      animated = false,
      className,
      ...props
    },
    ref
  ) => {
    const percentage = Math.min(Math.max((value / max) * 100, 0), 100)

    return (
      <div className={cn('w-full', className)} {...props}>
        {showLabel && (
          <div className="mb-1 flex justify-between text-sm">
            <span className="text-secondary-700 dark:text-secondary-300">
              Progress
            </span>
            <span className="font-semibold text-secondary-900 dark:text-white">
              {Math.round(percentage)}%
            </span>
          </div>
        )}
        <div
          ref={ref}
          role="progressbar"
          aria-valuenow={value}
          aria-valuemin={0}
          aria-valuemax={max}
          className={cn(
            'w-full overflow-hidden rounded-full',
            'bg-secondary-200 dark:bg-secondary-700',
            sizeStyles[size]
          )}
        >
          <div
            className={cn(
              'h-full rounded-full transition-all duration-300 ease-in-out',
              variantStyles[variant],
              striped &&
                'bg-[length:1rem_1rem] bg-gradient-to-r from-transparent via-white/20 to-transparent bg-repeat',
              animated && striped && 'animate-[progress-stripes_1s_linear_infinite]'
            )}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>
    )
  }
)

Progress.displayName = 'Progress'

// Circular Progress Component
export interface CircularProgressProps extends Omit<HTMLAttributes<HTMLDivElement>, 'children'> {
  value: number
  max?: number
  size?: number
  strokeWidth?: number
  variant?: ProgressVariant
  showLabel?: boolean
}

export const CircularProgress = forwardRef<HTMLDivElement, CircularProgressProps>(
  (
    {
      value,
      max = 100,
      size = 120,
      strokeWidth = 8,
      variant = 'primary',
      showLabel = true,
      className,
      ...props
    },
    ref
  ) => {
    const percentage = Math.min(Math.max((value / max) * 100, 0), 100)
    const radius = (size - strokeWidth) / 2
    const circumference = radius * 2 * Math.PI
    const offset = circumference - (percentage / 100) * circumference

    const colorMap = {
      primary: 'text-primary-600 dark:text-primary-500',
      success: 'text-success-600 dark:text-success-500',
      warning: 'text-warning-600 dark:text-warning-500',
      danger: 'text-danger-600 dark:text-danger-500',
      secondary: 'text-secondary-600 dark:text-secondary-500',
    }

    return (
      <div
        ref={ref}
        className={cn('relative inline-flex items-center justify-center', className)}
        style={{ width: size, height: size }}
        {...props}
      >
        <svg
          className="transform -rotate-90"
          width={size}
          height={size}
        >
          {/* Background circle */}
          <circle
            className="text-secondary-200 dark:text-secondary-700"
            strokeWidth={strokeWidth}
            stroke="currentColor"
            fill="transparent"
            r={radius}
            cx={size / 2}
            cy={size / 2}
          />
          {/* Progress circle */}
          <circle
            className={cn('transition-all duration-300 ease-in-out', colorMap[variant])}
            strokeWidth={strokeWidth}
            strokeDasharray={circumference}
            strokeDashoffset={offset}
            strokeLinecap="round"
            stroke="currentColor"
            fill="transparent"
            r={radius}
            cx={size / 2}
            cy={size / 2}
          />
        </svg>
        {showLabel && (
          <span className="absolute text-lg font-semibold text-secondary-900 dark:text-white">
            {Math.round(percentage)}%
          </span>
        )}
      </div>
    )
  }
)

CircularProgress.displayName = 'CircularProgress'

export default Progress
