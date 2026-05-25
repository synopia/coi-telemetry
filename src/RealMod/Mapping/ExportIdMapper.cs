using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Ids;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;

namespace CoiTelemetry.RealMod.Mapping;


public sealed class ExportIdMapper
{
    private readonly Dictionary<Mafi.Core.EntityId, EntityId> _machineIds = new();
    private readonly Dictionary<Mafi.Core.EntityId, EntityId> _vehicleIds = new();
    private readonly Dictionary<Mafi.Core.EntityId, EntityId> _mineTowers = new();
    private readonly Dictionary<ProductProto.ID, ProductId> _productIds = new();
    private readonly Dictionary<RecipeProto.ID, RecipeId> _recipeIds = new();

    public EntityId MineTower(Mafi.Core.EntityId id)
    {
        if (_mineTowers.TryGetValue(id, out var mineTowerId))
        {
            return mineTowerId;
        }
        mineTowerId = new EntityId($"mineTower:{id.Value}");
        _mineTowers[id] = mineTowerId;
        return mineTowerId;
    }

    public EntityId Machine(Mafi.Core.EntityId id, Proto.ID protoId)
    {
        if (_machineIds.TryGetValue(id, out var machineId))
        {
            return machineId;
        }
        machineId = new EntityId($"machine:{protoId.Value}:{id.Value}");
        _machineIds[id] = machineId;
        return machineId;
    }

    public EntityId Vehicle(Mafi.Core.EntityId id, Proto.ID protoId)
    {
        if (_vehicleIds.TryGetValue(id, out var vehicleId))
        {
            return vehicleId;
        }
        vehicleId = new EntityId($"vehicle:{protoId.Value}:{id.Value}");
        _vehicleIds[id] = vehicleId;
        return vehicleId;
    }

    public ProductId Product(ProductProto.ID id)
    {
        if (_productIds.TryGetValue(id, out var productId))
        {
            return productId;
        }
        productId = new ProductId($"product:{id.Value.Substring(8)}", id);
        _productIds[id] = productId;
        return productId;
    }

    public RecipeId Recipe(RecipeProto.ID id)
    {
        if (_recipeIds.TryGetValue(id, out var recipeId))
        {
            return recipeId;
        }
        recipeId = new RecipeId($"recipe:{id.Value}");
        _recipeIds[id] = recipeId;
        return recipeId;
    }
}