

export type LiveSummary = {
  window10s: ExportSummary,
  window1m: ExportSummary,
  window5m: ExportSummary,
  window10m: ExportSummary,
}
export type ExportSummary = {
  meta: SummaryMeta
  machines: MachineSummaryRow[]
}

export type SummaryMeta = {
  summaryId: string
  observedTicks: number
  step: number
  createdAtUtc: string
}


export const MachineObservedStates = ['none', 'broken', 'paused', 'notEnoughWorkers', 'notEnoughPower', 'notEnoughComputing', 'notEnoughInput', 'invalidPlacement', 'outputFull', 'noRecipes', 'working'] as const
export type MachineObservedState = typeof MachineObservedStates[number]

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