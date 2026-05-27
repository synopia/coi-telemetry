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


public sealed class EntityTracker(IdTracker ids)
{
    private readonly Dictionary<string, MetaInfo> _meta = new();
    public IEnumerable<MetaInfo> Meta => _meta.Values;
    
    public IdTracker Ids => ids;

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
        var id = ids.MineTower(mineTower.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Type:mineTower.DefaultTitle.Value,
                Name:mineTower.CustomTitle.ValueOrNull);
        }
        return id;
    }
    public EntityId FuelStation(FuelStation fuelStation)
    {
        var id = ids.FuelStation(fuelStation.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Type:fuelStation.DefaultTitle.Value,
                Name:fuelStation.CustomTitle.ValueOrNull);
        }
        return id;
    }
    public EntityId Machine(Machine machine)
    {
        var id = ids.Machine(machine.Id, machine.Prototype.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Type:machine.DefaultTitle.Value,
                Name:machine.CustomTitle.ValueOrNull);
        }
        return id;
    }

    public EntityId Vehicle(Vehicle vehicle)
    {
        var id = ids.Vehicle(vehicle.Id, vehicle.Prototype.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Type:vehicle.DefaultTitle.Value,
                Name:vehicle.CustomTitle.Value);
        }
        return id;
    }

    public ProductId Product(ProductProto product)
    {
        var id=ids.Product(product.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
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
        var id = ids.Recipe(recipe.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Name:recipe.Strings.Name.TranslatedString,
                Type:null
                );
        }
        return id;
    }
}