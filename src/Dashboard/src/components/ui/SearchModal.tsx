import { useState, useEffect, Fragment } from 'react'
import { Dialog, Transition } from '@headlessui/react'
import { Search, FileText, LayoutDashboard, ShoppingCart, Settings, X } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

interface SearchResult {
  id: string
  title: string
  description: string
  icon: typeof LayoutDashboard
  path: string
  category: string
}

interface SearchModalProps {
  isOpen: boolean
  onClose: () => void
}

export default function SearchModal({ isOpen, onClose }: SearchModalProps) {
  const [query, setQuery] = useState('')
  const navigate = useNavigate()

  // Mock search results - in a real app, this would be dynamic
  const allResults: SearchResult[] = [
    {
      id: '1',
      title: 'Dashboard',
      description: 'View analytics and overview',
      icon: LayoutDashboard,
      path: '/',
      category: 'Pages',
    },
    {
      id: '2',
      title: 'E-Commerce',
      description: 'Manage products and orders',
      icon: ShoppingCart,
      path: '/ecommerce',
      category: 'Pages',
    },
    {
      id: '3',
      title: 'Products',
      description: 'Browse all products',
      icon: ShoppingCart,
      path: '/ecommerce/products',
      category: 'E-Commerce',
    },
    {
      id: '4',
      title: 'Invoices',
      description: 'View and manage invoices',
      icon: FileText,
      path: '/ecommerce/invoices',
      category: 'E-Commerce',
    },
    {
      id: '5',
      title: 'Settings',
      description: 'Configure your preferences',
      icon: Settings,
      path: '/settings',
      category: 'Pages',
    },
  ]

  const filteredResults = query
    ? allResults.filter(
        (result) =>
          result.title.toLowerCase().includes(query.toLowerCase()) ||
          result.description.toLowerCase().includes(query.toLowerCase())
      )
    : allResults.slice(0, 5)

  const handleSelect = (path: string) => {
    navigate(path)
    onClose()
    setQuery('')
  }

  useEffect(() => {
    if (!isOpen) {
      setQuery('')
    }
  }, [isOpen])

  return (
    <Transition show={isOpen} as={Fragment}>
      <Dialog onClose={onClose} className="relative z-50">
        {/* Backdrop */}
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-200"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-150"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black/50" aria-hidden="true" />
        </Transition.Child>

        {/* Modal */}
        <div className="fixed inset-0 flex items-start justify-center pt-[20vh]">
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-200"
            enterFrom="opacity-0 scale-95"
            enterTo="opacity-100 scale-100"
            leave="ease-in duration-150"
            leaveFrom="opacity-100 scale-100"
            leaveTo="opacity-0 scale-95"
          >
            <Dialog.Panel className="w-full max-w-2xl mx-4 bg-white dark:bg-secondary-800 rounded-xl shadow-2xl border border-secondary-200 dark:border-secondary-700 overflow-hidden">
              {/* Search Input */}
              <div className="flex items-center gap-3 px-4 py-4 border-b border-secondary-200 dark:border-secondary-700">
                <Search className="w-5 h-5 text-secondary-400 flex-shrink-0" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search pages, products, and more..."
                  className="flex-1 bg-transparent border-none outline-none text-base text-secondary-900 dark:text-white placeholder:text-secondary-500 dark:placeholder:text-secondary-400"
                  autoFocus
                />
                <button
                  onClick={onClose}
                  className="p-1.5 hover:bg-secondary-100 dark:hover:bg-secondary-700 rounded-lg transition-colors"
                >
                  <X className="w-4 h-4 text-secondary-500 dark:text-secondary-400" />
                </button>
              </div>

              {/* Results */}
              <div className="max-h-96 overflow-y-auto">
                {filteredResults.length > 0 ? (
                  <div className="py-2">
                    {filteredResults.map((result) => {
                      const Icon = result.icon
                      return (
                        <button
                          key={result.id}
                          onClick={() => handleSelect(result.path)}
                          className="w-full flex items-center gap-3 px-4 py-3 hover:bg-secondary-50 dark:hover:bg-secondary-700/50 transition-colors text-left"
                        >
                          <div className="w-10 h-10 rounded-lg bg-primary-100 dark:bg-primary-900/20 flex items-center justify-center flex-shrink-0">
                            <Icon className="w-5 h-5 text-primary-600 dark:text-primary-400" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                              {result.title}
                            </p>
                            <p className="text-xs text-secondary-600 dark:text-secondary-400 truncate">
                              {result.description}
                            </p>
                          </div>
                          <span className="text-xs text-secondary-500 dark:text-secondary-500 flex-shrink-0">
                            {result.category}
                          </span>
                        </button>
                      )
                    })}
                  </div>
                ) : (
                  <div className="py-12 text-center">
                    <Search className="w-12 h-12 text-secondary-300 dark:text-secondary-600 mx-auto mb-3" />
                    <p className="text-sm text-secondary-600 dark:text-secondary-400">
                      No results found for "{query}"
                    </p>
                  </div>
                )}
              </div>

              {/* Footer */}
              <div className="px-4 py-3 border-t border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900/50">
                <div className="flex items-center justify-between text-xs text-secondary-600 dark:text-secondary-400">
                  <div className="flex items-center gap-4">
                    <div className="flex items-center gap-1.5">
                      <kbd className="px-2 py-1 bg-white dark:bg-secondary-700 border border-secondary-300 dark:border-secondary-600 rounded text-xs font-medium">
                        ↑↓
                      </kbd>
                      <span>Navigate</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <kbd className="px-2 py-1 bg-white dark:bg-secondary-700 border border-secondary-300 dark:border-secondary-600 rounded text-xs font-medium">
                        ↵
                      </kbd>
                      <span>Select</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <kbd className="px-2 py-1 bg-white dark:bg-secondary-700 border border-secondary-300 dark:border-secondary-600 rounded text-xs font-medium">
                      ESC
                    </kbd>
                    <span>Close</span>
                  </div>
                </div>
              </div>
            </Dialog.Panel>
          </Transition.Child>
        </div>
      </Dialog>
    </Transition>
  )
}
