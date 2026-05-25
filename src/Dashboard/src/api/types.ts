

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


export const ObservedStates = ['unknown', 'working', 'waiting', 'notEnoughWorkers', 'notEnoughPower', 'notEnoughComputing', 'notEnoughMaintenance', 'notEnoughInput', 'outputFull'] as const
export type ObservedState = typeof ObservedStates[number]

export type UptimePercent<T extends string> ={
  [K in T]: number
}

export type MachineSummaryRow={
  machineId: string
  recipeId?: string
  observedTicks:number

  uptimePercent: UptimePercent<ObservedState>
  uptimeTicks: UptimePercent<ObservedState>

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

  emptyTravelDistance: number
  loadedTravelDistance: number

  deliveriesCompleted: number
  fuelConsumed: number

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