import Card, { CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { LiveSummary, MetaInfo } from '@/api/types.ts'

const formatRate = (value: number) => value.toFixed(2)

const buildMetaIndex = (metadata: MetaInfo[]) =>
  new Map(metadata.map((meta) => [meta.id, meta] as const))

const resolveLabel = (metaIndex: Map<string, MetaInfo>, id: string) => {
  const meta = metaIndex.get(id)
  return meta?.name ?? meta?.type ?? id
}

export const DependencyOpportunities = ({ summary }: { summary: LiveSummary }) => {
  const metaIndex = buildMetaIndex(summary.metadata)
  const opportunities = summary.window10m.dependencyGraph.opportunities.slice(0, 8)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Dependency Opportunities</CardTitle>
      </CardHeader>
      <CardContent>
        {opportunities.length === 0 ? (
          <p className="text-secondary-600 dark:text-secondary-400">
            No local graph headroom detected in the current 10 minute window.
          </p>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-secondary-200 dark:border-secondary-700">
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Product
                </th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Extra Local Supply
                </th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Extra Downstream Pull
                </th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Net Headroom
                </th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Producer Fixes
                </th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-secondary-700 dark:text-secondary-300">
                  Consumer Fixes
                </th>
              </tr>
            </thead>
            <tbody>
              {opportunities.map((opportunity) => (
                <tr
                  key={opportunity.productId}
                  className="border-b border-secondary-100 dark:border-secondary-800 hover:bg-secondary-50 dark:hover:bg-secondary-800/50 transition-colors"
                >
                  <td className="py-3 px-4">
                    {resolveLabel(metaIndex, opportunity.productId)}
                  </td>
                  <td className="py-3 px-4">
                    {formatRate(opportunity.localProductionHeadroomPerMinute)}/min
                  </td>
                  <td className="py-3 px-4">
                    {formatRate(opportunity.downstreamDemandHeadroomPerMinute)}/min
                  </td>
                  <td
                    className={
                      'py-3 px-4 ' +
                      (opportunity.netHeadroomPerMinute >= 0
                        ? 'text-emerald-700 dark:text-emerald-300'
                        : 'text-amber-700 dark:text-amber-300')
                    }
                  >
                    {formatRate(opportunity.netHeadroomPerMinute)}/min
                  </td>
                  <td className="py-3 px-4">
                    {opportunity.producerMachineIds.length}
                  </td>
                  <td className="py-3 px-4">
                    {opportunity.consumerMachineIds.length}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  )
}
