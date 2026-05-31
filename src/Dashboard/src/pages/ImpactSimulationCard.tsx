import Card, { CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { LiveSummary } from '@/api/types.ts'
import { MetaInfos } from '@/api/names.ts'


const formatPercent = (value: number) => `${(value * 100).toFixed(0)}%`
const formatRate = (value: number) => `${value.toFixed(2)}/min`

export const ImpactSimulationCard = ({ summary }: { summary: LiveSummary }) => {
  const simulation = summary.window10m.impactSimulation
  const topMachines = simulation.machines
    .filter((machine) => machine.simulatedOutputPerMinute > machine.currentOutputPerMinute + 0.0001)
    .slice(0, 6)
  const topConstraints = simulation.constraints
    .filter((constraint) => constraint.requestedAdditionalDemandPerMinute > 0.0001)
    .slice(0, 6)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Impact Simulator</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="mb-6">
          <h3 className="text-sm font-semibold text-secondary-700 dark:text-secondary-300 mb-3">
            Machines That Really Expand
          </h3>
          {topMachines.length === 0 ? (
            <p className="text-secondary-600 dark:text-secondary-400">
              No machine headroom was feasible in the current 10 minute window.
            </p>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b border-secondary-200 dark:border-secondary-700">
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Machine
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Blocker
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Realized Headroom
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Extra Output
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Limited By
                  </th>
                </tr>
              </thead>
              <tbody>
                {topMachines.map((machine) => (
                  <tr
                    key={machine.machineId}
                    className="border-b border-secondary-100 dark:border-secondary-800 hover:bg-secondary-50 dark:hover:bg-secondary-800/50 transition-colors"
                  >
                    <td className="py-3 px-4">
                      {MetaInfos.getCombinedName(machine.machineId, machine.recipeId)}
                    </td>
                    <td className="py-3 px-4">{machine.primaryBlocker}</td>
                    <td className="py-3 px-4">
                      {formatPercent(machine.realizedHeadroomFactor)}
                    </td>
                    <td className="py-3 px-4">
                      {formatRate(machine.simulatedOutputPerMinute - machine.currentOutputPerMinute)}
                    </td>
                    <td className="py-3 px-4">
                      {machine.limitingProducts.length > 0
                        ? machine.limitingProducts
                            .slice(0, 2)
                            .map((productId) => MetaInfos.getProduct(productId)?.name)
                            .join(', ')
                        : 'Local only'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div>
          <h3 className="text-sm font-semibold text-secondary-700 dark:text-secondary-300 mb-3">
            Hard Limiting Products
          </h3>
          {topConstraints.length === 0 ? (
            <p className="text-secondary-600 dark:text-secondary-400">
              The solver did not find a contested product in this window.
            </p>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b border-secondary-200 dark:border-secondary-700">
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Product
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Requested
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Feasible
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Extra Supply
                  </th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                    Satisfaction
                  </th>
                </tr>
              </thead>
              <tbody>
                {topConstraints.map((constraint) => (
                  <tr
                    key={constraint.productId}
                    className="border-b border-secondary-100 dark:border-secondary-800 hover:bg-secondary-50 dark:hover:bg-secondary-800/50 transition-colors"
                  >
                    <td className="py-3 px-4">
                      {MetaInfos.getName( constraint.productId)}
                    </td>
                    <td className="py-3 px-4">
                      {formatRate(constraint.requestedAdditionalDemandPerMinute)}
                    </td>
                    <td className="py-3 px-4">
                      {formatRate(constraint.feasibleAdditionalDemandPerMinute)}
                    </td>
                    <td className="py-3 px-4">
                      {formatRate(constraint.additionalSupplyPerMinute)}
                    </td>
                    <td className="py-3 px-4">
                      {formatPercent(constraint.satisfactionPercent)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
