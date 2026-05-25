import { useState, useEffect } from 'react'
import {
  Menu,
  Search,
  Bell,
  Moon,
  Sun,
  Monitor,
  User,
  Settings,
  LogOut,
  CreditCard,
  MessageSquare,
  AlertCircle,
  CheckCircle,
} from 'lucide-react'
import { useSidebarStore } from '@/store/sidebarStore'
import { useThemeStore } from '@/store/themeStore'
import { cn } from '@/lib/utils'
import { formatRelativeTime } from '@/lib/utils'
import { Popover, PopoverButton, PopoverPanel } from '@headlessui/react'
import { Link } from 'react-router-dom'
import Badge from '@/components/ui/Badge'
import SearchModal from '@/components/ui/SearchModal'

export default function Header() {
  const { isOpen, toggleSidebar, toggleMobileSidebar } = useSidebarStore()
  const { theme, setTheme } = useThemeStore()
  const [isSearchOpen, setIsSearchOpen] = useState(false)

  // Handle Command+K / Ctrl+K keyboard shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setIsSearchOpen(true)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  const themeOptions = [
    { value: 'light' as const, label: 'Light', icon: Sun },
    { value: 'dark' as const, label: 'Dark', icon: Moon },
    { value: 'system' as const, label: 'System', icon: Monitor },
  ]

  const notifications = [
    {
      id: 1,
      type: 'success',
      title: 'Order completed',
      message: 'Your order #1234 has been completed',
      time: new Date(Date.now() - 1000 * 60 * 5), // 5 minutes ago
      read: false,
    },
    {
      id: 2,
      type: 'info',
      title: 'New message',
      message: 'You have a new message from John Doe',
      time: new Date(Date.now() - 1000 * 60 * 30), // 30 minutes ago
      read: false,
    },
    {
      id: 3,
      type: 'warning',
      title: 'Payment pending',
      message: 'Invoice #5678 payment is pending',
      time: new Date(Date.now() - 1000 * 60 * 60 * 2), // 2 hours ago
      read: true,
    },
    {
      id: 4,
      type: 'success',
      title: 'New sale',
      message: 'Product "Wireless Headphones" sold',
      time: new Date(Date.now() - 1000 * 60 * 60 * 5), // 5 hours ago
      read: true,
    },
  ]

  const unreadCount = notifications.filter((n) => !n.read).length

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'success':
        return CheckCircle
      case 'warning':
        return AlertCircle
      case 'info':
        return MessageSquare
      default:
        return Bell
    }
  }

  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'success':
        return 'text-success-600 dark:text-success-400 bg-success-100 dark:bg-success-900/20'
      case 'warning':
        return 'text-warning-600 dark:text-warning-400 bg-warning-100 dark:bg-warning-900/20'
      case 'info':
        return 'text-primary-600 dark:text-primary-400 bg-primary-100 dark:bg-primary-900/20'
      default:
        return 'text-secondary-600 dark:text-secondary-400 bg-secondary-100 dark:bg-secondary-800'
    }
  }

  return (
    <header
      className={cn(
        'fixed top-0 right-0 z-20 h-16 bg-white dark:bg-secondary-900 border-b border-secondary-200 dark:border-secondary-800 transition-all duration-300',
        isOpen ? 'lg:left-64' : 'lg:left-20',
        'left-0'
      )}
    >
      <div className="h-full px-4 lg:px-6 flex items-center justify-between gap-4">
        {/* Left section */}
        <div className="flex items-center gap-4">
          {/* Mobile menu toggle */}
          <button
            onClick={toggleMobileSidebar}
            aria-label="Open mobile menu"
            className="lg:hidden p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
          >
            <Menu className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
          </button>

          {/* Desktop sidebar toggle */}
          <button
            onClick={toggleSidebar}
            aria-label="Toggle sidebar"
            className="hidden lg:block p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
          >
            <Menu className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
          </button>

          {/* Search */}
          <button
            onClick={() => setIsSearchOpen(true)}
            className="hidden md:flex items-center gap-2 px-4 py-2 bg-secondary-100 dark:bg-secondary-800 rounded-lg min-w-[300px] hover:bg-secondary-200 dark:hover:bg-secondary-700 transition-colors"
          >
            <Search className="w-4 h-4 text-secondary-500 dark:text-secondary-400" />
            <span className="flex-1 text-left text-sm text-secondary-500 dark:text-secondary-400">
              Search...
            </span>
            <kbd className="hidden lg:inline-flex h-6 select-none items-center gap-0.5 rounded border border-secondary-300 dark:border-secondary-600 bg-white dark:bg-secondary-700 px-2 font-mono text-xs font-medium text-secondary-600 dark:text-secondary-400">
              <span className="text-sm">⌘</span>K
            </kbd>
          </button>
        </div>

        {/* Right section */}
        <div className="flex items-center gap-2">
          {/* Search button (mobile) */}
          <button
            onClick={() => setIsSearchOpen(true)}
            aria-label="Search"
            className="md:hidden p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
          >
            <Search className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
          </button>

          {/* Theme switcher */}
          <Popover className="relative">
            <PopoverButton aria-label="Change theme" className="p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors outline-none">
              {theme === 'light' && (
                <Sun className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
              )}
              {theme === 'dark' && (
                <Moon className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
              )}
              {theme === 'system' && (
                <Monitor className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
              )}
            </PopoverButton>

            <PopoverPanel className="absolute right-0 mt-2 w-48 origin-top-right rounded-xl bg-white dark:bg-secondary-800 shadow-lg border border-secondary-200/50 dark:border-secondary-700/50 py-1 z-50">
              {({ close }) => (
                <>
                  {themeOptions.map((option) => (
                    <button
                      key={option.value}
                      onClick={() => {
                        setTheme(option.value)
                        close()
                      }}
                      className={cn(
                        'flex items-center gap-3 w-full px-4 py-2.5 text-sm transition-colors hover:bg-secondary-100 dark:hover:bg-secondary-700',
                        theme === option.value
                          ? 'text-primary-600 dark:text-primary-400 font-medium'
                          : 'text-secondary-700 dark:text-secondary-300'
                      )}
                    >
                      <option.icon className="w-4 h-4" />
                      {option.label}
                    </button>
                  ))}
                </>
              )}
            </PopoverPanel>
          </Popover>

          {/* Notifications */}
          <Popover className="relative">
            <PopoverButton aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`} className="relative p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors outline-none">
              <Bell className="w-5 h-5 text-secondary-700 dark:text-secondary-300" />
              {unreadCount > 0 && (
                <span className="absolute top-1 right-1 flex items-center justify-center w-4 h-4 text-[10px] font-bold text-white bg-danger-500 rounded-full">
                  {unreadCount}
                </span>
              )}
            </PopoverButton>

            <PopoverPanel className="absolute right-0 mt-2 w-80 origin-top-right rounded-xl bg-white dark:bg-secondary-800 shadow-lg border border-secondary-200/50 dark:border-secondary-700/50 overflow-hidden z-50">
              {/* Header */}
              <div className="px-4 py-3 border-b border-secondary-200 dark:border-secondary-700">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">
                    Notifications
                  </h3>
                  {unreadCount > 0 && (
                    <Badge variant="primary" size="sm">
                      {unreadCount} new
                    </Badge>
                  )}
                </div>
              </div>

              {/* Notifications list */}
              <div className="max-h-96 overflow-y-auto">
                {notifications.map((notification) => {
                  const NotificationIcon = getNotificationIcon(notification.type)
                  return (
                    <button
                      key={notification.id}
                      className={cn(
                        'w-full px-4 py-3 text-left transition-colors border-b border-secondary-100 dark:border-secondary-700 last:border-0 hover:bg-secondary-50 dark:hover:bg-secondary-700/50',
                        !notification.read && 'bg-primary-50/50 dark:bg-primary-900/10'
                      )}
                    >
                      <div className="flex gap-3">
                        <div
                          className={cn(
                            'flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center',
                            getNotificationColor(notification.type)
                          )}
                        >
                          <NotificationIcon className="w-5 h-5" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                            {notification.title}
                          </p>
                          <p className="text-xs text-secondary-600 dark:text-secondary-400 mt-0.5 line-clamp-2">
                            {notification.message}
                          </p>
                          <p className="text-xs text-secondary-500 dark:text-secondary-500 mt-1">
                            {formatRelativeTime(notification.time)}
                          </p>
                        </div>
                        {!notification.read && (
                          <div className="flex-shrink-0">
                            <span className="w-2 h-2 bg-primary-600 rounded-full block"></span>
                          </div>
                        )}
                      </div>
                    </button>
                  )
                })}
              </div>

              {/* Footer */}
              <div className="px-4 py-3 border-t border-secondary-200 dark:border-secondary-700">
                <button className="text-sm text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 font-medium transition-colors">
                  View all notifications
                </button>
              </div>
            </PopoverPanel>
          </Popover>

          {/* User Menu */}
          <Popover className="relative">
            <PopoverButton className="flex items-center gap-2 p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors outline-none">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold text-sm">
                AS
              </div>
              <div className="hidden md:block text-left">
                <p className="text-sm font-medium text-secondary-900 dark:text-white">
                  Admin User
                </p>
                <p className="text-xs text-secondary-500 dark:text-secondary-400">
                  admin@example.com
                </p>
              </div>
            </PopoverButton>

            <PopoverPanel className="absolute right-0 mt-2 w-56 origin-top-right rounded-xl bg-white dark:bg-secondary-800 shadow-lg border border-secondary-200/50 dark:border-secondary-700/50 py-1 z-50">
              {({ close }) => (
                <>
                  {/* User info */}
                  <div className="px-4 py-3 border-b border-secondary-200 dark:border-secondary-700">
                    <p className="text-sm font-medium text-secondary-900 dark:text-white">
                      Admin User
                    </p>
                    <p className="text-xs text-secondary-600 dark:text-secondary-400 mt-0.5">
                      admin@example.com
                    </p>
                  </div>

                  {/* Menu items */}
                  <div className="py-1">
                    <Link
                      to="/profile"
                      onClick={() => close()}
                      className="flex items-center gap-3 px-4 py-2.5 text-sm transition-colors hover:bg-secondary-100 dark:hover:bg-secondary-700 text-secondary-700 dark:text-secondary-300"
                    >
                      <User className="w-4 h-4" />
                      Your Profile
                    </Link>

                    <Link
                      to="/settings"
                      onClick={() => close()}
                      className="flex items-center gap-3 px-4 py-2.5 text-sm transition-colors hover:bg-secondary-100 dark:hover:bg-secondary-700 text-secondary-700 dark:text-secondary-300"
                    >
                      <Settings className="w-4 h-4" />
                      Settings
                    </Link>

                    <button
                      onClick={() => close()}
                      className="flex items-center gap-3 w-full px-4 py-2.5 text-sm transition-colors hover:bg-secondary-100 dark:hover:bg-secondary-700 text-secondary-700 dark:text-secondary-300"
                    >
                      <CreditCard className="w-4 h-4" />
                      Billing
                    </button>
                  </div>

                  {/* Logout */}
                  <div className="py-1 border-t border-secondary-200 dark:border-secondary-700">
                    <button
                      onClick={() => close()}
                      className="flex items-center gap-3 w-full px-4 py-2.5 text-sm transition-colors hover:bg-danger-50 dark:hover:bg-danger-900/20 text-danger-600 dark:text-danger-400"
                    >
                      <LogOut className="w-4 h-4" />
                      Sign out
                    </button>
                  </div>
                </>
              )}
            </PopoverPanel>
          </Popover>
        </div>
      </div>

      {/* Search Modal */}
      <SearchModal isOpen={isSearchOpen} onClose={() => setIsSearchOpen(false)} />
    </header>
  )
}
