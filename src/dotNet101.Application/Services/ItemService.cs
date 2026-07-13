using dotNet101.Application.Abstractions;
using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Domain.Entities;

namespace dotNet101.Application.Services;

public sealed class ItemService
{
    private readonly IAppStore _store;

    public ItemService(IAppStore store)
    {
        _store = store;
    }

    public PagedResponse<ItemResponse> List(int skip, int limit, ItemListFilters filters)
    {
        lock (_store.SyncRoot)
        {
            var filtered = ApplyFilters(filters)
                .OrderBy(item => item.Id)
                .ToArray();

            var page = filtered
                .Skip(skip)
                .Take(limit)
                .Select(MapResponse)
                .ToArray();

            return new PagedResponse<ItemResponse>
            {
                Items = page,
                Total = filtered.Length,
                Skip = skip,
                Limit = limit
            };
        }
    }

    public ItemResponse GetById(int itemId)
    {
        lock (_store.SyncRoot)
        {
            var item = FindItem(itemId);
            return MapResponse(item);
        }
    }

    public ItemResponse Create(ItemCreateRequest request)
    {
        lock (_store.SyncRoot)
        {
            EnsureCategoryExists(request.CategoryId);

            var item = new Item
            {
                Id = _store.NextItemId(),
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                CategoryId = request.CategoryId
            };

            _store.Items.Add(item);
            return MapResponse(item);
        }
    }

    public ItemResponse Update(int itemId, ItemUpdateRequest request)
    {
        lock (_store.SyncRoot)
        {
            var item = FindItem(itemId);

            if (request.HasCategoryId)
            {
                EnsureCategoryExists(request.CategoryId);
                item.CategoryId = request.CategoryId;
            }

            if (request.HasName && request.Name is not null)
            {
                item.Name = request.Name;
            }

            if (request.HasDescription)
            {
                item.Description = request.Description;
            }

            if (request.HasPrice && request.Price.HasValue)
            {
                item.Price = request.Price.Value;
            }

            return MapResponse(item);
        }
    }

    public void Delete(int itemId)
    {
        lock (_store.SyncRoot)
        {
            var item = FindItem(itemId);
            _store.Items.Remove(item);
        }
    }

    public ItemStatsResponse GetStats()
    {
        lock (_store.SyncRoot)
        {
            if (_store.Items.Count == 0)
            {
                return new ItemStatsResponse
                {
                    TotalItems = 0,
                    AveragePrice = 0.0m,
                    MinPrice = null,
                    MaxPrice = null,
                    UncategorizedCount = 0,
                    ByCategory = Array.Empty<CategoryItemStatsResponse>()
                };
            }

            var averagePrice = Math.Round(_store.Items.Average(item => item.Price), 2, MidpointRounding.AwayFromZero);
            var byCategory = _store.Categories
                .Select(category => new
                {
                    Category = category,
                    Items = _store.Items.Where(item => item.CategoryId == category.Id).ToArray()
                })
                .Where(group => group.Items.Length > 0)
                .OrderBy(group => group.Category.Name, StringComparer.Ordinal)
                .Select(group => new CategoryItemStatsResponse
                {
                    CategoryId = group.Category.Id,
                    CategoryName = group.Category.Name,
                    ItemCount = group.Items.Length,
                    AveragePrice = Math.Round(group.Items.Average(item => item.Price), 2, MidpointRounding.AwayFromZero)
                })
                .ToArray();

            return new ItemStatsResponse
            {
                TotalItems = _store.Items.Count,
                AveragePrice = averagePrice,
                MinPrice = _store.Items.Min(item => item.Price),
                MaxPrice = _store.Items.Max(item => item.Price),
                UncategorizedCount = _store.Items.Count(item => item.CategoryId is null),
                ByCategory = byCategory
            };
        }
    }

    private IEnumerable<Item> ApplyFilters(ItemListFilters filters)
    {
        IEnumerable<Item> query = _store.Items;

        if (filters.MinPrice.HasValue)
        {
            query = query.Where(item => item.Price >= filters.MinPrice.Value);
        }

        if (filters.MaxPrice.HasValue)
        {
            query = query.Where(item => item.Price <= filters.MaxPrice.Value);
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(item => item.CategoryId == filters.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.NameContains))
        {
            query = query.Where(item =>
                item.Name.Contains(filters.NameContains, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private void EnsureCategoryExists(int? categoryId)
    {
        if (categoryId.HasValue && _store.Categories.All(category => category.Id != categoryId.Value))
        {
            throw new CategoryNotFoundException(categoryId.Value);
        }
    }

    private Item FindItem(int itemId) =>
        _store.Items.FirstOrDefault(candidate => candidate.Id == itemId)
        ?? throw new ItemNotFoundException(itemId);

    private ItemResponse MapResponse(Item item)
    {
        var category = item.CategoryId.HasValue
            ? _store.Categories.FirstOrDefault(candidate => candidate.Id == item.CategoryId.Value)
            : null;

        return ItemResponse.From(item, category);
    }
}
