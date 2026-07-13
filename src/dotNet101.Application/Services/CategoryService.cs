using dotNet101.Application.Abstractions;
using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Domain.Entities;

namespace dotNet101.Application.Services;

public sealed class CategoryService
{
    private readonly IAppStore _store;

    public CategoryService(IAppStore store)
    {
        _store = store;
    }

    public PagedResponse<CategoryResponse> List(int skip, int limit)
    {
        lock (_store.SyncRoot)
        {
            var items = _store.Categories
                .OrderBy(category => category.Id)
                .Skip(skip)
                .Take(limit)
                .Select(CategoryResponse.From)
                .ToArray();

            return new PagedResponse<CategoryResponse>
            {
                Items = items,
                Total = _store.Categories.Count,
                Skip = skip,
                Limit = limit
            };
        }
    }

    public CategoryResponse GetById(int categoryId)
    {
        lock (_store.SyncRoot)
        {
            var category = FindCategory(categoryId);
            return CategoryResponse.From(category);
        }
    }

    public CategoryResponse Create(CategoryCreateRequest request)
    {
        lock (_store.SyncRoot)
        {
            EnsureUniqueName(request.Name);

            var category = new Category
            {
                Id = _store.NextCategoryId(),
                Name = request.Name,
                Description = request.Description
            };

            _store.Categories.Add(category);
            return CategoryResponse.From(category);
        }
    }

    public CategoryResponse Update(int categoryId, CategoryUpdateRequest request)
    {
        lock (_store.SyncRoot)
        {
            var category = FindCategory(categoryId);

            if (request.HasName && request.Name is not null && !string.Equals(category.Name, request.Name, StringComparison.Ordinal))
            {
                EnsureUniqueName(request.Name, categoryId);
                category.Name = request.Name;
            }

            if (request.HasDescription)
            {
                category.Description = request.Description;
            }

            return CategoryResponse.From(category);
        }
    }

    public void Delete(int categoryId)
    {
        lock (_store.SyncRoot)
        {
            var category = FindCategory(categoryId);
            if (_store.Items.Any(item => item.CategoryId == categoryId))
            {
                throw new CategoryInUseException(categoryId);
            }

            _store.Categories.Remove(category);
        }
    }

    public Category? FindEntity(int categoryId)
    {
        lock (_store.SyncRoot)
        {
            var category = _store.Categories.FirstOrDefault(candidate => candidate.Id == categoryId);
            return category is null ? null : Clone(category);
        }
    }

    private Category FindCategory(int categoryId) =>
        _store.Categories.FirstOrDefault(candidate => candidate.Id == categoryId)
        ?? throw new CategoryNotFoundException(categoryId);

    private void EnsureUniqueName(string name, int? excludedCategoryId = null)
    {
        var exists = _store.Categories.Any(category =>
            category.Id != excludedCategoryId && string.Equals(category.Name, name, StringComparison.Ordinal));

        if (exists)
        {
            throw new CategoryNameExistsException(name);
        }
    }

    private static Category Clone(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description
    };
}
