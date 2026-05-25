import { TrendingUp, TrendingDown } from 'lucide-react'
import Card from './Card'
import Badge from './Badge'
import { cn } from '@/lib/utils'
import type { StatCardProps } from '@/types'

export default function StatCard({
  title,
  value,
  change,
  icon: Icon,
}: StatCardProps) {
  const isPositive = change >= 0
  const TrendIcon = isPositive ? TrendingUp : TrendingDown

  return (
    <Card className="relative overflow-hidden">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            {title}
          </p>
          <p className="text-3xl font-bold text-secondary-900 dark:text-white mb-2">
            {value}
          </p>
          <div className="flex items-center gap-2">
            <Badge
              variant={isPositive ? 'success' : 'danger'}
              size="sm"
              className="flex items-center gap-1"
            >
              <TrendIcon className="w-3 h-3" />
              <span>{Math.abs(change)}%</span>
            </Badge>
            <span className="text-xs text-secondary-500 dark:text-secondary-400">
              vs last month
            </span>
          </div>
        </div>
        <div
          className={cn(
            'p-3 rounded-xl',
            isPositive
              ? 'bg-success-100 dark:bg-success-900/20'
              : 'bg-danger-100 dark:bg-danger-900/20'
          )}
        >
          <Icon
            className={cn(
              'w-6 h-6',
              isPositive
                ? 'text-success-600 dark:text-success-400'
                : 'text-danger-600 dark:text-danger-400'
            )}
          />
        </div>
      </div>
    </Card>
  )
}
