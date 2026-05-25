import { forwardRef, type SelectHTMLAttributes } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface SelectOption {
  value: string
  label: string
  disabled?: boolean
}

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  helperText?: string
  options: SelectOption[]
  placeholder?: string
}

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  (
    {
      label,
      error,
      helperText,
      options,
      placeholder,
      className,
      disabled,
      ...props
    },
    ref
  ) => {
    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={props.id}
            className="mb-2 block text-sm font-medium text-secondary-900 dark:text-white"
          >
            {label}
            {props.required && <span className="ml-1 text-danger-500">*</span>}
          </label>
        )}
        <div className="relative">
          <select
            ref={ref}
            disabled={disabled}
            className={cn(
              'block w-full appearance-none rounded-lg border px-4 py-2.5 pr-10',
              'text-secondary-900 dark:text-white',
              'bg-white dark:bg-secondary-800',
              'placeholder:text-secondary-400 dark:placeholder:text-secondary-500',
              'focus:outline-none focus:ring-2 focus:ring-offset-0',
              'transition-colors duration-200',
              error
                ? 'border-danger-300 dark:border-danger-600 focus:border-danger-500 focus:ring-danger-500/20'
                : 'border-secondary-300 dark:border-secondary-600 focus:border-primary-500 focus:ring-primary-500/20',
              disabled &&
                'cursor-not-allowed opacity-60 bg-secondary-50 dark:bg-secondary-900',
              className
            )}
            {...props}
          >
            {placeholder && (
              <option value="" disabled>
                {placeholder}
              </option>
            )}
            {options.map((option) => (
              <option
                key={option.value}
                value={option.value}
                disabled={option.disabled}
              >
                {option.label}
              </option>
            ))}
          </select>
          <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3">
            <ChevronDown
              className={cn(
                'h-5 w-5',
                error
                  ? 'text-danger-500'
                  : 'text-secondary-400 dark:text-secondary-500'
              )}
              aria-hidden="true"
            />
          </div>
        </div>
        {(error || helperText) && (
          <p
            className={cn(
              'mt-1.5 text-sm',
              error
                ? 'text-danger-600 dark:text-danger-400'
                : 'text-secondary-600 dark:text-secondary-400'
            )}
          >
            {error || helperText}
          </p>
        )}
      </div>
    )
  }
)

Select.displayName = 'Select'

export default Select
