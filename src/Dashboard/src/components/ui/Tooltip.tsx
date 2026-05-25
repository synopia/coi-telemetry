import { type ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface TooltipProps {
  children: ReactNode
  content: string | ReactNode
  position?: 'top' | 'bottom' | 'left' | 'right'
  delay?: number
  className?: string
}

export default function Tooltip({
  children,
  content,
  position = 'top',
  delay = 0,
  className,
}: TooltipProps) {
  const positionStyles = {
    top: 'bottom-full left-1/2 -translate-x-1/2 -translate-y-2 mb-1',
    bottom: 'top-full left-1/2 -translate-x-1/2 translate-y-2 mt-1',
    left: 'right-full top-1/2 -translate-y-1/2 -translate-x-2 mr-1',
    right: 'left-full top-1/2 -translate-y-1/2 translate-x-2 ml-1',
  }

  const arrowStyles = {
    top: 'top-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-b-transparent',
    bottom: 'bottom-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-t-transparent',
    left: 'left-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-r-transparent',
    right: 'right-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-l-transparent',
  }

  return (
    <div className="group/tooltip relative inline-flex">
      {children}
      <div
        role="tooltip"
        className={cn(
          'pointer-events-none absolute z-50 whitespace-nowrap rounded-lg',
          'bg-secondary-900 dark:bg-secondary-700 px-3 py-2 text-sm text-white',
          'opacity-0 transition-opacity duration-200',
          'group-hover/tooltip:opacity-100',
          positionStyles[position],
          className
        )}
        style={{ transitionDelay: `${delay}ms` }}
      >
        {content}
        {/* Arrow */}
        <div
          className={cn(
            'absolute h-0 w-0 border-4',
            'border-secondary-900 dark:border-secondary-700',
            arrowStyles[position]
          )}
        />
      </div>
    </div>
  )
}
