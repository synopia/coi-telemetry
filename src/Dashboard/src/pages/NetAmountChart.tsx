import { LiveSummary } from '@/api/types.ts'
import EChartsReact from 'echarts-for-react'


export const NetAmountChart = ({ summary }: { summary: LiveSummary }) => {
  const data = summary.window1m.productFlow.map((product) => ({
    value: [product.netPerMinute,product.productId],
  }))
  .sort((a, b) => b.value[0] - a.value[0])
  const yLabels = data.map((row) => row.value[1])

  const option = {
    xAxis: {
      type: 'value',
    },
    yAxis: {
      type: 'category',
      data: yLabels,
    },
    series: [
      {
        name: 'Net Amount',
        type: 'bar',
        data,
        encode: {
          x: 0,
          y: 1,
        },
      },
    ],
  }
  return <EChartsReact option={option} style={{height:480}} />
}