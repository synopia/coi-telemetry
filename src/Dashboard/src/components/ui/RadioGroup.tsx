import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface RadioProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string | ReactNode
  description?: string
}

export const Radio = forwardRef<HTMLInputElement, RadioProps>(
  ({ label, description, className, disabled, ...props }, ref) => {
    return (
      <div className={cn('flex items-start', className)}>
        <div className="flex h-5 items-center">
          <div className="relative inline-flex">
            <input
              ref={ref}
              type="radio"
              disabled={disabled}
              className={cn(
                'peer h-5 w-5 cursor-pointer appearance-none rounded-full border-2',
                'bg-white dark:bg-secondary-800',
                'transition-all duration-150',
                'focus:outline-none focus:ring-2 focus:ring-offset-2',
                'border-secondary-300 dark:border-secondary-600',
                'focus:border-primary-500 focus:ring-primary-500/20',
                'checked:border-primary-600 dark:checked:border-primary-500',
                'hover:border-primary-400 dark:hover:border-primary-400',
                disabled &&
                  'cursor-not-allowed opacity-60 hover:border-secondary-300 dark:hover:border-secondary-600'
              )}
              {...props}
            />
            <div className="pointer-events-none absolute inset-0 flex items-center justify-center opacity-0 peer-checked:opacity-100">
              <div className="h-2.5 w-2.5 rounded-full bg-primary-600 dark:bg-primary-500" />
            </div>
          </div>
        </div>
        {(label || description) && (
          <div className="ml-3 flex-1">
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
              </label>
            )}
            {description && (
              <p className="text-sm text-secondary-600 dark:text-secondary-400">
                {description}
              </p>
            )}
          </div>
        )}
      </div>
    )
  }
)

Radio.displayName = 'Radio'

export interface RadioGroupProps {
  label?: string
  description?: string
  error?: string
  required?: boolean
  children: ReactNode
  className?: string
  orientation?: 'vertical' | 'horizontal'
}

export function RadioGroup({
  label,
  description,
  error,
  required,
  children,
  className,
  orientation = 'vertical',
}: RadioGroupProps) {
  return (
    <fieldset className={className}>
      {label && (
        <legend className="mb-2 block text-sm font-medium text-secondary-900 dark:text-white">
          {label}
          {required && <span className="ml-1 text-danger-500">*</span>}
        </legend>
      )}
      {description && (
        <p className="mb-3 text-sm text-secondary-600 dark:text-secondary-400">
          {description}
        </p>
      )}
      <div
        className={cn(
          orientation === 'vertical' ? 'space-y-2' : 'flex flex-wrap gap-4'
        )}
      >
        {children}
      </div>
      {error && (
        <p className="mt-2 text-sm text-danger-600 dark:text-danger-400">
          {error}
        </p>
      )}
    </fieldset>
  )
}

export default RadioGroup
