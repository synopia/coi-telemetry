import { forwardRef, useEffect, useRef, type TextareaHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string
  error?: string
  helperText?: string
  resize?: 'none' | 'vertical' | 'horizontal' | 'both'
  autoResize?: boolean
  maxHeight?: number
}

const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  (
    {
      label,
      error,
      helperText,
      resize = 'vertical',
      autoResize = false,
      maxHeight = 400,
      className,
      disabled,
      ...props
    },
    ref
  ) => {
    const internalRef = useRef<HTMLTextAreaElement>(null)
    const textareaRef = (ref as any) || internalRef

    // Auto-resize functionality
    useEffect(() => {
      if (!autoResize || !textareaRef.current) return

      const textarea = textareaRef.current
      const adjustHeight = () => {
        textarea.style.height = 'auto'
        const newHeight = Math.min(textarea.scrollHeight, maxHeight)
        textarea.style.height = `${newHeight}px`
      }

      // Adjust on mount and when value changes
      adjustHeight()

      // Add event listener for input
      textarea.addEventListener('input', adjustHeight)
      return () => textarea.removeEventListener('input', adjustHeight)
    }, [autoResize, maxHeight, props.value])

    const resizeStyles = {
      none: 'resize-none',
      vertical: 'resize-y',
      horizontal: 'resize-x',
      both: 'resize',
    }

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
        <textarea
          ref={textareaRef}
          disabled={disabled}
          className={cn(
            'block w-full rounded-lg border px-4 py-2.5',
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
            autoResize ? 'resize-none overflow-hidden' : resizeStyles[resize],
            className
          )}
          {...props}
        />
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

Textarea.displayName = 'Textarea'

export default Textarea
