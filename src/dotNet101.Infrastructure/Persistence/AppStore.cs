using dotNet101.Application.Abstractions;
using dotNet101.Domain.Entities;

namespace dotNet101.Infrastructure.Persistence;

public sealed class AppStore : IAppStore
{
    public object SyncRoot { get; } = new();
    public List<User> Users { get; } = new();
    public List<Category> Categories { get; } = new();
    public List<Item> Items { get; } = new();

    private int _nextUserId = 1;
    private int _nextCategoryId = 1;
    private int _nextItemId = 1;

    public int NextUserId() => _nextUserId++;
    public int NextCategoryId() => _nextCategoryId++;
    public int NextItemId() => _nextItemId++;

    public void Reset()
    {
        lock (SyncRoot)
        {
            Users.Clear();
            Categories.Clear();
            Items.Clear();
            _nextUserId = 1;
            _nextCategoryId = 1;
            _nextItemId = 1;
        }
    }
}
