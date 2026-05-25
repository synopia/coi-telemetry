

  import EChartsReact from 'echarts-for-react'
  import { LiveSummary } from '@/api/types.ts'

  type BottleneckRow = {
    id: string
    label: string
    kind: 'machine' | 'vehicle'

    workingPercent?: number
    waitingPercent?: number
    notEnoughWorkersPercent?: number
    notEnoughPowerPercent?: number
    notEnoughComputingPercent?: number
    notEnoughMaintenancePercent?: number
    notEnoughInputPercent?: number
    outputFullPercent?: number

    primaryBlocker?: string
  }

  const columns = [
    'Run',
    'Wait',
    'Workers',
    'Power',
    'Compute',
    'Maint',
    'Input',
    'Output',
  ] as const

  function value(row: BottleneckRow, column: (typeof columns)[number]) {
    switch (column) {
      case "Run":
        return row.workingPercent ?? 0
      case "Input":
        return row.notEnoughInputPercent ?? 0
      case "Output":
        return row.outputFullPercent ?? 0
      case "Workers":
        return row.notEnoughWorkersPercent ?? 0
      case "Compute":
        return row.notEnoughComputingPercent ?? 0
      case "Power":
        return row.notEnoughPowerPercent ?? 0
      case "Maint":
        return row.notEnoughMaintenancePercent ?? 0
      case "Wait":
        return row.waitingPercent ?? 0
    }
  }

function bottleneckScore(row: BottleneckRow) {
  return(
    (row.notEnoughInputPercent??0) +
    (row.outputFullPercent??0)+
      (row.notEnoughWorkersPercent??0)*1.2 +
      (row.notEnoughPowerPercent??0)*1.5 +
      (row.notEnoughComputingPercent??0)*1.1 +
      (row.notEnoughMaintenancePercent??0)*1.2 +
      (row.waitingPercent??0)*0.8

  )
}

export function toBottleneckRows(summary:LiveSummary, window:"10s"|"1m"|"5m"|"10m"){
  const w = summary[`window${window}`]
  const machineRows: BottleneckRow[] = w.machines.map((m) => ({
    id: m.machineId,
    label: m.machineId,
    kind: 'machine',
    workingPercent: m.uptimePercent.working,
    notEnoughInputPercent: m.uptimePercent.notEnoughInput ,
    outputFullPercent: m.uptimePercent.outputFull ,
    notEnoughWorkersPercent: m.uptimePercent.notEnoughWorkers ,
    notEnoughPowerPercent: m.uptimePercent.notEnoughPower ,
    notEnoughComputingPercent: m.uptimePercent.notEnoughComputing ,
    notEnoughMaintenancePercent: m.uptimePercent.notEnoughMaintenance ,
    waitingPercent: m.uptimePercent.waiting ,

    primaryBlocker: m.primaryBlocker,
  }))
  const vehicleRows: BottleneckRow[] = w.vehicles.map((v) => ({
    id: v.vehicleId,
    label: v.vehicleId,
    kind: 'vehicle',
    workingPercent: v.uptimePercent.working ,
    outputFullPercent: v.uptimePercent.outputFull ,
    notEnoughWorkersPercent: v.uptimePercent.notEnoughWorkers ,
    notEnoughPowerPercent: v.uptimePercent.notEnoughPower ,
    notEnoughComputingPercent: v.uptimePercent.notEnoughComputing ,
    notEnoughMaintenancePercent: v.uptimePercent.notEnoughMaintenance ,
    notEnoughInputPercent: v.uptimePercent.notEnoughInput ,
    waitingPercent: v.uptimePercent.waiting ,

    primaryBlocker: v.primaryBlocker,
  }))

  return [...machineRows, ...vehicleRows]
}
export function BottleneckHeatmapChart({rows, maxRows=20}:{rows: BottleneckRow[], maxRows?:number}) {
  const visibleRows = [...rows]
    .sort((a, b) => bottleneckScore(b) - bottleneckScore(a))
    .slice(0, maxRows)
    .reverse()

  const yLabels = visibleRows.map(row => row.label)
  const data = visibleRows.flatMap((row,y)=>
    columns.map((column,x)=>({value:[x,y,100*value(row,column)], row})))
  const option = {
    animation: false,
    tooltip: {
      formatter: function (params: any) {
        const column = columns[params.value[0]]
        const row = params.data.row as BottleneckRow
        const percent = params.value[2] as number

        return [
          `<b>${row.label}</b>`,
          `Type: ${row.kind}`,
          `Metric: ${column}`,
          `Value: ${percent.toFixed(1)}%`,
          row.primaryBlocker ? `Blocker: ${row.primaryBlocker}` : "",
        ].filter(Boolean).join("<br>")
      }
    },
    grid:{
      left:180,
      right:24,
      top:32,
      bottom:48,
    },
    xAxis: {
      type: 'category',
      data: columns,
      splitArea: { show: true },
    },
    yAxis: {
      type: 'category',
      data: yLabels,
      splitArea: { show: true },
      axisLabel: {
        width: 160,
        overflow: 'truncate',
      }
    },
    visualMap: {
      min:0,
      max:100,
      dimension: 2,
      calculable: true,
      orient: 'horizontal',
      left: 'center',
      bottom: 0,
      // inRange: {
      //   color: ['#313695', '#4575b4', '#74add1', '#abd9e9', '#e0f3f8', '#ffffbf', '#fee090', '#fdae61', '#f46d43', '#d73027', '#a50026']
      // }
    },
    series: [
      {
        name: 'Bottleneck %',
        type: 'heatmap',
        animation: false,
        data,
        encode:{
          x:0,y:1, value:2
        },
        label: {
          show: true,
          formatter: (params: any) => {
            const percent = params.value[2] as number
            return percent>=10 ? `${percent.toFixed(0)}` : ``
          }
        },
        emphasis: {
          itemStyle: {
            shadowBlur: 8,
            shadowColor: 'rgba(0, 0, 0, 0.35)'
          }
        }
      }
    ]
  }
  return <EChartsReact option={option} style={{height:480}}/>
}