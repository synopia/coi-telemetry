import { Menu, Monitor, Moon, Sun } from 'lucide-react'
import { useSidebarStore } from '@/store/sidebarStore'
import { useThemeStore } from '@/store/themeStore'
import { cn } from '@/lib/utils'
import { Popover, PopoverButton, PopoverPanel } from '@headlessui/react'

export default function Header() {
  const { isOpen, toggleSidebar, toggleMobileSidebar } = useSidebarStore()
  const { theme, setTheme } = useThemeStore()

  const themeOptions = [
    { value: 'light' as const, label: 'Light', icon: Sun },
    { value: 'dark' as const, label: 'Dark', icon: Moon },
    { value: 'system' as const, label: 'System', icon: Monitor },
  ]

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
        </div>

        {/* Right section */}
        <div className="flex items-center gap-2">
          {/* Theme switcher */}
          <Popover className="relative">
            <PopoverButton
              aria-label="Change theme"
              className="p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors outline-none"
            >
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
        </div>
      </div>
    </header>
  )
}
