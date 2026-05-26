using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;

namespace CoiTelemetry.RealMod.Mapping;


public sealed class EntityTracker
{
    private readonly ExportIdMapper _ids;
    private readonly Dictionary<string, EntityInfo> _entities = new();
    public IEnumerable<EntityInfo> Entities => _entities.Values;

    public EntityTracker(ExportIdMapper ids)
    {
        _ids = ids;
    }

    public EntityId? Entity(IEntity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        if (entity is FuelStation fuelStation)
        {
            return FuelStation(fuelStation);
        }
        if (entity is MineTower mineTower)
        {
            return MineTower(mineTower);
        }
        if (entity is Machine machine)
        {
            return Machine(machine);
        }

        if (entity is Vehicle vehicle)
        {
            return Vehicle(vehicle);
        }
        return null;
    }

    public EntityId MineTower(MineTower mineTower)
    {
        var id = _ids.MineTower(mineTower.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Type:mineTower.DefaultTitle.Value,
                Name:mineTower.CustomTitle.ValueOrNull);
        }
        return id;
    }
    public EntityId FuelStation(FuelStation fuelStation)
    {
        var id = _ids.FuelStation(fuelStation.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Type:fuelStation.DefaultTitle.Value,
                Name:fuelStation.CustomTitle.ValueOrNull);
        }
        return id;
    }
    public EntityId Machine(Machine machine)
    {
        var id = _ids.Machine(machine.Id, machine.Prototype.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Type:machine.DefaultTitle.Value,
                Name:machine.CustomTitle.ValueOrNull);
        }
        return id;
    }

    public EntityId Vehicle(Vehicle vehicle)
    {
        var id = _ids.Vehicle(vehicle.Id, vehicle.Prototype.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Type:vehicle.DefaultTitle.Value,
                Name:vehicle.CustomTitle.Value);
        }
        return id;
    }

    public ProductId Product(ProductProto product)
    {
        var id=_ids.Product(product.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Name:product.Strings.Name.TranslatedString,
                Type:null
                );
        }
        return id;
    }

    public RecipeId? Recipe(RecipeProto? recipe)
    {
        if (recipe is null)
        {
            return null;
        }
        var id = _ids.Recipe(recipe.Id);
        if (!_entities.ContainsKey(id.Value))
        {
            _entities[id.Value] = new EntityInfo(
                Id:id.Value,
                Name:recipe.Strings.Name.TranslatedString,
                Type:null
                );
        }
        return id;
    }
}