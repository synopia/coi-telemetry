import { useState } from 'react'
import {
  CheckCircle,
  XCircle,
  AlertTriangle,
  Info,
  X,
} from 'lucide-react'
import { useToastStore, type Toast as ToastType } from '@/store/toastStore'
import { cn } from '@/lib/utils'

const iconMap = {
  success: CheckCircle,
  error: XCircle,
  warning: AlertTriangle,
  info: Info,
}

const colorMap = {
  success: {
    container: 'bg-success-50 border-success-200 dark:bg-success-900/20 dark:border-success-800',
    icon: 'text-success-600 dark:text-success-400',
    title: 'text-success-900 dark:text-success-200',
    message: 'text-success-700 dark:text-success-300',
    button: 'text-success-600 hover:text-success-700 dark:text-success-400 dark:hover:text-success-300',
  },
  error: {
    container: 'bg-danger-50 border-danger-200 dark:bg-danger-900/20 dark:border-danger-800',
    icon: 'text-danger-600 dark:text-danger-400',
    title: 'text-danger-900 dark:text-danger-200',
    message: 'text-danger-700 dark:text-danger-300',
    button: 'text-danger-600 hover:text-danger-700 dark:text-danger-400 dark:hover:text-danger-300',
  },
  warning: {
    container: 'bg-warning-50 border-warning-200 dark:bg-warning-900/20 dark:border-warning-800',
    icon: 'text-warning-600 dark:text-warning-500',
    title: 'text-warning-900 dark:text-warning-200',
    message: 'text-warning-700 dark:text-warning-300',
    button: 'text-warning-600 hover:text-warning-700 dark:text-warning-500 dark:hover:text-warning-400',
  },
  info: {
    container: 'bg-primary-50 border-primary-200 dark:bg-primary-900/20 dark:border-primary-800',
    icon: 'text-primary-600 dark:text-primary-400',
    title: 'text-primary-900 dark:text-primary-200',
    message: 'text-primary-700 dark:text-primary-300',
    button: 'text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300',
  },
}

interface ToastProps {
  toast: ToastType
}

function Toast({ toast }: ToastProps) {
  const removeToast = useToastStore((state) => state.removeToast)
  const [isExiting, setIsExiting] = useState(false)

  const Icon = iconMap[toast.type]
  const colors = colorMap[toast.type]

  const handleClose = () => {
    setIsExiting(true)
    setTimeout(() => {
      removeToast(toast.id)
    }, 300)
  }

  return (
    <div
      className={cn(
        'pointer-events-auto flex w-full max-w-sm overflow-hidden rounded-lg border shadow-lg',
        'transition-all duration-300 ease-in-out',
        isExiting
          ? 'translate-x-full opacity-0'
          : 'translate-x-0 opacity-100',
        colors.container
      )}
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
    >
      <div className="flex w-full items-start gap-3 p-4">
        <Icon className={cn('h-5 w-5 flex-shrink-0 mt-0.5', colors.icon)} aria-hidden="true" />
        <div className="flex-1 min-w-0">
          {toast.title && (
            <p className={cn('text-sm font-semibold', colors.title)}>
              {toast.title}
            </p>
          )}
          <p className={cn('text-sm', toast.title ? 'mt-1' : '', colors.message)}>
            {toast.message}
          </p>
        </div>
        <button
          type="button"
          onClick={handleClose}
          className={cn(
            'flex-shrink-0 inline-flex rounded-md p-1.5 transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2',
            colors.button
          )}
          aria-label="Close notification"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      </div>
    </div>
  )
}

export function ToastContainer() {
  const toasts = useToastStore((state) => state.toasts)

  return (
    <div
      className="pointer-events-none fixed inset-0 z-50 flex flex-col items-end gap-4 p-4 sm:p-6"
      aria-live="polite"
    >
      <div className="flex w-full flex-col items-end space-y-4 sm:items-end">
        {toasts.map((toast) => (
          <Toast key={toast.id} toast={toast} />
        ))}
      </div>
    </div>
  )
}

export default Toast
