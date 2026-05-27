

export type LiveSummary = {
  window10s: ExportSummary,
  window1m: ExportSummary,
  window5m: ExportSummary,
  window10m: ExportSummary,
}
export type ExportSummary = {
  meta: SummaryMeta
  machines: MachineSummaryRow[]
  productFlow: ProductFlowSummaryRow[]
  vehicles: VehicleSummaryRow[]
}

export type SummaryMeta = {
  summaryId: string
  observedTicks: number
  step: number
  createdAtUtc: string
}


export const ObservedStates = [
  'unknown',
  'working',
  'idle',
  'notEnoughInput',
  'outputFull',
  'notEnoughWorkers',
  'notEnoughPower',
  'notEnoughComputing',
  'notEnoughMaintenance', ] as const

export type ObservedState = typeof ObservedStates[number]

export type UptimePercent<T extends string> ={
  [K in T]: number
}

export type MachineSummaryRow = {
  machineId: string
  recipeId?: string
  observedTicks: number

  uptimePercent: UptimePercent<ObservedState>
  uptimeTicks: UptimePercent<ObservedState>

  maintenance: number
  power: number
  computing: number
  workers: number

  inputs: ProductFlowSummary[]
  outputs: ProductFlowSummary[]

  inputBuffers: ProductBufferSummary[]
  outputBuffers: ProductBufferSummary[]

  primaryBlocker: ObservedState
}

export type ProductFlowSummary = {
  productId: string
  amount: number
  perMinute: number
}
export type ProductBufferSummary = {
  productId: string
  stored: number
  capacity: number
  fillPercent: number
}

export type VehicleSummaryRow = {
  vehicleId: string
  observedTicks: number
  assignedTo?: string

  uptimePercent: UptimePercent<ObservedState>
  uptimeTicks: UptimePercent<ObservedState>

  maintenance: number
  power: number
  computing: number
  workers: number

  emptyTravelDistance: number
  loadedTravelDistance: number

  deliveriesCompleted: number

  delivered: ProductFlowSummary[]
  produced: ProductFlowSummary[]
  consumed: ProductFlowSummary[]

  primaryBlocker: ObservedState
}

export type ProductFlowSummaryRow = {
  productId: string
  observedTicks: number
  latestStored: number
  latestCapacity: number
  latestFillPercent: number

  minStored: number
  maxStored: number
  avgStored: number

  producedAmount: number
  consumedAmount: number
  importedAmount: number
  exportedAmount: number
  minedAmount: number
  dumpedAmount: number
  lostAmount: number
  netAmount: number
  producedPerMinute: number
  netPerMinute: number
  estimatedMinutesUntilEmpty?: number
  estimatedMinutesUntilFull?: number

}