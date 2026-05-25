import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react'
import { Check, Minus } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string | ReactNode
  description?: string
  error?: string
  indeterminate?: boolean
}

const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  (
    {
      label,
      description,
      error,
      indeterminate = false,
      className,
      disabled,
      ...props
    },
    ref
  ) => {
    return (
      <div className={cn('flex items-start', className)}>
        <div className="flex h-5 items-center">
          <div className="relative inline-flex">
            <input
              ref={ref}
              type="checkbox"
              disabled={disabled}
              className={cn(
                'peer h-5 w-5 cursor-pointer appearance-none rounded border-2',
                'bg-white dark:bg-secondary-800',
                'transition-all duration-150',
                'focus:outline-none focus:ring-2 focus:ring-offset-2',
                error
                  ? 'border-danger-300 dark:border-danger-600 focus:border-danger-500 focus:ring-danger-500/20'
                  : 'border-secondary-300 dark:border-secondary-600 focus:border-primary-500 focus:ring-primary-500/20',
                'checked:border-primary-600 checked:bg-primary-600 dark:checked:border-primary-500 dark:checked:bg-primary-500',
                'hover:border-primary-400 dark:hover:border-primary-400',
                disabled &&
                  'cursor-not-allowed opacity-60 hover:border-secondary-300 dark:hover:border-secondary-600'
              )}
              {...props}
            />
            <div className="pointer-events-none absolute inset-0 flex items-center justify-center text-white opacity-0 peer-checked:opacity-100">
              {indeterminate ? (
                <Minus className="h-3 w-3" aria-hidden="true" />
              ) : (
                <Check className="h-3.5 w-3.5" aria-hidden="true" />
              )}
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

Checkbox.displayName = 'Checkbox'

// CheckboxGroup for managing multiple checkboxes
export interface CheckboxGroupProps {
  label?: string
  description?: string
  error?: string
  required?: boolean
  children: ReactNode
  className?: string
}

export function CheckboxGroup({
  label,
  description,
  error,
  required,
  children,
  className,
}: CheckboxGroupProps) {
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
      <div className="space-y-2">{children}</div>
      {error && (
        <p className="mt-2 text-sm text-danger-600 dark:text-danger-400">
          {error}
        </p>
      )}
    </fieldset>
  )
}

export default Checkbox
