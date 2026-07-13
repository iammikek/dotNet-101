using dotNet101.Domain.Entities;

namespace dotNet101.Application.Abstractions;

public interface IAppStore
{
    object SyncRoot { get; }
    List<User> Users { get; }
    List<Category> Categories { get; }
    List<Item> Items { get; }
    int NextUserId();
    int NextCategoryId();
    int NextItemId();
    void Reset();
}
