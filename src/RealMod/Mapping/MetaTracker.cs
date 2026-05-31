using System.Collections;
using System.Collections.Generic;
using CoiTelemetry.RealMod.Contracts.Dtos;
using CoiTelemetry.RealMod.Contracts.Ids;
using Mafi;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;

namespace CoiTelemetry.RealMod.Mapping;

public sealed class MetaTracker
{
    private readonly IEntitiesManager _entitiesManager;
    private readonly ProtosDb _db;
    private readonly Dictionary<string, MetaProto> _metaProtos = new();
    private readonly Dictionary<string, MetaEntity> _metaEntities = new();
    private readonly Dictionary<string, MetaProduct> _metaProducts = new();
    private readonly Dictionary<string, MetaRecipe> _metaRecipes = new();
    
    private readonly Dictionary<Mafi.Core.EntityId, EntityId> _entities = new();
    private readonly Dictionary<ProductProto.ID, ProductId> _products = new();
    private readonly Dictionary<ProductId, ProductProto> _productProtos = new();
    private readonly Dictionary<RecipeProto.ID, RecipeId> _recipes = new();

    public MetaTracker(ProtosDb db, IEntitiesManager entitiesManager)
    {
        _entitiesManager = entitiesManager;
        _db = db;
    }

    public Metadata BuildMetadata()
    {
        return new Metadata(
            Protos:_metaProtos,
            Entities:_metaEntities,
            Products:_metaProducts,
            Recipes:_metaRecipes
            );
    }
    private string GetEntityType(IEntity entity)
    {
        if (entity is FuelStation)
        {
            return "fuelStation";
        }
        if (entity is MineTower)
        {
            return "mineTower";
        }
        if (entity is Machine)
        {
            return "machine";
        }

        if (entity is Vehicle)
        {
            return "vehicle";
        }

        return "entity";
    }

    private string GetMetaProto(IProto proto)
    {
        var id = $"proto:{proto.Id.Value.LowerFirstChar()}";
        if( _metaProtos.TryGetValue(id, out var metaProto))
        {
            return id;
        }

        metaProto = new MetaProto(proto.Strings.Name.TranslatedString);
        _metaProtos[id] = metaProto;
        return id;
    }
    
    public EntityId Entity(Mafi.Core.EntityId id)
    {
        if (_entities.TryGetValue(id, out EntityId entityId))
        {
            return entityId;
        }
        var entity = _entitiesManager.GetEntity(id).ValueOrNull;
        if (entity == null)
        {
            return new EntityId(id.ToString());
        }
        var entityType = GetEntityType(entity);
        entityId =  new EntityId($"{entityType}:{id}");
        _entities[id] = entityId;
        var metaProtoId = GetMetaProto(entity.Prototype);
        _metaEntities[entityId.Value] = new MetaEntity( entity.GetTitle(), metaProtoId);
        return entityId;
    }

    public bool TryGetProduct(ProductId productId, out ProductProto product)
    {
        return _productProtos.TryGetValue(productId, out product!);
    }
    public ProductId Product(ProductProto.ID id)
    {
        if (_products.TryGetValue(id, out ProductId productId))
        {
            return productId;
        }

        var product = _db.Get<ProductProto>(id).ValueOrNull;
        if (product == null)
        {
            return new ProductId(id.Value);
        }

        productId = new ProductId($"product:{id.Value.Substring(8).LowerFirstChar()}");
        _products[id] = productId;
        _productProtos[productId] = product;
        _metaProducts[productId.Value] = new MetaProduct( product.Strings.Name.TranslatedString, product.IconPath);
        return productId;
    }

    public RecipeId Recipe(RecipeProto.ID id)
    {
        if (_recipes.TryGetValue(id, out var recipeId))
        {
            return recipeId;
        }

        var recipe = _db.Get<RecipeProto>(id).ValueOrNull;
        if (recipe == null)
        {
            return new RecipeId(id.Value);
        }
        recipeId = new RecipeId($"recipe:{id.Value.LowerFirstChar()}");
        _recipes[id] = recipeId;
        _metaRecipes[recipeId.Value] = new MetaRecipe( recipe.Strings.Name.TranslatedString);
        return recipeId;
    }
}