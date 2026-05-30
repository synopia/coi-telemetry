using System.Collections.Generic;
using System.Linq;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;

namespace CoiTelemetry.RealMod.Mapping;


public sealed class EntityTracker(IdTracker ids)
{
    private readonly Dictionary<string, MetaInfo> _meta = new();
    private readonly Dictionary<string, MetaInfo> _protoMeta = new();
    private readonly Dictionary<string, ProductProto> _productsById = new();
    
    private IEnumerable<MetaInfo>? _metaCached = null;
    private int _sizeCached = -1;
    private int _size2Cached = -1;

    public IEnumerable<MetaInfo> Meta
    {
        get
        {
            if (_metaCached is null || _meta.Count != _sizeCached || _protoMeta.Count != _size2Cached)
            {
                _metaCached = _protoMeta.Values.OrderBy(meta => meta.Id).Concat(_meta.Values.OrderBy(meta => meta.Id)).ToArray();
                _sizeCached = _meta.Count;
                _size2Cached = _protoMeta.Count;
            }
            return _metaCached;
        }
    }


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

    public string Proto(IProto proto)
    {
        if (!_protoMeta.TryGetValue(proto.Id.Value, out var meta))
        {
            _protoMeta[proto.Id.Value] = new MetaInfo(
                Id: proto.Id.Value,
                Name: proto.Strings.Name.TranslatedString,
                Type: null
            );
        }
        return proto.Id.Value;
    }
    
    public EntityId MineTower(MineTower mineTower)
    {
        var id = ids.MineTower(mineTower.Id);
        if (!_meta.ContainsKey(id.Value))
        {
            _meta[id.Value] = new MetaInfo(
                Id:id.Value,
                Type:Proto(mineTower.Prototype),
                Name:mineTower.CustomTitle.ValueOrNull);
            
            ;
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
                Type:Proto(fuelStation.Prototype),
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
                Type:Proto(machine.Prototype),
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
                Type:Proto(vehicle.Prototype),
                Name:vehicle.CustomTitle.ValueOrNull);
        }
        return id;
    }

    public ProductId Product(ProductProto product)
    {
        var id=ids.Product(product.Id);
        _productsById[id.Value] = product;
        if (!_protoMeta.ContainsKey(id.Value))
        {
            _protoMeta[id.Value] = new MetaInfo(
                Id:id.Value,
                Name:product.Strings.Name.TranslatedString,
                Type:null
                );
        }
        return id;
    }

    public bool TryGetProduct(ProductId productId, out ProductProto product)
    {
        return _productsById.TryGetValue(productId.Value, out product!);
    }

    public RecipeId? Recipe(RecipeProto? recipe)
    {
        if (recipe is null)
        {
            return null;
        }
        var id = ids.Recipe(recipe.Id);
        if (!_protoMeta.ContainsKey(id.Value))
        {
            _protoMeta[id.Value] = new MetaInfo(
                Id:id.Value,
                Name:recipe.Strings.Name.TranslatedString,
                Type:null
                );
        }
        return id;
    }
}
