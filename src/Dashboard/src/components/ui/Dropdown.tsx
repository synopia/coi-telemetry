import { Fragment, type ReactNode } from 'react'
import { Menu, MenuButton, MenuItem, MenuItems, Transition } from '@headlessui/react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface DropdownItem {
  label: string | ReactNode
  icon?: ReactNode
  onClick?: () => void
  href?: string
  disabled?: boolean
  danger?: boolean
  variant?: 'danger' | 'default'
  divider?: boolean
}

export interface DropdownProps {
  trigger?: ReactNode
  items: DropdownItem[]
  align?: 'left' | 'right'
  className?: string
  buttonClassName?: string
}

export default function Dropdown({
  trigger,
  items,
  align = 'right',
  className,
  buttonClassName,
}: DropdownProps) {
  return (
    <Menu as="div" className={cn('relative inline-block text-left', className)}>
      <MenuButton
        className={cn(
          'inline-flex items-center justify-center gap-2',
          'rounded-lg border border-secondary-300 dark:border-secondary-600',
          'bg-white dark:bg-secondary-800',
          'px-4 py-2 text-sm font-medium',
          'text-secondary-700 dark:text-secondary-200',
          'hover:bg-secondary-50 dark:hover:bg-secondary-700',
          'focus:outline-none focus:ring-2 focus:ring-primary-500/20',
          'transition-colors duration-150',
          buttonClassName
        )}
      >
        {trigger || (
          <>
            Options
            <ChevronDown className="h-4 w-4" aria-hidden="true" />
          </>
        )}
      </MenuButton>

      <Transition
        as={Fragment}
        enter="transition ease-out duration-100"
        enterFrom="transform opacity-0 scale-95"
        enterTo="transform opacity-100 scale-100"
        leave="transition ease-in duration-75"
        leaveFrom="transform opacity-100 scale-100"
        leaveTo="transform opacity-0 scale-95"
      >
        <MenuItems
          className={cn(
            'absolute z-50 mt-2 w-56 origin-top-right',
            'rounded-lg border border-secondary-200 dark:border-secondary-700',
            'bg-white dark:bg-secondary-800 shadow-lg',
            'ring-1 ring-black/5 dark:ring-white/10',
            'focus:outline-none',
            'divide-y divide-secondary-100 dark:divide-secondary-700',
            align === 'left' ? 'left-0' : 'right-0'
          )}
        >
          <div className="py-1">
            {items.map((item, index) => {
              if (item.divider) {
                return (
                  <div
                    key={index}
                    className="my-1 h-px bg-secondary-200 dark:bg-secondary-700"
                  />
                )
              }

              return (
                <MenuItem key={index} disabled={item.disabled}>
                  {({ focus }) => (
                    <button
                      type="button"
                      onClick={item.onClick}
                      disabled={item.disabled}
                      className={cn(
                        'group flex w-full items-center gap-3 px-4 py-2 text-sm',
                        'transition-colors duration-150',
                        focus && !item.disabled
                          ? 'bg-secondary-100 dark:bg-secondary-700'
                          : '',
                        item.disabled
                          ? 'cursor-not-allowed opacity-50'
                          : 'cursor-pointer',
                        item.danger
                          ? 'text-danger-600 dark:text-danger-400'
                          : 'text-secondary-900 dark:text-white'
                      )}
                    >
                      {item.icon && (
                        <span className="flex-shrink-0">{item.icon}</span>
                      )}
                      <span>{item.label}</span>
                    </button>
                  )}
                </MenuItem>
              )
            })}
          </div>
        </MenuItems>
      </Transition>
    </Menu>
  )
}
