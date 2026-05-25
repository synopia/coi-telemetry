import { forwardRef, type HTMLAttributes, type ReactNode } from 'react'
import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-react'
import { cn } from '@/lib/utils'

export type AlertVariant = 'success' | 'error' | 'warning' | 'info'

const iconMap = {
  success: CheckCircle,
  error: XCircle,
  warning: AlertTriangle,
  info: Info,
}

const variantStyles = {
  success: {
    container: 'bg-success-50 border-success-200 dark:bg-success-900/20 dark:border-success-800',
    icon: 'text-success-600 dark:text-success-400',
    title: 'text-success-900 dark:text-success-200',
    description: 'text-success-700 dark:text-success-300',
  },
  error: {
    container: 'bg-danger-50 border-danger-200 dark:bg-danger-900/20 dark:border-danger-800',
    icon: 'text-danger-600 dark:text-danger-400',
    title: 'text-danger-900 dark:text-danger-200',
    description: 'text-danger-700 dark:text-danger-300',
  },
  warning: {
    container: 'bg-warning-50 border-warning-200 dark:bg-warning-900/20 dark:border-warning-800',
    icon: 'text-warning-600 dark:text-warning-500',
    title: 'text-warning-900 dark:text-warning-200',
    description: 'text-warning-700 dark:text-warning-300',
  },
  info: {
    container: 'bg-primary-50 border-primary-200 dark:bg-primary-900/20 dark:border-primary-800',
    icon: 'text-primary-600 dark:text-primary-400',
    title: 'text-primary-900 dark:text-primary-200',
    description: 'text-primary-700 dark:text-primary-300',
  },
}

export interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  variant?: AlertVariant
  title?: string
  description?: string | ReactNode
  icon?: ReactNode
  showIcon?: boolean
  dismissible?: boolean
  onDismiss?: () => void
}

const Alert = forwardRef<HTMLDivElement, AlertProps>(
  (
    {
      variant = 'info',
      title,
      description,
      icon,
      showIcon = true,
      dismissible = false,
      onDismiss,
      className,
      children,
      ...props
    },
    ref
  ) => {
    const Icon = iconMap[variant]
    const styles = variantStyles[variant]

    return (
      <div
        ref={ref}
        role="alert"
        className={cn(
          'relative flex gap-3 rounded-lg border p-4',
          styles.container,
          className
        )}
        {...props}
      >
        {showIcon && (
          <div className="flex-shrink-0">
            {icon || <Icon className={cn('h-5 w-5', styles.icon)} aria-hidden="true" />}
          </div>
        )}
        <div className="flex-1 min-w-0">
          {title && (
            <h5 className={cn('text-sm font-semibold mb-1', styles.title)}>
              {title}
            </h5>
          )}
          {(description || children) && (
            <div className={cn('text-sm', styles.description)}>
              {description || children}
            </div>
          )}
        </div>
        {dismissible && onDismiss && (
          <button
            type="button"
            onClick={onDismiss}
            className={cn(
              'flex-shrink-0 inline-flex rounded-md p-1 transition-colors',
              'hover:bg-black/5 dark:hover:bg-white/5',
              styles.icon
            )}
            aria-label="Dismiss alert"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        )}
      </div>
    )
  }
)

Alert.displayName = 'Alert'

export default Alert
