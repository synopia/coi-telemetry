import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import Header from './Header'
import { useSidebarStore } from '@/store/sidebarStore'
import { cn } from '@/lib/utils'

export default function Layout() {
  const { isOpen } = useSidebarStore()

  return (
    <div className="min-h-screen bg-secondary-100 dark:bg-secondary-950">
      <Sidebar />
      <Header />

      <main
        className={cn(
          'pt-16 transition-all duration-300',
          isOpen ? 'lg:ml-64' : 'lg:ml-20'
        )}
      >
        <div className="p-4 lg:p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
