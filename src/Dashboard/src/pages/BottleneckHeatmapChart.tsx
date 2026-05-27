

  import EChartsReact from 'echarts-for-react'
  import { LiveSummary } from '@/api/types.ts'

  type BottleneckRow = {
    id: string
    label: string
    kind: 'machine' | 'vehicle'

    workingPercent: number
    idlePercent: number
    notEnoughInputPercent: number
    outputFullPercent: number

    maintenancePressure: number | null
    powerPressure: number | null
    computingPressure: number | null
    workersPressure: number | null

    notEnoughWorkersStopPercent: number
    notEnoughPowerStopPercent: number
    notEnoughComputingStopPercent: number
    notEnoughMaintenanceStopPercent: number
  }

  type HeatmapColumn = {
    key: string
    label: string
    colorValue: (row: BottleneckRow)=>number|null
    stopValue?: (row: BottleneckRow)=>number
    tooltip?: (row: BottleneckRow)=>string
  }
  const columns : HeatmapColumn[] = [
    {
      key:"working",
      label:"Working",
      colorValue:(r)=>r.workingPercent
    },
    {
      key:"idle",
      label:"Idle",
      colorValue:(r)=>r.idlePercent
    },
    {
      key:"noInput",
      label:"No Input",
      colorValue:(r)=>r.notEnoughInputPercent
    },
    {
      key:"outputFull",
      label:"Output Full",
      colorValue:(r)=>r.outputFullPercent
    },
    {
      key:"maintenance",
      label:"Maint",
      colorValue:(r)=>r.maintenancePressure,
      stopValue:(r)=>r.notEnoughMaintenanceStopPercent,
    },
    {
      key:"workers",
      label:"Workers",
      colorValue:(r)=>r.workersPressure,
      stopValue:(r)=>r.notEnoughWorkersStopPercent,
    },
    {
      key:"power",
      label:"Power",
      colorValue:(r)=>r.powerPressure,
      stopValue:(r)=>r.notEnoughPowerStopPercent,
    },
    {
      key:"computing",
      label:"Computing",
      colorValue:(r)=>r.computingPressure,
      stopValue:(r)=>r.notEnoughComputingStopPercent,
    },
  ] as const

function bottleneckScore(row: BottleneckRow) {
  const softPressure =
    (row.maintenancePressure??0)*0.3+
    (row.workersPressure??0)*0.3+
    (row.powerPressure??0)*0.3+
    (row.computingPressure??0)*0.3
  const hardStops =
    (row.notEnoughWorkersStopPercent)*2.9+
    (row.notEnoughPowerStopPercent)*2.5+
    (row.notEnoughComputingStopPercent)*2+
    (row.notEnoughMaintenanceStopPercent)*2
  const productionProblems=
    row.notEnoughInputPercent+
      row.outputFullPercent+
      row.idlePercent*0.2
  return hardStops+softPressure+productionProblems

}
const pressureFromAvailableRatio = (ratio: number|null|undefined) => {
  if( ratio==null||!Number.isFinite(ratio) ) return null
  return Math.max(0, Math.min(100, (1-ratio)*100))
}
const percentFromRatio = (ratio: number|null|undefined) => {
  if( ratio==null||!Number.isFinite(ratio) ) return 0
  return Math.max(0, Math.min(100, ratio*100))
}
export function toBottleneckRows(summary:LiveSummary, window:"10s"|"1m"|"5m"|"10m"){
  const w = summary[`window${window}`]
  const machineRows: BottleneckRow[] = (w.machines).map((m) => ({
    id: m.machineId,
    label: m.machineId,
    kind: 'machine',

    workingPercent: percentFromRatio(m.uptimePercent.working),
    idlePercent: percentFromRatio(m.uptimePercent.idle),
    notEnoughInputPercent: percentFromRatio(m.uptimePercent.notEnoughInput),
    outputFullPercent: percentFromRatio(m.uptimePercent.outputFull),

    maintenancePressure:pressureFromAvailableRatio(m.maintenance),
    powerPressure: pressureFromAvailableRatio(m.power),
    computingPressure:pressureFromAvailableRatio(m.computing),
    workersPressure:pressureFromAvailableRatio(m.workers),

    notEnoughWorkersStopPercent:percentFromRatio(m.uptimePercent.notEnoughWorkers),
    notEnoughPowerStopPercent: percentFromRatio(m.uptimePercent.notEnoughPower),
    notEnoughComputingStopPercent: percentFromRatio(m.uptimePercent.notEnoughComputing),
    notEnoughMaintenanceStopPercent: percentFromRatio(m.uptimePercent.notEnoughMaintenance),

  }))
  const vehicleRows: BottleneckRow[] = w.vehicles.map((v) => ({
    id: v.vehicleId,
    label: v.vehicleId,
    kind: 'vehicle',
    workingPercent: percentFromRatio(v.uptimePercent.working),
    idlePercent: percentFromRatio(v.uptimePercent.idle),
    notEnoughInputPercent: percentFromRatio(v.uptimePercent.notEnoughInput),
    outputFullPercent: percentFromRatio(v.uptimePercent.outputFull),

    maintenancePressure:pressureFromAvailableRatio( v.maintenance),
    powerPressure: pressureFromAvailableRatio(v.power),
    computingPressure:pressureFromAvailableRatio( v.computing),
    workersPressure: pressureFromAvailableRatio(v.workers),

    notEnoughWorkersStopPercent: percentFromRatio(v.uptimePercent.notEnoughWorkers),
    notEnoughPowerStopPercent: percentFromRatio(v.uptimePercent.notEnoughPower),
    notEnoughComputingStopPercent: percentFromRatio(v.uptimePercent.notEnoughComputing),
    notEnoughMaintenanceStopPercent: percentFromRatio(v.uptimePercent.notEnoughMaintenance),
  }))

  return [...machineRows, ...vehicleRows]
}
export function BottleneckHeatmapChart({rows, maxRows=20}:{rows: BottleneckRow[], maxRows?:number}) {
  const visibleRows = [...rows]
    .sort((a, b) => bottleneckScore(b) - bottleneckScore(a))
    .slice(0, maxRows)
    .reverse()
  const heatmapData = visibleRows.flatMap((row,y)=>columns
    .map((column,x)=>{
      const raw = column.colorValue(row);
      const value = raw==null ? 0 : raw;
      return{value:[x,y,value], row, column, isResourceColumn:!!column.stopValue, stopPercent: column.stopValue?.(row)??0}
    }))
  const stopMarkerData = visibleRows.flatMap((row,y)=>columns.flatMap((column,x)=>{
    const stopPercent = column.stopValue?.(row)??0
    if(stopPercent<=0) return []
    return [{value:[x,y,stopPercent], row, column}]
  }))

  const option = {
    animation: false,
    tooltip: {
      formatter: function (params: any) {
        const row = params.data.row as BottleneckRow
        const column = params.data.column as HeatmapColumn
        const pressureOrState = params.data.value[2] as number
        const stopPercent = params.data.stopPercent ?? params.data.value[2] ?? 0

        if (params.seriesType === 'scatter') {
          return [
            `<b>${row.label}</b>`,
            `Metric: ${column.label}`,
            `Stopped: ${stopPercent.toFixed(1)}%`,
          ].join('<br>')
        }
        if (column.stopValue) {
          return [
            `<b>${row.label}</b>`,
            `Resource: ${column.label}`,
            `Pressure: ${pressureOrState.toFixed(1)}%`,
            `Stopped: ${(column.stopValue(row) ?? 0).toFixed(1)}%`,
          ].join('<br>')
        }
        return [
          `<b>${row.label}</b>`,
          `Metric: ${column.label}`,
          `Value: ${pressureOrState.toFixed(1)}%`,
        ].join('<br>')
      },
    },
    grid: {
      left: 180,
      right: 24,
      top: 32,
      bottom: 64,
    },
    xAxis: {
      type: 'category',
      data: columns.map((c) => c.label),
      splitArea: { show: true },
    },
    yAxis: {
      type: 'category',
      data: visibleRows.map((row) => row.label),
      splitArea: { show: true },
      axisLabel: {
        width: 160,
        overflow: 'truncate',
      },
    },
    visualMap: {
      min: 0,
      max: 100,
      dimension: 2,
      calculable: true,
      orient: 'horizontal',
      left: 'center',
      bottom: 0,
      // inRange: {
      //   color: ['#f3f4f6', '#fde68a', '#fb923c', '#dc2626'],
      // },
      inRange: {
        color: ['#313695', '#4575b4', '#74add1', '#abd9e9', '#e0f3f8', '#ffffbf', '#fee090', '#fdae61', '#f46d43', '#d73027', '#a50026']
      }
    },
    series: [
      {
        name: 'Pressure / State %',
        type: 'heatmap',
        animation: false,
        data: heatmapData,
        encode: {
          x: 0,
          y: 1,
          value: 2,
        },
        label: {
          show: true,
          formatter: (params: any) => {
            const v = params.value[2] as number
            const stopPercent = params.data.stopPercent as number|undefined
            if(stopPercent!==undefined && stopPercent>=1){
              return `!${stopPercent.toFixed(0)}`
            }
            return v>=10 ? `${v.toFixed(0)}%` : ''
          },
        },
      },
      {
        name: "Hard Stop",
        type: 'scatter',
        data: stopMarkerData,
        animation: false,
        symbol: "diamond",
        symbolSize: (value:number[])=>{
          const stopPercent = value[2] ??0
          return Math.max(8, Math.min(22, 8+stopPercent*0.25))
        },
        itemStyle: {
          color: '#111827',
          borderColor: '#ffffff',
          borderWidth: 1,
        },
        label:{
          show:true,
          position: "inside",
          color: "#ffffff",
          fontSize:10,
          formatter: (params: any) => {
            const stopPercent = params.value[2] as number
            return stopPercent>=5 ? "!" : ""
          }
        },
        tooltip:{
          show: true,
        },
        z:10
      }
    ],
  }
  return <EChartsReact option={option} style={{height:480}}/>
}