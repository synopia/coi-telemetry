
export type MetaInfo = {
  id: string
  type?: string
  name?: string
}

export type LiveSummary = {
  metadata: MetaInfo[]
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
  dependencyGraph: ProductDependencyGraph
  impactSimulation: ProductDependencyImpactSimulation
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

export const VehicleBlockerKinds = [
  'none',
  'unknown',
  'noJob',
  'goalUnreachable',
  'notEnoughMaintenance',
  'notEnoughWorkers',
  'needsFuel',
  'refuelRequestFailed',
  'refuelUnreachable',
  'notEnoughComputing',
  'pathFinding',
  'waitingForRoadExit',
  'stuck',
  'strugglingToNavigate',
  'cannotDeliverCargo',
  'waitingForUnload',
  'waitingForPickup',
  'noHarvestTarget',
  'noTruckAvailable',
  'waitingForTruck',
] as const

export type VehicleBlockerKind = typeof VehicleBlockerKinds[number]
export type PressureSummary = {
  maintenance?: number
  power?: number
  computing?: number
  workers?: number
}
export type MachineSummaryRow = {
  machineId: string
  recipeId?: string
  observedTicks: number

  uptimePercent: UptimePercent<ObservedState>
  uptimeTicks: UptimePercent<ObservedState>

  electricity: number
  pressure: PressureSummary

  inputs: ProductFlowSummary[]
  outputs: ProductFlowSummary[]

  inputBuffers: ProductBufferSummary[]
  outputBuffers: ProductBufferSummary[]
  potentialScenarios: MachinePotentialScenario[]

  primaryBlocker: ObservedState
}

export type MachinePotentialScenario = {
  scenarioId: string
  label: string
  factor: number
  inputs: ProductFlowSummary[]
  outputs: ProductFlowSummary[]
}

export type ProductDependencyGraph = {
  products: ProductDependencyProductNode[]
  machines: ProductDependencyMachineNode[]
  edges: ProductDependencyEdge[]
  opportunities: ProductDependencyOpportunity[]
}

export type ProductDependencyImpactSimulation = {
  machines: ImpactMachineSimulationRow[]
  products: ImpactProductSimulationRow[]
  constraints: ImpactConstraintRow[]
}

export type ImpactMachineSimulationRow = {
  machineId: string
  recipeId?: string
  realizedHeadroomFactor: number
  currentInputPerMinute: number
  potentialInputPerMinute: number
  simulatedInputPerMinute: number
  currentOutputPerMinute: number
  potentialOutputPerMinute: number
  simulatedOutputPerMinute: number
  limitingProducts: string[]
  primaryBlocker: ObservedState
}

export type ImpactProductSimulationRow = {
  productId: string
  currentNetPerMinute: number
  baselineSurplusPerMinute: number
  additionalProducedPerMinute: number
  additionalConsumedPerMinute: number
  simulatedNetPerMinute: number
  residualSurplusPerMinute: number
  isLimiting: boolean
}

export type ImpactConstraintRow = {
  productId: string
  baselineSurplusPerMinute: number
  additionalSupplyPerMinute: number
  requestedAdditionalDemandPerMinute: number
  feasibleAdditionalDemandPerMinute: number
  satisfactionPercent: number
}

export type ProductDependencyProductNode = {
  productId: string
  producedPerMinute: number
  consumedPerMinute: number
  netPerMinute: number
  stored: number
  capacity: number
  fillPercent: number
  currentLocalProducedPerMinute: number
  potentialLocalProducedPerMinute: number
  localProductionHeadroomPerMinute: number
  currentDownstreamDemandPerMinute: number
  potentialDownstreamDemandPerMinute: number
  downstreamDemandHeadroomPerMinute: number
}

export type ProductDependencyMachineNode = {
  machineId: string
  recipeId?: string
  currentInputPerMinute: number
  potentialInputPerMinute: number
  inputHeadroomPerMinute: number
  currentOutputPerMinute: number
  potentialOutputPerMinute: number
  outputHeadroomPerMinute: number
  currentUtilizationFactor: number
  primaryBlocker: ObservedState
}

export type ProductDependencyEdge = {
  sourceNodeId: string
  targetNodeId: string
  productId: string
  currentPerMinute: number
  potentialPerMinute: number
  headroomPerMinute: number
}

export type ProductDependencyOpportunity = {
  productId: string
  currentLocalProducedPerMinute: number
  potentialLocalProducedPerMinute: number
  localProductionHeadroomPerMinute: number
  currentDownstreamDemandPerMinute: number
  potentialDownstreamDemandPerMinute: number
  downstreamDemandHeadroomPerMinute: number
  netHeadroomPerMinute: number
  producerMachineIds: string[]
  consumerMachineIds: string[]
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
  blockerPercent: UptimePercent<VehicleBlockerKind>
  blockerTicks: UptimePercent<VehicleBlockerKind>

  electricity: number
  pressure: PressureSummary

  emptyTravelDistance: number
  loadedTravelDistance: number

  deliveriesCompleted: number

  delivered: ProductFlowSummary[]
  produced: ProductFlowSummary[]
  consumed: ProductFlowSummary[]

  jobs: Record<string, number>
  currentJob?: string
  currentJobInfo?: string
  currentGoal?: string
  pathFindingState: string
  drivingState: string

  primaryDetailedBlocker: VehicleBlockerKind
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
  consumedPerMinute: number
  netPerMinute: number
  estimatedMinutesUntilEmpty?: number
  estimatedMinutesUntilFull?: number

}
