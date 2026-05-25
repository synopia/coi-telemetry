import { useState, type ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface Tab {
  id: string
  label: string
  icon?: ReactNode
  content: ReactNode
  disabled?: boolean
}

export interface TabsProps {
  tabs: Tab[]
  defaultTab?: string
  onChange?: (tabId: string) => void
  orientation?: 'horizontal' | 'vertical'
  variant?: 'line' | 'pills' | 'enclosed'
  className?: string
}

export default function Tabs({
  tabs,
  defaultTab,
  onChange,
  orientation = 'horizontal',
  variant = 'line',
  className,
}: TabsProps) {
  const [activeTab, setActiveTab] = useState(defaultTab || tabs[0]?.id)

  const handleTabChange = (tabId: string) => {
    setActiveTab(tabId)
    onChange?.(tabId)
  }

  const variantStyles = {
    line: {
      container: orientation === 'horizontal' ? 'border-b border-secondary-200 dark:border-secondary-700' : 'border-r border-secondary-200 dark:border-secondary-700',
      list: orientation === 'horizontal' ? 'flex space-x-6' : 'flex flex-col space-y-2',
      button: cn(
        'px-1 py-2 text-sm font-medium transition-colors',
        orientation === 'horizontal' ? 'border-b-2 -mb-px' : 'border-r-2 -mr-px',
        'border-transparent',
        'hover:text-secondary-900 dark:hover:text-white',
        'hover:border-secondary-300 dark:hover:border-secondary-600'
      ),
      active: 'text-primary-600 dark:text-primary-400 border-primary-600 dark:border-primary-400',
      inactive: 'text-secondary-600 dark:text-secondary-400',
    },
    pills: {
      container: 'bg-secondary-100 dark:bg-secondary-800 rounded-lg p-1',
      list: orientation === 'horizontal' ? 'flex space-x-1' : 'flex flex-col space-y-1',
      button: 'px-4 py-2 text-sm font-medium rounded-md transition-all',
      active: 'bg-white dark:bg-secondary-700 text-primary-600 dark:text-primary-400 shadow-sm',
      inactive: 'text-secondary-600 dark:text-secondary-400 hover:text-secondary-900 dark:hover:text-white',
    },
    enclosed: {
      container: 'border-b border-secondary-200 dark:border-secondary-700',
      list: orientation === 'horizontal' ? 'flex -mb-px space-x-2' : 'flex flex-col space-y-2',
      button: cn(
        'px-4 py-2 text-sm font-medium rounded-t-lg border transition-all',
        orientation === 'vertical' && 'rounded-l-lg rounded-t-none'
      ),
      active: 'bg-white dark:bg-secondary-800 text-primary-600 dark:text-primary-400 border-secondary-200 dark:border-secondary-700 border-b-white dark:border-b-secondary-800',
      inactive: 'bg-transparent text-secondary-600 dark:text-secondary-400 border-transparent hover:text-secondary-900 dark:hover:text-white hover:border-secondary-300 dark:hover:border-secondary-600',
    },
  }

  const styles = variantStyles[variant]

  return (
    <div className={cn('w-full', className)}>
      <div
        className={cn(
          orientation === 'vertical' ? 'flex gap-6' : 'block'
        )}
      >
        {/* Tab List */}
        <div className={styles.container}>
          <nav className={styles.list} role="tablist">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={activeTab === tab.id}
                aria-controls={`tabpanel-${tab.id}`}
                disabled={tab.disabled}
                onClick={() => handleTabChange(tab.id)}
                className={cn(
                  styles.button,
                  activeTab === tab.id ? styles.active : styles.inactive,
                  tab.disabled && 'cursor-not-allowed opacity-50'
                )}
              >
                <span className="flex items-center gap-2">
                  {tab.icon}
                  {tab.label}
                </span>
              </button>
            ))}
          </nav>
        </div>

        {/* Tab Content */}
        <div
          className={cn(
            'flex-1',
            orientation === 'horizontal' ? 'mt-4' : 'mt-0'
          )}
        >
          {tabs.map((tab) => (
            <div
              key={tab.id}
              id={`tabpanel-${tab.id}`}
              role="tabpanel"
              aria-labelledby={`tab-${tab.id}`}
              hidden={activeTab !== tab.id}
              className={cn(activeTab === tab.id ? 'block' : 'hidden')}
            >
              {tab.content}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
