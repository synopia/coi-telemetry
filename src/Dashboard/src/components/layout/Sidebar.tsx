import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ChevronDown, LayoutDashboard, X } from 'lucide-react'
import { useSidebarStore } from '@/store/sidebarStore'
import { cn } from '@/lib/utils'
import type { NavItem } from '@/types'

const navigation: NavItem[] = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
]

export default function Sidebar() {
  const location = useLocation()
  const { isOpen, isMobileOpen, closeMobileSidebar } = useSidebarStore()
  const [expandedItems, setExpandedItems] = useState<string[]>([])

  const toggleExpanded = (itemName: string) => {
    setExpandedItems((prev) =>
      prev.includes(itemName)
        ? prev.filter((name) => name !== itemName)
        : [...prev, itemName]
    )
  }

  const isItemActive = (item: NavItem): boolean => {
    if (item.children) {
      return item.children.some((child) => location.pathname === child.href)
    }
    return location.pathname === item.href
  }

  // Auto-expand parent menus when a child item is active
  useEffect(() => {
    const activeParents: string[] = []

    navigation.forEach((item) => {
      if (item.children) {
        const hasActiveChild = item.children.some(
          (child) => location.pathname === child.href
        )
        if (hasActiveChild) {
          activeParents.push(item.name)
        }
      }
    })

    if (activeParents.length > 0) {
      setExpandedItems((prev) => {
        // Merge with existing expanded items to preserve manually opened menus
        const newExpanded = [...new Set([...prev, ...activeParents])]
        return newExpanded
      })
    }
  }, [location.pathname])

  return (
    <>
      {/* Mobile overlay */}
      {isMobileOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 lg:hidden"
          onClick={closeMobileSidebar}
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed top-0 left-0 z-50 h-screen transition-all duration-300 ease-in-out',
          'bg-white dark:bg-secondary-900 border-r border-secondary-200 dark:border-secondary-800',
          'flex flex-col',
          'lg:z-30',
          isOpen ? 'lg:w-64' : 'lg:w-20',
          isMobileOpen
            ? 'translate-x-0 w-64'
            : '-translate-x-full lg:translate-x-0'
        )}
      >
        {/* Logo */}
        <div
          className={cn(
            'flex items-center h-16 px-4 border-b border-secondary-200 dark:border-secondary-800',
            !isOpen ? 'lg:justify-center' : 'justify-between'
          )}
        >
          <Link to="/" className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary-500 to-primary-700 flex items-center justify-center text-white font-bold text-lg flex-shrink-0">
              T
            </div>
            <span
              className={cn(
                'text-xl font-bold text-secondary-900 dark:text-white transition-opacity',
                !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
              )}
            >
              CoI Telemetry
            </span>
          </Link>

          {/* Mobile close button */}
          <button
            onClick={closeMobileSidebar}
            aria-label="Close sidebar"
            className={cn(
              'lg:hidden p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800',
              !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
            )}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto py-4 px-3">
          <ul className="space-y-1">
            {navigation.map((item) => {
              const isActive = isItemActive(item)
              const isExpanded = expandedItems.includes(item.name)
              const hasChildren = item.children && item.children.length > 0

              return (
                <li key={item.name}>
                  {/* Parent Item */}
                  {hasChildren ? (
                    <>
                      <button
                        onClick={() => toggleExpanded(item.name)}
                        className={cn(
                          'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200',
                          'text-secondary-700 dark:text-secondary-300',
                          'hover:bg-secondary-100 dark:hover:bg-secondary-800',
                          isActive &&
                            'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 font-medium',
                          !isOpen && 'lg:justify-center lg:px-2'
                        )}
                      >
                        <item.icon className="w-5 h-5 flex-shrink-0" />
                        <span
                          className={cn(
                            'flex-1 text-left transition-opacity',
                            !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
                          )}
                        >
                          {item.name}
                        </span>
                        <ChevronDown
                          className={cn(
                            'w-4 h-4 transition-transform',
                            isExpanded && 'rotate-180',
                            !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
                          )}
                        />
                      </button>

                      {/* Children Items */}
                      {isExpanded && (
                        <ul
                          className={cn(
                            'mt-1 space-y-1',
                            !isOpen && 'lg:hidden'
                          )}
                        >
                          {item.children?.map((child) => {
                            const isChildActive =
                              location.pathname === child.href
                            return (
                              <li key={child.name}>
                                <Link
                                  to={child.href}
                                  onClick={closeMobileSidebar}
                                  className={cn(
                                    'flex items-center gap-3 px-3 py-2 ml-6 rounded-lg transition-all duration-200',
                                    'text-secondary-600 dark:text-secondary-400 text-sm',
                                    'hover:bg-secondary-100 dark:hover:bg-secondary-800',
                                    isChildActive &&
                                      'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 font-medium'
                                  )}
                                >
                                  <child.icon className="w-4 h-4 flex-shrink-0" />
                                  <span>{child.name}</span>
                                </Link>
                              </li>
                            )
                          })}
                        </ul>
                      )}
                    </>
                  ) : (
                    <Link
                      to={item.href}
                      onClick={closeMobileSidebar}
                      className={cn(
                        'flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200',
                        'text-secondary-700 dark:text-secondary-300',
                        'hover:bg-secondary-100 dark:hover:bg-secondary-800',
                        isActive &&
                          'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 font-medium',
                        !isOpen && 'lg:justify-center lg:px-2'
                      )}
                    >
                      <item.icon className="w-5 h-5 flex-shrink-0" />
                      <span
                        className={cn(
                          'transition-opacity',
                          !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
                        )}
                      >
                        {item.name}
                      </span>
                    </Link>
                  )}
                </li>
              )
            })}
          </ul>
        </nav>
      </aside>
    </>
  )
}
