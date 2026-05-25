import { type ReactNode } from 'react'
import { ToastContainer } from './Toast'

// Re-export useToast hook for convenience
export { useToast } from '@/hooks/useToast'

export function ToastProvider({ children }: { children: ReactNode }) {
  return (
    <>
      {children}
      <ToastContainer />
    </>
  )
}
