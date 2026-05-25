import { useState, useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import {
  LayoutDashboard,
  BarChart3,
  ShoppingCart,
  LineChart,
  Table,
  FileText,
  Calendar,
  User,
  Users,
  Settings,
  Layers,
  X,
  ChevronDown,
  LogIn,
  UserPlus,
  KeyRound,
  Shield,
  Package,
  Plus,
  FileText as Invoice,
  ArrowLeftRight,
  Files,
  FolderOpen,
  DollarSign,
  HelpCircle,
  Key,
  Puzzle,
  FileQuestion,
  AlertTriangle,
  Clock,
  Wrench,
  CheckCircle,
  Sparkles,
  PenSquare,
} from 'lucide-react'
import { useSidebarStore } from '@/store/sidebarStore'
import { cn } from '@/lib/utils'
import type { NavItem } from '@/types'

const navigation: NavItem[] = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Analytics', href: '/analytics', icon: BarChart3 },
  { name: 'User Management', href: '/users', icon: Users },
  {
    name: 'Showcase',
    href: '/showcase',
    icon: Sparkles,
    children: [
      { name: 'UI Components', href: '/ui-showcase', icon: Layers },
      { name: 'Forms', href: '/forms-showcase', icon: PenSquare },
      { name: 'Tables', href: '/tables-showcase', icon: Table },
    ],
  },
  {
    name: 'E-Commerce',
    href: '/ecommerce',
    icon: ShoppingCart,
    children: [
      { name: 'Products', href: '/ecommerce/products', icon: Package },
      { name: 'Add Product', href: '/ecommerce/add-product', icon: Plus },
      { name: 'Invoices', href: '/ecommerce/invoices', icon: Invoice },
      { name: 'Create Invoice', href: '/ecommerce/create-invoice', icon: FileText },
      { name: 'Transactions', href: '/ecommerce/transactions', icon: ArrowLeftRight },
    ],
  },
  { name: 'Charts', href: '/charts', icon: LineChart },
  { name: 'Tables', href: '/tables', icon: Table },
  { name: 'Forms', href: '/forms', icon: FileText },
  { name: 'UI Elements', href: '/ui-elements', icon: Layers },
  { name: 'Components', href: '/components', icon: Layers },
  { name: 'Calendar', href: '/calendar', icon: Calendar },
  {
    name: 'Authentication',
    href: '/auth',
    icon: Shield,
    children: [
      { name: 'Login', href: '/auth/login', icon: LogIn },
      { name: 'Sign Up', href: '/auth/signup', icon: UserPlus },
      { name: 'Reset Password', href: '/auth/reset-password', icon: KeyRound },
    ],
  },
  {
    name: 'Pages',
    href: '/pages',
    icon: Files,
    children: [
      { name: 'File Manager', href: '/pages/file-manager', icon: FolderOpen },
      { name: 'Pricing Tables', href: '/pages/pricing', icon: DollarSign },
      { name: 'FAQ', href: '/pages/faq', icon: HelpCircle },
      { name: 'API Keys', href: '/pages/api-keys', icon: Key },
      { name: 'Integrations', href: '/pages/integrations', icon: Puzzle },
      { name: 'Blank Page', href: '/pages/blank', icon: FileQuestion },
      { name: '404 Error', href: '/404', icon: AlertTriangle },
      { name: '500 Error', href: '/pages/500', icon: AlertTriangle },
      { name: '503 Error', href: '/pages/503', icon: AlertTriangle },
      { name: 'Coming Soon', href: '/pages/coming-soon', icon: Clock },
      { name: 'Maintenance', href: '/pages/maintenance', icon: Wrench },
      { name: 'Success', href: '/pages/success', icon: CheckCircle },
    ],
  },
  { name: 'Profile', href: '/profile', icon: User },
  { name: 'Settings', href: '/settings', icon: Settings },
]

export default function Sidebar() {
  const location = useLocation()
  const { isOpen, isMobileOpen, closeMobileSidebar } = useSidebarStore()
  const [expandedItems, setExpandedItems] = useState<string[]>([])

  const toggleExpanded = (itemName: string) => {
    setExpandedItems((prev) =>
      prev.includes(itemName) ? prev.filter((name) => name !== itemName) : [...prev, itemName]
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
        const hasActiveChild = item.children.some((child) => location.pathname === child.href)
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
          isMobileOpen ? 'translate-x-0 w-64' : '-translate-x-full lg:translate-x-0'
        )}
      >
        {/* Logo */}
        <div className={cn(
          "flex items-center h-16 px-4 border-b border-secondary-200 dark:border-secondary-800",
          !isOpen ? 'lg:justify-center' : 'justify-between'
        )}>
          <Link
            to="/"
            className="flex items-center gap-3"
          >
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary-500 to-primary-700 flex items-center justify-center text-white font-bold text-lg flex-shrink-0">
              T
            </div>
            <span className={cn(
              "text-xl font-bold text-secondary-900 dark:text-white transition-opacity",
              !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
            )}>
              TailPanel
            </span>
          </Link>

          {/* Mobile close button */}
          <button
            onClick={closeMobileSidebar}
            aria-label="Close sidebar"
            className={cn(
              "lg:hidden p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800",
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
                        <ul className={cn('mt-1 space-y-1', !isOpen && 'lg:hidden')}>
                          {item.children?.map((child) => {
                            const isChildActive = location.pathname === child.href
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

        {/* User section - always at bottom */}
        <div className="border-t border-secondary-200 dark:border-secondary-800 p-4 mt-auto">
          <div
            className={cn(
              'flex items-center gap-3',
              !isOpen && 'lg:justify-center'
            )}
          >
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold flex-shrink-0">
              AS
            </div>
            <div
              className={cn(
                'flex-1 transition-opacity',
                !isOpen && 'lg:opacity-0 lg:w-0 lg:overflow-hidden'
              )}
            >
              <p className="text-sm font-medium text-secondary-900 dark:text-white">
                Admin User
              </p>
              <p className="text-xs text-secondary-500 dark:text-secondary-400">
                admin@tailpanel.com
              </p>
            </div>
          </div>
        </div>
      </aside>
    </>
  )
}
