using System;
using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;

namespace CoiTelemetry.RealMod.Aggregation;

public static class ProductImpactSimulator
{
    private const string FullLocalCapacityScenarioId = "full-local-capacity";
    private const double Epsilon = 0.0001;
    private const int MaxIterations = 64;

    public static ProductDependencyImpactSimulation Build(
        IReadOnlyList<MachineSummaryRow> machines,
        IReadOnlyList<ProductFlowSummaryRow> productFlow)
    {
        var productIds = new HashSet<string>(productFlow.Select(product => product.ProductId));
        var baselineSurplus = productFlow.ToDictionary(
            product => product.ProductId,
            product => Math.Max(0, product.NetPerMinute));

        var machineStates = machines
            .Select(CreateMachineState)
            .Where(state => state is not null)
            .Cast<MachineState>()
            .ToArray();

        foreach (var machine in machineStates)
        {
            foreach (var productId in machine.InputHeadroom.Keys)
            {
                productIds.Add(productId);
            }

            foreach (var productId in machine.OutputHeadroom.Keys)
            {
                productIds.Add(productId);
            }
        }

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var productFactors = BuildProductFactors(machineStates, baselineSurplus, productIds);
            if (productFactors.Count == 0)
            {
                break;
            }

            var changed = false;
            foreach (var machine in machineStates)
            {
                var factor = 1.0;
                foreach (var productId in machine.InputHeadroom.Keys)
                {
                    if (productFactors.TryGetValue(productId, out var productFactor))
                    {
                        factor = Math.Min(factor, productFactor);
                    }
                }

                var nextAlpha = machine.Alpha * factor;
                if (nextAlpha + Epsilon < machine.Alpha)
                {
                    machine.Alpha = nextAlpha;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        var constraints = BuildConstraintRows(machineStates, baselineSurplus, productIds);
        var limitingProductIds = new HashSet<string>(constraints
            .Where(constraint => constraint.SatisfactionPercent < 0.9999)
            .Select(constraint => constraint.ProductId));

        var machineRows = machineStates
            .Select(machine => BuildMachineRow(machine, limitingProductIds))
            .OrderByDescending(machine => machine.SimulatedOutputPerMinute - machine.CurrentOutputPerMinute)
            .ThenBy(machine => machine.MachineId)
            .ToArray();

        var productFlowIndex = productFlow.ToDictionary(product => product.ProductId);
        var productRows = productIds
            .Select(productId => BuildProductRow(
                productId,
                productFlowIndex.TryGetValue(productId, out var currentProduct) ? currentProduct : null,
                baselineSurplus,
                machineStates,
                limitingProductIds.Contains(productId)))
            .OrderByDescending(product => Math.Abs(product.AdditionalProducedPerMinute - product.AdditionalConsumedPerMinute))
            .ThenBy(product => product.ProductId)
            .ToArray();

        return new ProductDependencyImpactSimulation(
            Machines: machineRows,
            Products: productRows,
            Constraints: constraints);
    }

    private static MachineState? CreateMachineState(MachineSummaryRow machine)
    {
        if (machine.RecipeId is null)
        {
            return null;
        }

        var fullScenario = machine.PotentialScenarios
            .FirstOrDefault(s => string.Equals(s.ScenarioId, FullLocalCapacityScenarioId, StringComparison.Ordinal));

        if (fullScenario is null)
        {
            return null;
        }

        var currentInputs = machine.Inputs.ToDictionary(flow => flow.ProductId, flow => flow.PerMinute);
        var currentOutputs = machine.Outputs.ToDictionary(flow => flow.ProductId, flow => flow.PerMinute);
        var potentialInputs = fullScenario.Inputs.ToDictionary(flow => flow.ProductId, flow => flow.PerMinute);
        var potentialOutputs = fullScenario.Outputs.ToDictionary(flow => flow.ProductId, flow => flow.PerMinute);

        var inputHeadroom = BuildHeadroom(currentInputs, potentialInputs);
        var outputHeadroom = BuildHeadroom(currentOutputs, potentialOutputs);

        if (inputHeadroom.Count == 0 && outputHeadroom.Count == 0)
        {
            return null;
        }

        return new MachineState(
            machine.MachineId,
            machine.RecipeId,
            machine.PrimaryBlocker,
            currentInputs,
            potentialInputs,
            currentOutputs,
            potentialOutputs,
            inputHeadroom,
            outputHeadroom);
    }

    private static Dictionary<string, double> BuildHeadroom(
        IReadOnlyDictionary<string, double> current,
        IReadOnlyDictionary<string, double> potential)
    {
        return current.Keys
            .Union(potential.Keys)
            .Select(productId =>
            {
                var currentValue = current.TryGetValue(productId, out var currentPerMinute) ? currentPerMinute : 0;
                var potentialValue = potential.TryGetValue(productId, out var potentialPerMinute) ? potentialPerMinute : currentValue;
                return new KeyValuePair<string, double>(productId, Math.Max(0, potentialValue - currentValue));
            })
            .Where(entry => entry.Value > Epsilon)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private static Dictionary<string, double> BuildProductFactors(
        IReadOnlyList<MachineState> machines,
        IReadOnlyDictionary<string, double> baselineSurplus,
        IReadOnlyCollection<string> productIds)
    {
        var factors = new Dictionary<string, double>();

        foreach (var productId in productIds)
        {
            var additionalSupply = machines.Sum(machine => machine.GetAdditionalOutput(productId));
            var requestedDemand = machines.Sum(machine => machine.GetAdditionalInput(productId));
            var available = (baselineSurplus.TryGetValue(productId, out var surplus) ? surplus : 0) + additionalSupply;

            if (requestedDemand > available + Epsilon)
            {
                factors[productId] = available <= Epsilon
                    ? 0
                    : Math.Max(0, Math.Min(1, available / requestedDemand));
            }
        }

        return factors;
    }

    private static ImpactConstraintRow[] BuildConstraintRows(
        IReadOnlyList<MachineState> machines,
        IReadOnlyDictionary<string, double> baselineSurplus,
        IReadOnlyCollection<string> productIds)
    {
        return productIds
            .Select(productId =>
            {
                var additionalSupply = machines.Sum(machine => machine.GetAdditionalOutput(productId));
                var requestedDemand = machines.Sum(machine => machine.GetRequestedInput(productId));
                var feasibleDemand = machines.Sum(machine => machine.GetAdditionalInput(productId));
                var surplus = baselineSurplus.TryGetValue(productId, out var baseline) ? baseline : 0;
                var satisfactionPercent = requestedDemand <= Epsilon
                    ? 1
                    : Math.Max(0, Math.Min(1, feasibleDemand / requestedDemand));

                return new ImpactConstraintRow(
                    ProductId: productId,
                    BaselineSurplusPerMinute: surplus,
                    AdditionalSupplyPerMinute: additionalSupply,
                    RequestedAdditionalDemandPerMinute: requestedDemand,
                    FeasibleAdditionalDemandPerMinute: feasibleDemand,
                    SatisfactionPercent: satisfactionPercent);
            })
            .Where(constraint =>
                constraint.AdditionalSupplyPerMinute > Epsilon ||
                constraint.RequestedAdditionalDemandPerMinute > Epsilon ||
                constraint.BaselineSurplusPerMinute > Epsilon)
            .OrderBy(constraint => constraint.SatisfactionPercent)
            .ThenByDescending(constraint => constraint.RequestedAdditionalDemandPerMinute)
            .ThenBy(constraint => constraint.ProductId)
            .ToArray();
    }

    private static ImpactMachineSimulationRow BuildMachineRow(
        MachineState machine,
        HashSet<string> limitingProductIds)
    {
        var limitingProducts = machine.InputHeadroom.Keys
            .Where(limitingProductIds.Contains)
            .OrderBy(productId => productId)
            .ToArray();

        return new ImpactMachineSimulationRow(
            MachineId: machine.MachineId,
            RecipeId: machine.RecipeId,
            RealizedHeadroomFactor: machine.Alpha,
            CurrentInputPerMinute: machine.CurrentInputs.Values.Sum(),
            PotentialInputPerMinute: machine.PotentialInputs.Values.Sum(),
            SimulatedInputPerMinute: machine.CurrentInputs.Values.Sum() + machine.InputHeadroom.Values.Sum(value => value * machine.Alpha),
            CurrentOutputPerMinute: machine.CurrentOutputs.Values.Sum(),
            PotentialOutputPerMinute: machine.PotentialOutputs.Values.Sum(),
            SimulatedOutputPerMinute: machine.CurrentOutputs.Values.Sum() + machine.OutputHeadroom.Values.Sum(value => value * machine.Alpha),
            LimitingProducts: limitingProducts,
            PrimaryBlocker: machine.PrimaryBlocker);
    }

    private static ImpactProductSimulationRow BuildProductRow(
        string productId,
        ProductFlowSummaryRow? productFlow,
        IReadOnlyDictionary<string, double> baselineSurplus,
        IReadOnlyList<MachineState> machines,
        bool isLimiting)
    {
        var additionalProduced = machines.Sum(machine => machine.GetAdditionalOutput(productId));
        var additionalConsumed = machines.Sum(machine => machine.GetAdditionalInput(productId));
        var currentNet = productFlow?.NetPerMinute ?? 0;
        var baseline = baselineSurplus.TryGetValue(productId, out var surplus) ? surplus : 0;

        return new ImpactProductSimulationRow(
            ProductId: productId,
            CurrentNetPerMinute: currentNet,
            BaselineSurplusPerMinute: baseline,
            AdditionalProducedPerMinute: additionalProduced,
            AdditionalConsumedPerMinute: additionalConsumed,
            SimulatedNetPerMinute: currentNet + additionalProduced - additionalConsumed,
            ResidualSurplusPerMinute: baseline + additionalProduced - additionalConsumed,
            IsLimiting: isLimiting);
    }

    private sealed class MachineState(
        string machineId,
        string recipeId,
        Contracts.Enums.ObservedState primaryBlocker,
        IReadOnlyDictionary<string, double> currentInputs,
        IReadOnlyDictionary<string, double> potentialInputs,
        IReadOnlyDictionary<string, double> currentOutputs,
        IReadOnlyDictionary<string, double> potentialOutputs,
        IReadOnlyDictionary<string, double> inputHeadroom,
        IReadOnlyDictionary<string, double> outputHeadroom)
    {
        public string MachineId { get; } = machineId;
        public string RecipeId { get; } = recipeId;
        public Contracts.Enums.ObservedState PrimaryBlocker { get; } = primaryBlocker;
        public IReadOnlyDictionary<string, double> CurrentInputs { get; } = currentInputs;
        public IReadOnlyDictionary<string, double> PotentialInputs { get; } = potentialInputs;
        public IReadOnlyDictionary<string, double> CurrentOutputs { get; } = currentOutputs;
        public IReadOnlyDictionary<string, double> PotentialOutputs { get; } = potentialOutputs;
        public IReadOnlyDictionary<string, double> InputHeadroom { get; } = inputHeadroom;
        public IReadOnlyDictionary<string, double> OutputHeadroom { get; } = outputHeadroom;
        public double Alpha { get; set; } = 1;

        public double GetRequestedInput(string productId)
        {
            return InputHeadroom.TryGetValue(productId, out var inputPerMinute) ? inputPerMinute : 0;
        }

        public double GetAdditionalInput(string productId)
        {
            return GetRequestedInput(productId) * Alpha;
        }

        public double GetAdditionalOutput(string productId)
        {
            return (OutputHeadroom.TryGetValue(productId, out var outputPerMinute) ? outputPerMinute : 0) * Alpha;
        }
    }
}
