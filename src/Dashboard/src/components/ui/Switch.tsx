import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export type SwitchSize = 'sm' | 'md' | 'lg'

const sizeStyles = {
  sm: {
    track: 'h-5 w-9',
    thumb: 'h-4 w-4',
    translate: 'translate-x-4',
  },
  md: {
    track: 'h-6 w-11',
    thumb: 'h-5 w-5',
    translate: 'translate-x-5',
  },
  lg: {
    track: 'h-7 w-14',
    thumb: 'h-6 w-6',
    translate: 'translate-x-7',
  },
}

export interface SwitchProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'size'> {
  label?: string
  description?: string
  error?: string
  size?: SwitchSize
}

const Switch = forwardRef<HTMLInputElement, SwitchProps>(
  (
    {
      label,
      description,
      error,
      size = 'md',
      className,
      disabled,
      ...props
    },
    ref
  ) => {
    const styles = sizeStyles[size]

    return (
      <div className={cn('flex items-start gap-3', className)}>
        <label className="relative inline-flex cursor-pointer items-center group">
          <input
            ref={ref}
            type="checkbox"
            role="switch"
            disabled={disabled}
            className="sr-only"
            {...props}
          />
          <div
            className={cn(
              'relative rounded-full transition-colors duration-200',
              'bg-secondary-300 dark:bg-secondary-600',
              'group-has-[:checked]:bg-primary-600 dark:group-has-[:checked]:bg-primary-500',
              'group-has-[:focus-visible]:outline-none group-has-[:focus-visible]:ring-2 group-has-[:focus-visible]:ring-primary-500/20 group-has-[:focus-visible]:ring-offset-2',
              disabled && 'cursor-not-allowed opacity-60',
              styles.track
            )}
          >
            <div
              className={cn(
                'absolute top-0.5 left-0.5 rounded-full bg-white shadow-sm transition-transform duration-200',
                styles.thumb,
                // Conditionally apply the checked translate class based on size
                size === 'sm' && 'group-has-[:checked]:translate-x-4',
                size === 'md' && 'group-has-[:checked]:translate-x-5',
                size === 'lg' && 'group-has-[:checked]:translate-x-7'
              )}
            />
          </div>
        </label>
        {(label || description) && (
          <div className="flex-1">
            {label && (
              <label
                htmlFor={props.id}
                className={cn(
                  'block text-sm font-medium',
                  disabled
                    ? 'text-secondary-400 dark:text-secondary-600 cursor-not-allowed'
                    : 'text-secondary-900 dark:text-white cursor-pointer'
                )}
              >
                {label}
                {props.required && <span className="ml-1 text-danger-500">*</span>}
              </label>
            )}
            {description && (
              <p className="text-sm text-secondary-600 dark:text-secondary-400">
                {description}
              </p>
            )}
            {error && (
              <p className="mt-1 text-sm text-danger-600 dark:text-danger-400">
                {error}
              </p>
            )}
          </div>
        )}
      </div>
    )
  }
)

Switch.displayName = 'Switch'

export default Switch
