import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface SidebarState {
  isOpen: boolean
  isMobileOpen: boolean
  toggleSidebar: () => void
  toggleMobileSidebar: () => void
  closeMobileSidebar: () => void
}

export const useSidebarStore = create<SidebarState>()(
  persist(
    (set) => ({
      isOpen: true,
      isMobileOpen: false,
      toggleSidebar: () => set((state) => ({ isOpen: !state.isOpen })),
      toggleMobileSidebar: () =>
        set((state) => ({ isMobileOpen: !state.isMobileOpen })),
      closeMobileSidebar: () => set({ isMobileOpen: false }),
    }),
    {
      name: 'sidebar-storage',
      partialize: (state) => ({ isOpen: state.isOpen }),
    }
  )
)
