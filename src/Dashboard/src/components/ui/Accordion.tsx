import { useState, type ReactNode } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface AccordionItem {
  id: string
  title: string | ReactNode
  content: ReactNode
  icon?: ReactNode
  disabled?: boolean
}

export interface AccordionProps {
  items: AccordionItem[]
  type?: 'single' | 'multiple'
  defaultOpen?: string | string[]
  className?: string
  variant?: 'default' | 'bordered' | 'separated'
}

export default function Accordion({
  items,
  type = 'single',
  defaultOpen,
  className,
  variant = 'default',
}: AccordionProps) {
  const [openItems, setOpenItems] = useState<string[]>(() => {
    if (!defaultOpen) return []
    return Array.isArray(defaultOpen) ? defaultOpen : [defaultOpen]
  })

  const toggleItem = (id: string) => {
    setOpenItems((prev) => {
      if (type === 'single') {
        return prev.includes(id) ? [] : [id]
      } else {
        return prev.includes(id)
          ? prev.filter((item) => item !== id)
          : [...prev, id]
      }
    })
  }

  const isOpen = (id: string) => openItems.includes(id)

  const variantStyles = {
    default: {
      container: 'divide-y divide-secondary-200 dark:divide-secondary-700',
      item: '',
      button: 'w-full',
    },
    bordered: {
      container: 'border border-secondary-200 dark:border-secondary-700 rounded-lg divide-y divide-secondary-200 dark:divide-secondary-700',
      item: '',
      button: 'w-full',
    },
    separated: {
      container: 'space-y-2',
      item: 'border border-secondary-200 dark:border-secondary-700 rounded-lg overflow-hidden',
      button: 'w-full',
    },
  }

  const styles = variantStyles[variant]

  return (
    <div className={cn(styles.container, className)}>
      {items.map((item) => (
        <div key={item.id} className={styles.item}>
          <button
            type="button"
            onClick={() => !item.disabled && toggleItem(item.id)}
            disabled={item.disabled}
            className={cn(
              'flex items-center justify-between gap-3 px-4 py-4 text-left',
              'transition-colors duration-150',
              'hover:bg-secondary-50 dark:hover:bg-secondary-800/50',
              'focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-500/20',
              item.disabled && 'cursor-not-allowed opacity-60',
              styles.button
            )}
            aria-expanded={isOpen(item.id)}
            aria-controls={`accordion-content-${item.id}`}
          >
            <div className="flex items-center gap-3 flex-1 min-w-0">
              {item.icon && (
                <span className="flex-shrink-0 text-secondary-600 dark:text-secondary-400">
                  {item.icon}
                </span>
              )}
              <span className="text-sm font-medium text-secondary-900 dark:text-white">
                {item.title}
              </span>
            </div>
            <ChevronDown
              className={cn(
                'h-5 w-5 flex-shrink-0 text-secondary-500 dark:text-secondary-400',
                'transition-transform duration-200',
                isOpen(item.id) && 'rotate-180'
              )}
              aria-hidden="true"
            />
          </button>
          <div
            id={`accordion-content-${item.id}`}
            role="region"
            aria-labelledby={`accordion-button-${item.id}`}
            className={cn(
              'overflow-hidden transition-all duration-200 ease-in-out',
              isOpen(item.id) ? 'max-h-[1000px] opacity-100' : 'max-h-0 opacity-0'
            )}
          >
            <div className="px-4 py-4 text-sm text-secondary-700 dark:text-secondary-300 border-t border-secondary-100 dark:border-secondary-700/50">
              {item.content}
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

// Simple single item accordion for quick use
export interface AccordionItemProps {
  title: string | ReactNode
  children: ReactNode
  defaultOpen?: boolean
  icon?: ReactNode
  className?: string
}

export function AccordionSingle({
  title,
  children,
  defaultOpen = false,
  icon,
  className,
}: AccordionItemProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen)

  return (
    <div className={cn('border border-secondary-200 dark:border-secondary-700 rounded-lg overflow-hidden', className)}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-between gap-3 w-full px-4 py-4 text-left transition-colors duration-150 hover:bg-secondary-50 dark:hover:bg-secondary-800/50"
        aria-expanded={isOpen}
      >
        <div className="flex items-center gap-3 flex-1 min-w-0">
          {icon && (
            <span className="flex-shrink-0 text-secondary-600 dark:text-secondary-400">
              {icon}
            </span>
          )}
          <span className="text-sm font-medium text-secondary-900 dark:text-white">
            {title}
          </span>
        </div>
        <ChevronDown
          className={cn(
            'h-5 w-5 flex-shrink-0 text-secondary-500 dark:text-secondary-400',
            'transition-transform duration-200',
            isOpen && 'rotate-180'
          )}
          aria-hidden="true"
        />
      </button>
      <div
        className={cn(
          'overflow-hidden transition-all duration-200 ease-in-out',
          isOpen ? 'max-h-[1000px] opacity-100' : 'max-h-0 opacity-0'
        )}
      >
        <div className="px-4 py-4 text-sm text-secondary-700 dark:text-secondary-300 border-t border-secondary-100 dark:border-secondary-700/50">
          {children}
        </div>
      </div>
    </div>
  )
}
