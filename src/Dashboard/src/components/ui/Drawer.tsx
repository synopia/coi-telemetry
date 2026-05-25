import { Fragment, type ReactNode } from 'react'
import { Dialog, DialogPanel, DialogTitle, Transition, TransitionChild } from '@headlessui/react'
import { X } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface DrawerProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  position?: 'left' | 'right' | 'top' | 'bottom'
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full'
  showCloseButton?: boolean
  closeOnOverlayClick?: boolean
}

export default function Drawer({
  isOpen,
  onClose,
  title,
  description,
  children,
  position = 'right',
  size = 'md',
  showCloseButton = true,
  closeOnOverlayClick = true,
}: DrawerProps) {
  const sizeStyles = {
    left: {
      sm: 'max-w-xs',
      md: 'max-w-md',
      lg: 'max-w-lg',
      xl: 'max-w-xl',
      full: 'max-w-full w-screen',
    },
    right: {
      sm: 'max-w-xs',
      md: 'max-w-md',
      lg: 'max-w-lg',
      xl: 'max-w-xl',
      full: 'max-w-full w-screen',
    },
    top: {
      sm: 'max-h-[25vh]',
      md: 'max-h-[50vh]',
      lg: 'max-h-[75vh]',
      xl: 'max-h-[90vh]',
      full: 'max-h-screen h-screen',
    },
    bottom: {
      sm: 'max-h-[25vh]',
      md: 'max-h-[50vh]',
      lg: 'max-h-[75vh]',
      xl: 'max-h-[90vh]',
      full: 'max-h-screen h-screen',
    },
  }

  const positionStyles = {
    left: 'inset-y-0 left-0',
    right: 'inset-y-0 right-0',
    top: 'inset-x-0 top-0',
    bottom: 'inset-x-0 bottom-0',
  }

  const slideDirections = {
    left: {
      enterFrom: '-translate-x-full',
      enterTo: 'translate-x-0',
      leaveFrom: 'translate-x-0',
      leaveTo: '-translate-x-full',
    },
    right: {
      enterFrom: 'translate-x-full',
      enterTo: 'translate-x-0',
      leaveFrom: 'translate-x-0',
      leaveTo: 'translate-x-full',
    },
    top: {
      enterFrom: '-translate-y-full',
      enterTo: 'translate-y-0',
      leaveFrom: 'translate-y-0',
      leaveTo: '-translate-y-full',
    },
    bottom: {
      enterFrom: 'translate-y-full',
      enterTo: 'translate-y-0',
      leaveFrom: 'translate-y-0',
      leaveTo: 'translate-y-full',
    },
  }

  const slide = slideDirections[position]

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog
        as="div"
        className="relative z-50"
        onClose={closeOnOverlayClick ? onClose : () => {}}
      >
        {/* Backdrop */}
        <TransitionChild
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black/50 backdrop-blur-sm" />
        </TransitionChild>

        {/* Drawer Panel */}
        <div className="fixed inset-0 overflow-hidden">
          <div className={cn('absolute', positionStyles[position])}>
            <TransitionChild
              as={Fragment}
              enter="transform transition ease-in-out duration-300"
              enterFrom={slide.enterFrom}
              enterTo={slide.enterTo}
              leave="transform transition ease-in-out duration-300"
              leaveFrom={slide.leaveFrom}
              leaveTo={slide.leaveTo}
            >
              <DialogPanel
                className={cn(
                  'flex h-full w-full flex-col',
                  'bg-white dark:bg-secondary-800',
                  'shadow-xl',
                  sizeStyles[position][size]
                )}
              >
                {/* Header */}
                {(title || showCloseButton) && (
                  <div className="flex items-start justify-between border-b border-secondary-200 dark:border-secondary-700 px-6 py-4">
                    <div className="flex-1">
                      {title && (
                        <DialogTitle
                          as="h3"
                          className="text-lg font-semibold text-secondary-900 dark:text-white"
                        >
                          {title}
                        </DialogTitle>
                      )}
                      {description && (
                        <p className="mt-1 text-sm text-secondary-600 dark:text-secondary-400">
                          {description}
                        </p>
                      )}
                    </div>
                    {showCloseButton && (
                      <button
                        type="button"
                        className="ml-4 rounded-lg p-1 text-secondary-400 hover:bg-secondary-100 dark:hover:bg-secondary-700 hover:text-secondary-500 dark:hover:text-secondary-300 transition-colors"
                        onClick={onClose}
                      >
                        <X className="h-5 w-5" aria-label="Close drawer" />
                      </button>
                    )}
                  </div>
                )}

                {/* Content */}
                <div className="flex-1 overflow-y-auto px-6 py-4">
                  {children}
                </div>
              </DialogPanel>
            </TransitionChild>
          </div>
        </div>
      </Dialog>
    </Transition>
  )
}
