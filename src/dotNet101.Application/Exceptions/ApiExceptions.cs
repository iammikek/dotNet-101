namespace dotNet101.Application.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(int statusCode, string detail, string code)
        : base(detail)
    {
        StatusCode = statusCode;
        Detail = detail;
        Code = code;
    }

    public int StatusCode { get; }
    public string Detail { get; }
    public string Code { get; }
}

public sealed class ItemNotFoundException : ApiException
{
    public ItemNotFoundException(int itemId)
        : base(404, "Item not found", "ITEM_NOT_FOUND")
    {
        ItemId = itemId;
    }

    public int ItemId { get; }
}

public sealed class CategoryNotFoundException : ApiException
{
    public CategoryNotFoundException(int categoryId)
        : base(404, "Category not found", "CATEGORY_NOT_FOUND")
    {
        CategoryId = categoryId;
    }

    public int CategoryId { get; }
}

public sealed class CategoryInUseException : ApiException
{
    public CategoryInUseException(int categoryId)
        : base(409, "Category has items and cannot be deleted", "CATEGORY_IN_USE")
    {
        CategoryId = categoryId;
    }

    public int CategoryId { get; }
}

public sealed class CategoryNameExistsException : ApiException
{
    public CategoryNameExistsException(string name)
        : base(409, "Category name already exists", "CATEGORY_NAME_EXISTS")
    {
        Name = name;
    }

    public string Name { get; }
}

public sealed class UserEmailExistsException : ApiException
{
    public UserEmailExistsException(string email)
        : base(409, "User email already exists", "USER_EMAIL_EXISTS")
    {
        Email = email;
    }

    public string Email { get; }
}

public sealed class RateLimitExceededException : ApiException
{
    public RateLimitExceededException()
        : base(429, "Rate limit exceeded", "RATE_LIMIT_EXCEEDED")
    {
    }
}
