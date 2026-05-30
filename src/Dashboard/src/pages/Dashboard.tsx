import Card, { CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import Breadcrumbs from '@/components/layout/Breadcrumbs'
import { useLiveSummary } from '@/api/useLiveSummary.ts'
import { BottleneckHeatmapChart, toBottleneckRows } from '@/pages/BottleneckHeatmapChart.tsx'
import { NetAmountChart } from '@/pages/NetAmountChart.tsx'
import { DependencyOpportunities } from '@/pages/DependencyOpportunities.tsx'
import { ImpactSimulationCard } from '@/pages/ImpactSimulationCard.tsx'

const WorstMachines = () => {
  const { summary, error } = useLiveSummary()
  const worstMachines = [...(summary?.window10m?.machines ?? [])]
    .sort(
      (a, b) =>
        (b.uptimePercent.notEnoughInput ?? 0) +
        (b.uptimePercent.outputFull ?? 0) -
        (a.uptimePercent.notEnoughInput ?? 0) +
        (a.uptimePercent.outputFull ?? 0)
    )
    .slice(0, 5)

  const fmt = (n?: number) => (!n ? 0 : (100 * n).toFixed(1))
  return (
    <Card>
      <CardHeader>
        <CardTitle>Worst Machines</CardTitle>
      </CardHeader>
      <CardContent>
        <table className="w-full">
          <thead>
            <tr className="border-b border-secondary-200 dark:border-secondary-700">
              <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                Machine
              </th>
              <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                Running
              </th>
              <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                Input Shortage
              </th>
              <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                Output Full
              </th>
              <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                Blocker
              </th>
            </tr>
          </thead>
          <tbody>
            {worstMachines.map((machine, index) => (
              <tr
                key={index}
                className="border-b border-secondary-100 dark:border-secondary-800 hover:bg-secondary-50 dark:hover:bg-secondary-800/50 transition-colors"
              >
                <td className="py-3 px-4">{machine.machineId}</td>
                <td className="py-3 px-4">
                  {fmt(machine.uptimePercent.working)}%
                </td>
                <td className="py-3 px-4">
                  {fmt(machine.uptimePercent.notEnoughInput)}%
                </td>
                <td className="py-3 px-4">
                  {fmt(machine.uptimePercent.outputFull)}%
                </td>
                <td className="py-3 px-4">{machine.primaryBlocker}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </CardContent>
    </Card>
  )
}

export default function Dashboard() {
  const { summary, error } = useLiveSummary()

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

      <div className="grid grid-cols-1 lg:grid-cols-1 gap-6 mb-6">
        {summary && (
          <BottleneckHeatmapChart rows={toBottleneckRows(summary, '1m')} />
        )}
        {/*<WorstMachines />*/}
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Revenue Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Net amount chart</CardTitle>
          </CardHeader>
          <CardContent>
            {summary&&<NetAmountChart summary={summary}/>}
          </CardContent>
        </Card>

        {/* Profit vs Expenses */}
        {summary && <DependencyOpportunities summary={summary} />}
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-1 gap-6 mb-6">
        {summary && <ImpactSimulationCard summary={summary} />}
      </div>

      {/* Tables */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Orders */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Orders</CardTitle>
          </CardHeader>
          <CardContent>
          </CardContent>
        </Card>

        {/* Top Products */}
        <Card>
          <CardHeader>
            <CardTitle>Top Products</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
