import {
  DollarSign,
  Users,
  ShoppingCart,
  TrendingUp,
  ArrowUpRight,
  ArrowDownRight,
} from 'lucide-react'
import StatCard from '@/components/ui/StatCard'
import Card, { CardHeader, CardTitle, CardContent } from '@/components/ui/Card'
import Badge from '@/components/ui/Badge'
import Breadcrumbs from '@/components/layout/Breadcrumbs'
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts'
import { formatCurrency } from '@/lib/utils'
import { useLiveSummary } from '@/api/useLiveSummary.ts'

const WorstMachines = ()=>{
  const { summary, error } = useLiveSummary()
  const worstMachines = [...summary?.window10m?.machines??[]]
      .sort((a, b) => (b.uptimePercent.notEnoughInput??0)+(b.uptimePercent.outputFull??0)-(a.uptimePercent.notEnoughInput??0)+(a.uptimePercent.outputFull??0))
      .slice(0,10)

  const fmt = (n?: number) => !n ? 0 : (100*n).toFixed(1)
  return (
    <Card>
      <CardHeader>
        <CardTitle>Worst Machines</CardTitle>
      </CardHeader>
      <CardContent>
        <table className="w-full">
          <thead>
            <tr>
              <th>Machine</th>
              <th>Running</th>
              <th>Input Shortage</th>
              <th>Output Full</th>
              <th>Blocker</th>
            </tr>
          </thead>
          <tbody>
            {worstMachines.map((machine, index) => (
              <tr key={index}>
                <td>{machine.machineId}</td>
                <td>{fmt(machine.uptimePercent.working)}%</td>
                <td>{fmt(machine.uptimePercent.notEnoughInput)}%</td>
                <td>{fmt(machine.uptimePercent.outputFull)}%</td>
                <td>{machine.primaryBlocker}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </CardContent>
    </Card>
  )
}

export default function Dashboard() {
  return (
    <div>
      <Breadcrumbs items={[{ name: 'Dashboard' }]} />

      <div className="mb-6">
        <h1 className="text-3xl font-bold text-secondary-900 dark:text-white mb-2">
          Dashboard Overview
        </h1>
        <p className="text-secondary-600 dark:text-secondary-400">
          Welcome back! Here's what's happening with your business today.
        </p>
      </div>

      {/* Stats Grid */}
{/*
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
        <StatCard
          title="Total Revenue"
          value={formatCurrency(45231.89)}
          change={20.1}
          icon={DollarSign}
        />
        <StatCard
          title="Active Users"
          value="2,350"
          change={15.3}
          icon={Users}
        />
        <StatCard
          title="Total Orders"
          value="1,234"
          change={-4.2}
          icon={ShoppingCart}
        />
        <StatCard
          title="Conversion Rate"
          value="3.42%"
          change={8.7}
          icon={TrendingUp}
        />
      </div>
*/}

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-1 gap-6 mb-6">
        <WorstMachines />
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">

        {/* Revenue Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Revenue Overview</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
{/*
              <AreaChart data={revenueData}>
                <defs>
                  <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" className="stroke-secondary-200 dark:stroke-secondary-700" />
                <XAxis dataKey="name" className="text-xs" />
                <YAxis className="text-xs" />
                <Tooltip
                  contentStyle={{
                    backgroundColor: 'rgba(255, 255, 255, 0.95)',
                    border: '1px solid #e2e8f0',
                    borderRadius: '8px',
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="revenue"
                  stroke="#3b82f6"
                  fillOpacity={1}
                  fill="url(#colorRevenue)"
                />
              </AreaChart>
*/}
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Profit vs Expenses */}
        <Card>
          <CardHeader>
            <CardTitle>Profit vs Expenses</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
{/*
              <BarChart data={revenueData}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-secondary-200 dark:stroke-secondary-700" />
                <XAxis dataKey="name" className="text-xs" />
                <YAxis className="text-xs" />
                <Tooltip
                  contentStyle={{
                    backgroundColor: 'rgba(255, 255, 255, 0.95)',
                    border: '1px solid #e2e8f0',
                    borderRadius: '8px',
                  }}
                />
                <Legend />
                <Bar dataKey="profit" fill="#2563eb" radius={[8, 8, 0, 0]} />
                <Bar dataKey="expenses" fill="#64748b" radius={[8, 8, 0, 0]} />
              </BarChart>
*/}
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* Tables */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Orders */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Orders</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {/*{recentOrders.map((order) => (
                <div
                  key={order.id}
                  className="flex items-center justify-between py-3 border-b border-secondary-200 dark:border-secondary-700 last:border-0"
                >
                  <div className="flex-1">
                    <p className="font-medium text-secondary-900 dark:text-white">
                      {order.customer}
                    </p>
                    <p className="text-sm text-secondary-600 dark:text-secondary-400">
                      {order.product}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-secondary-900 dark:text-white">
                      {formatCurrency(order.amount)}
                    </p>
                    <Badge
                      variant={
                        order.status === 'completed'
                          ? 'success'
                          : order.status === 'pending'
                            ? 'warning'
                            : 'primary'
                      }
                      size="sm"
                    >
                      {order.status}
                    </Badge>
                  </div>
                </div>
              ))}*/}
            </div>
          </CardContent>
        </Card>

        {/* Top Products */}
        <Card>
          <CardHeader>
            <CardTitle>Top Products</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
{/*
              {topProducts.map((product) => (
                <div
                  key={product.id}
                  className="flex items-center justify-between py-3 border-b border-secondary-200 dark:border-secondary-700 last:border-0"
                >
                  <div className="flex-1">
                    <p className="font-medium text-secondary-900 dark:text-white">
                      {product.name}
                    </p>
                    <p className="text-sm text-secondary-600 dark:text-secondary-400">
                      {product.sales} sales
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-secondary-900 dark:text-white">
                      {formatCurrency(product.revenue)}
                    </p>
                    <div
                      className={`flex items-center justify-end gap-1 text-sm font-medium ${
                        product.trend >= 0
                          ? 'text-success-700 dark:text-success-400'
                          : 'text-danger-700 dark:text-danger-400'
                      }`}
                    >
                      {product.trend >= 0 ? (
                        <ArrowUpRight className="w-4 h-4" />
                      ) : (
                        <ArrowDownRight className="w-4 h-4" />
                      )}
                      <span>{Math.abs(product.trend)}%</span>
                    </div>
                  </div>
                </div>
              ))}
*/}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
