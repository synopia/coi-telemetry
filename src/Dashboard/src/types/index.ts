export interface User {
  id: string
  name: string
  email: string
  avatar?: string
  role: 'admin' | 'user' | 'moderator'
}

export interface StatCardProps {
  title: string
  value: string | number
  change: number
  icon: React.ComponentType<{ className?: string }>
  trend?: 'up' | 'down'
}

export interface ChartDataPoint {
  name: string
  value: number
  [key: string]: string | number
}

export interface TableColumn<T> {
  id: string
  header: string
  accessor: keyof T | ((row: T) => React.ReactNode)
  cell?: (value: unknown) => React.ReactNode
}

export interface NavItem {
  name: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  children?: NavItem[]
}

export interface BreadcrumbItem {
  name: string
  href?: string
}
