import React, { forwardRef, type HTMLAttributes } from 'react'
import { User } from 'lucide-react'
import { cn } from '@/lib/utils'

export type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl'
export type AvatarStatus = 'online' | 'offline' | 'away' | 'busy'

const sizeStyles = {
  xs: 'h-6 w-6 text-xs',
  sm: 'h-8 w-8 text-sm',
  md: 'h-10 w-10 text-base',
  lg: 'h-12 w-12 text-lg',
  xl: 'h-16 w-16 text-xl',
  '2xl': 'h-24 w-24 text-2xl',
}

const statusStyles = {
  online: 'bg-success-500 ring-white dark:ring-secondary-800',
  offline: 'bg-secondary-400 ring-white dark:ring-secondary-800',
  away: 'bg-warning-500 ring-white dark:ring-secondary-800',
  busy: 'bg-danger-500 ring-white dark:ring-secondary-800',
}

const statusSizes = {
  xs: 'h-1.5 w-1.5 ring-1',
  sm: 'h-2 w-2 ring-1',
  md: 'h-2.5 w-2.5 ring-2',
  lg: 'h-3 w-3 ring-2',
  xl: 'h-4 w-4 ring-2',
  '2xl': 'h-5 w-5 ring-2',
}

export interface AvatarProps extends Omit<HTMLAttributes<HTMLDivElement>, 'children'> {
  src?: string
  alt?: string
  size?: AvatarSize
  status?: AvatarStatus
  fallback?: string
  rounded?: boolean
}

const Avatar = forwardRef<HTMLDivElement, AvatarProps>(
  (
    {
      src,
      alt = 'Avatar',
      size = 'md',
      status,
      fallback,
      rounded = true,
      className,
      ...props
    },
    ref
  ) => {
    const getInitials = (name?: string) => {
      if (!name) return ''
      const parts = name.split(' ')
      if (parts.length >= 2) {
        return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
      }
      return name.slice(0, 2).toUpperCase()
    }

    const initials = getInitials(fallback || alt)

    return (
      <div
        ref={ref}
        className={cn('relative inline-flex', className)}
        {...props}
      >
        <div
          className={cn(
            'inline-flex items-center justify-center overflow-hidden',
            'bg-secondary-200 dark:bg-secondary-700',
            'text-secondary-600 dark:text-secondary-300',
            'font-semibold',
            sizeStyles[size],
            rounded ? 'rounded-full' : 'rounded-lg'
          )}
        >
          {src ? (
            <img
              src={src}
              alt={alt}
              className="h-full w-full object-cover"
              onError={(e) => {
                // Hide image on error and show fallback
                e.currentTarget.style.display = 'none'
              }}
            />
          ) : initials ? (
            <span>{initials}</span>
          ) : (
            <User className="h-1/2 w-1/2" aria-hidden="true" />
          )}
        </div>
        {status && (
          <span
            className={cn(
              'absolute bottom-0 right-0 rounded-full',
              statusStyles[status],
              statusSizes[size]
            )}
            aria-label={`Status: ${status}`}
          />
        )}
      </div>
    )
  }
)

Avatar.displayName = 'Avatar'

// Avatar Group component for displaying multiple avatars
export interface AvatarGroupProps extends HTMLAttributes<HTMLDivElement> {
  max?: number
  size?: AvatarSize
  children: React.ReactElement<AvatarProps>[]
}

export function AvatarGroup({
  max = 5,
  size = 'md',
  children,
  className,
  ...props
}: AvatarGroupProps) {
  const avatars = Array.isArray(children) ? children : [children]
  const displayAvatars = avatars.slice(0, max)
  const remainingCount = avatars.length - max

  return (
    <div
      className={cn('flex items-center -space-x-2', className)}
      {...props}
    >
      {displayAvatars.map((avatar, index) => (
        <div
          key={index}
          className="ring-2 ring-white dark:ring-secondary-800 rounded-full"
        >
          {React.cloneElement(avatar, { size })}
        </div>
      ))}
      {remainingCount > 0 && (
        <div
          className={cn(
            'inline-flex items-center justify-center',
            'bg-secondary-300 dark:bg-secondary-600',
            'text-secondary-700 dark:text-secondary-200',
            'font-semibold rounded-full',
            'ring-2 ring-white dark:ring-secondary-800',
            sizeStyles[size]
          )}
        >
          <span className="text-xs">+{remainingCount}</span>
        </div>
      )}
    </div>
  )
}

export default Avatar
