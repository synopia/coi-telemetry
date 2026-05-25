import { type ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface TimelineItem {
  id: string
  title: string
  description?: string | ReactNode
  time?: string
  icon?: ReactNode
  iconColor?: 'primary' | 'success' | 'warning' | 'danger' | 'secondary'
  content?: ReactNode
}

export interface TimelineProps {
  items: TimelineItem[]
  variant?: 'default' | 'simple'
  className?: string
}

const iconColorStyles = {
  primary: 'bg-primary-600 dark:bg-primary-500 ring-primary-100 dark:ring-primary-900',
  success: 'bg-success-600 dark:bg-success-500 ring-success-100 dark:ring-success-900',
  warning: 'bg-warning-600 dark:bg-warning-500 ring-warning-100 dark:ring-warning-900',
  danger: 'bg-danger-600 dark:bg-danger-500 ring-danger-100 dark:ring-danger-900',
  secondary: 'bg-secondary-600 dark:bg-secondary-500 ring-secondary-100 dark:ring-secondary-900',
}

export default function Timeline({
  items,
  variant = 'default',
  className,
}: TimelineProps) {
  if (variant === 'simple') {
    return (
      <div className={cn('space-y-6', className)}>
        {items.map((item, index) => (
          <div key={item.id} className="relative flex gap-4">
            {/* Line */}
            {index !== items.length - 1 && (
              <div className="absolute left-2 top-8 bottom-0 w-px bg-secondary-200 dark:bg-secondary-700" />
            )}

            {/* Dot */}
            <div className="relative z-10 flex h-5 w-5 flex-shrink-0 items-center justify-center">
              <div className={cn(
                'h-3 w-3 rounded-full',
                item.iconColor ? iconColorStyles[item.iconColor] : 'bg-secondary-400 dark:bg-secondary-600'
              )} />
            </div>

            {/* Content */}
            <div className="flex-1 pb-8">
              <div className="flex items-baseline justify-between gap-2 mb-1">
                <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">
                  {item.title}
                </h4>
                {item.time && (
                  <time className="text-xs text-secondary-500 dark:text-secondary-400">
                    {item.time}
                  </time>
                )}
              </div>
              {item.description && (
                <div className="text-sm text-secondary-600 dark:text-secondary-400">
                  {item.description}
                </div>
              )}
              {item.content && (
                <div className="mt-2">{item.content}</div>
              )}
            </div>
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className={cn('space-y-8', className)}>
      {items.map((item, index) => (
        <div key={item.id} className="relative flex gap-6">
          {/* Line */}
          {index !== items.length - 1 && (
            <div className="absolute left-5 top-12 bottom-0 w-px bg-secondary-200 dark:bg-secondary-700" />
          )}

          {/* Icon */}
          <div className="relative z-10 flex h-10 w-10 flex-shrink-0 items-center justify-center">
            {item.icon ? (
              <div
                className={cn(
                  'flex h-10 w-10 items-center justify-center rounded-full ring-8',
                  item.iconColor
                    ? iconColorStyles[item.iconColor]
                    : 'bg-secondary-600 dark:bg-secondary-500 ring-secondary-100 dark:ring-secondary-900'
                )}
              >
                <div className="text-white">{item.icon}</div>
              </div>
            ) : (
              <div
                className={cn(
                  'h-3 w-3 rounded-full ring-8',
                  item.iconColor
                    ? iconColorStyles[item.iconColor]
                    : 'bg-secondary-600 dark:bg-secondary-500 ring-secondary-100 dark:ring-secondary-900'
                )}
              />
            )}
          </div>

          {/* Content */}
          <div className="flex-1 pb-8">
            <div className="flex items-baseline justify-between gap-2 mb-2">
              <h4 className="text-base font-semibold text-secondary-900 dark:text-white">
                {item.title}
              </h4>
              {item.time && (
                <time className="text-sm text-secondary-500 dark:text-secondary-400">
                  {item.time}
                </time>
              )}
            </div>
            {item.description && (
              <div className="text-sm text-secondary-600 dark:text-secondary-400 mb-3">
                {item.description}
              </div>
            )}
            {item.content && item.content}
          </div>
        </div>
      ))}
    </div>
  )
}
