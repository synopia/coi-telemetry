

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


export const MachineObservedStates = ['none', 'broken', 'paused', 'notEnoughWorkers', 'notEnoughPower', 'notEnoughComputing', 'notEnoughInput', 'invalidPlacement', 'outputFull', 'noRecipes', 'working'] as const
export type MachineObservedState = typeof MachineObservedStates[number]

export const VehicleObservedStates = ['none', 'broke', 'idle', 'movingEmpty', 'movingLoaded', 'loading', 'unloading', "waiting", "working","stuck", "noFuel"] as const
export type VehicleObservedState = typeof VehicleObservedStates[number]

export type UptimePercent<T extends string> ={
  [K in T]: number
}

export type MachineSummaryRow={
  machineId: string
  recipeId?: string
  observedTicks:number

  uptimePercent: UptimePercent<MachineObservedState>
  uptimeTicks: UptimePercent<MachineObservedState>

  inputs: ProductFlowSummary[]
  outputs: ProductFlowSummary[]

  inputBuffers: ProductBufferSummary[]
  outputBuffers: ProductBufferSummary[]

  primaryBlocker: MachineObservedState
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

  uptimePercent: UptimePercent<VehicleObservedState>
  uptimeTicks: UptimePercent<VehicleObservedState>

  emptyTravelDistance: number
  loadedTravelDistance: number

  deliveriesCompleted: number
  fuelConsumed: number

  delivered: ProductFlowSummary[]
  produced: ProductFlowSummary[]
  consumed: ProductFlowSummary[]

  primaryBlocker: VehicleObservedState
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