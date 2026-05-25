import { Fragment, type ReactNode } from 'react'
import { Popover as HeadlessPopover, PopoverButton, PopoverPanel, Transition } from '@headlessui/react'
import { cn } from '@/lib/utils'

export interface PopoverProps {
  trigger: ReactNode
  children: ReactNode
  position?: 'top' | 'bottom' | 'left' | 'right'
  align?: 'start' | 'center' | 'end'
  className?: string
  panelClassName?: string
}

export default function Popover({
  trigger,
  children,
  position = 'bottom',
  align = 'center',
  className,
  panelClassName,
}: PopoverProps) {
  const positionStyles = {
    top: {
      start: 'bottom-full left-0 mb-2',
      center: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
      end: 'bottom-full right-0 mb-2',
    },
    bottom: {
      start: 'top-full left-0 mt-2',
      center: 'top-full left-1/2 -translate-x-1/2 mt-2',
      end: 'top-full right-0 mt-2',
    },
    left: {
      start: 'right-full top-0 mr-2',
      center: 'right-full top-1/2 -translate-y-1/2 mr-2',
      end: 'right-full bottom-0 mr-2',
    },
    right: {
      start: 'left-full top-0 ml-2',
      center: 'left-full top-1/2 -translate-y-1/2 ml-2',
      end: 'left-full bottom-0 ml-2',
    },
  }

  return (
    <HeadlessPopover className={cn('relative inline-block', className)}>
      <PopoverButton className="focus:outline-none focus:ring-2 focus:ring-primary-500/20 rounded-lg">
        {trigger}
      </PopoverButton>

      <Transition
        as={Fragment}
        enter="transition ease-out duration-200"
        enterFrom="opacity-0 translate-y-1"
        enterTo="opacity-100 translate-y-0"
        leave="transition ease-in duration-150"
        leaveFrom="opacity-100 translate-y-0"
        leaveTo="opacity-0 translate-y-1"
      >
        <PopoverPanel
          className={cn(
            'absolute z-50',
            'rounded-lg border border-secondary-200 dark:border-secondary-700',
            'bg-white dark:bg-secondary-800 shadow-lg',
            'ring-1 ring-black/5 dark:ring-white/10',
            positionStyles[position][align],
            panelClassName
          )}
        >
          {children}
        </PopoverPanel>
      </Transition>
    </HeadlessPopover>
  )
}
