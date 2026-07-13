using System.Net.Mime;
using System.Text.Json;
using dotNet101.Application.Abstractions;
using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Application.Services;
using dotNet101.Application.Validation;
using dotNet101.Infrastructure.Persistence;
using dotNet101.Infrastructure.RateLimiting;
using dotNet101.Infrastructure.Security;
using dotNet101.Infrastructure.Time;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<IAppStore, AppStore>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<RequestRateLimiter>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<ItemService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ApiException exception)
    {
        context.Response.StatusCode = exception.StatusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Detail = exception.Detail,
            Code = exception.Code
        });
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    message = "Hello from dotNet-101"
}));

app.MapGet("/health", (ITimeProvider timeProvider) =>
{
    _ = timeProvider.UtcNow;

    return Results.Ok(new
    {
        status = "ok",
        database = "connected"
    });
});

app.MapPost("/auth/register", (HttpContext context, UserCreateRequest request, UserService userService) =>
{
    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"auth:register:{ApiHelpers.GetClientId(context)}", 10, TimeSpan.FromMinutes(1));

    var validationErrors = ApiHelpers.ValidateRegisterRequest(request);
    if (validationErrors.Count > 0)
    {
        return ApiHelpers.ValidationError(validationErrors);
    }

    return Results.Json(userService.Register(request), statusCode: StatusCodes.Status201Created);
});

app.MapPost("/auth/login", async (HttpContext context, UserService userService, ITokenService tokenService) =>
{
    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"auth:login:{ApiHelpers.GetClientId(context)}", 10, TimeSpan.FromMinutes(1));

    var request = await ApiHelpers.ReadLoginRequestAsync(context.Request);
    if (request is null)
    {
        return Results.Unauthorized();
    }

    var user = userService.Authenticate(request.Value.Email, request.Value.Password);
    if (user is null)
    {
        return Results.Json(
            new { detail = "Incorrect email or password" },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    return Results.Ok(new TokenResponse
    {
        AccessToken = tokenService.CreateAccessToken(user.Email)
    });
});

app.MapGet("/auth/me", (HttpContext context, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    return authResult.Error ?? Results.Ok(UserResponse.From(authResult.User!));
});

app.MapGet("/categories", (int? skip, int? limit, CategoryService categoryService) =>
{
    var paging = ApiHelpers.ValidatePaging(skip, limit);
    return paging.Error ?? Results.Ok(categoryService.List(paging.Skip, paging.Limit));
});

app.MapGet("/categories/{categoryId:int}", (int categoryId, CategoryService categoryService) =>
    Results.Ok(categoryService.GetById(categoryId)));

app.MapPost("/categories", (HttpContext context, CategoryCreateRequest request, CategoryService categoryService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:categories:create:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    var validationErrors = ApiHelpers.ValidateCategoryCreateRequest(request);
    if (validationErrors.Count > 0)
    {
        return ApiHelpers.ValidationError(validationErrors);
    }

    return Results.Json(categoryService.Create(request), statusCode: StatusCodes.Status201Created);
});

app.MapPatch("/categories/{categoryId:int}", async (int categoryId, HttpContext context, CategoryService categoryService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:categories:update:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    var parsedRequest = await ApiHelpers.ReadCategoryUpdateRequestAsync(context.Request);
    if (parsedRequest.Error is not null)
    {
        return parsedRequest.Error;
    }

    return Results.Ok(categoryService.Update(categoryId, parsedRequest.Request!));
});

app.MapDelete("/categories/{categoryId:int}", (int categoryId, HttpContext context, CategoryService categoryService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:categories:delete:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    categoryService.Delete(categoryId);
    return Results.NoContent();
});

app.MapGet("/items", (
    int? skip,
    int? limit,
    [FromQuery(Name = "min_price")] decimal? minPrice,
    [FromQuery(Name = "max_price")] decimal? maxPrice,
    [FromQuery(Name = "category_id")] int? categoryId,
    [FromQuery(Name = "name_contains")] string? nameContains,
    ItemService itemService) =>
{
    var paging = ApiHelpers.ValidatePaging(skip, limit);
    if (paging.Error is not null)
    {
        return paging.Error;
    }

    var filterValidationErrors = ApiHelpers.ValidateItemFilters(minPrice, maxPrice, categoryId, nameContains);
    if (filterValidationErrors.Count > 0)
    {
        return ApiHelpers.ValidationError(filterValidationErrors);
    }

    var filters = new ItemListFilters
    {
        MinPrice = minPrice,
        MaxPrice = maxPrice,
        CategoryId = categoryId,
        NameContains = nameContains
    };

    return Results.Ok(itemService.List(paging.Skip, paging.Limit, filters));
});

app.MapGet("/items/stats/summary", (ItemService itemService) =>
    Results.Ok(itemService.GetStats()));

app.MapGet("/items/{itemId:int}", (int itemId, ItemService itemService) =>
    Results.Ok(itemService.GetById(itemId)));

app.MapPost("/items", (HttpContext context, ItemCreateRequest request, ItemService itemService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:items:create:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    var validationErrors = ApiHelpers.ValidateItemCreateRequest(request);
    if (validationErrors.Count > 0)
    {
        return ApiHelpers.ValidationError(validationErrors);
    }

    return Results.Json(itemService.Create(request), statusCode: StatusCodes.Status201Created);
});

app.MapPatch("/items/{itemId:int}", async (int itemId, HttpContext context, ItemService itemService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:items:update:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    var parsedRequest = await ApiHelpers.ReadItemUpdateRequestAsync(context.Request);
    if (parsedRequest.Error is not null)
    {
        return parsedRequest.Error;
    }

    return Results.Ok(itemService.Update(itemId, parsedRequest.Request!));
});

app.MapDelete("/items/{itemId:int}", (int itemId, HttpContext context, ItemService itemService, UserService userService, ITokenService tokenService) =>
{
    var authResult = ApiHelpers.RequireUser(context, userService, tokenService);
    if (authResult.Error is not null)
    {
        return authResult.Error;
    }

    var limiter = context.RequestServices.GetRequiredService<RequestRateLimiter>();
    limiter.Consume($"write:items:delete:{ApiHelpers.GetClientId(context)}", 60, TimeSpan.FromMinutes(1));

    itemService.Delete(itemId);
    return Results.NoContent();
});

app.Run();

public partial class Program;

file static class ApiHelpers
{
    public static string GetClientId(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IResult ValidationError(IReadOnlyList<ValidationIssue> issues) =>
        Results.Json(new { detail = issues }, statusCode: StatusCodes.Status422UnprocessableEntity);

    public static (int Skip, int Limit, IResult? Error) ValidatePaging(int? skip, int? limit)
    {
        var errors = new List<ValidationIssue>();
        var resolvedSkip = skip ?? 0;
        var resolvedLimit = limit ?? 10;

        if (resolvedSkip < 0)
        {
            errors.Add(new ValidationIssue(["query", "skip"], "Input should be greater than or equal to 0", "greater_than_equal", resolvedSkip));
        }

        if (resolvedLimit < 1)
        {
            errors.Add(new ValidationIssue(["query", "limit"], "Input should be greater than or equal to 1", "greater_than_equal", resolvedLimit));
        }

        if (resolvedLimit > 100)
        {
            errors.Add(new ValidationIssue(["query", "limit"], "Input should be less than or equal to 100", "less_than_equal", resolvedLimit));
        }

        return errors.Count > 0
            ? (0, 0, ValidationError(errors))
            : (resolvedSkip, resolvedLimit, null);
    }

    public static List<ValidationIssue> ValidateRegisterRequest(UserCreateRequest request)
    {
        var errors = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length < 5)
        {
            errors.Add(new ValidationIssue(["body", "email"], "String should have at least 5 characters", "string_too_short", request.Email));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add(new ValidationIssue(["body", "password"], "String should have at least 8 characters", "string_too_short", request.Password));
        }

        return errors;
    }

    public static List<ValidationIssue> ValidateCategoryCreateRequest(CategoryCreateRequest request)
    {
        var errors = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationIssue(["body", "name"], "String should have at least 1 character", "string_too_short", request.Name));
        }

        if (request.Name.Length > 100)
        {
            errors.Add(new ValidationIssue(["body", "name"], "String should have at most 100 characters", "string_too_long", request.Name));
        }

        return errors;
    }

    public static List<ValidationIssue> ValidateItemCreateRequest(ItemCreateRequest request)
    {
        var errors = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationIssue(["body", "name"], "String should have at least 1 character", "string_too_short", request.Name));
        }

        if (request.Name.Length > 255)
        {
            errors.Add(new ValidationIssue(["body", "name"], "String should have at most 255 characters", "string_too_long", request.Name));
        }

        if (request.Price <= 0)
        {
            errors.Add(new ValidationIssue(["body", "price"], "Input should be greater than 0", "greater_than", request.Price));
        }

        return errors;
    }

    public static List<ValidationIssue> ValidateItemFilters(decimal? minPrice, decimal? maxPrice, int? categoryId, string? nameContains)
    {
        var errors = new List<ValidationIssue>();
        if (minPrice.HasValue && minPrice.Value <= 0)
        {
            errors.Add(new ValidationIssue(["query", "min_price"], "Input should be greater than 0", "greater_than", minPrice.Value));
        }

        if (maxPrice.HasValue && maxPrice.Value <= 0)
        {
            errors.Add(new ValidationIssue(["query", "max_price"], "Input should be greater than 0", "greater_than", maxPrice.Value));
        }

        if (categoryId.HasValue && categoryId.Value < 1)
        {
            errors.Add(new ValidationIssue(["query", "category_id"], "Input should be greater than or equal to 1", "greater_than_equal", categoryId.Value));
        }

        if (nameContains is not null && nameContains.Length == 0)
        {
            errors.Add(new ValidationIssue(["query", "name_contains"], "String should have at least 1 character", "string_too_short", nameContains));
        }

        return errors;
    }

    public static List<ValidationIssue> ValidateCategoryUpdateRequest(CategoryUpdateRequest request)
    {
        var errors = new List<ValidationIssue>();
        if (request.HasName)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add(new ValidationIssue(["body", "name"], "String should have at least 1 character", "string_too_short", request.Name));
            }
            else if (request.Name.Length > 100)
            {
                errors.Add(new ValidationIssue(["body", "name"], "String should have at most 100 characters", "string_too_long", request.Name));
            }
        }

        return errors;
    }

    public static List<ValidationIssue> ValidateItemUpdateRequest(ItemUpdateRequest request)
    {
        var errors = new List<ValidationIssue>();
        if (request.HasName)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add(new ValidationIssue(["body", "name"], "String should have at least 1 character", "string_too_short", request.Name));
            }
            else if (request.Name.Length > 255)
            {
                errors.Add(new ValidationIssue(["body", "name"], "String should have at most 255 characters", "string_too_long", request.Name));
            }
        }

        if (request.HasPrice && (!request.Price.HasValue || request.Price.Value <= 0))
        {
            errors.Add(new ValidationIssue(["body", "price"], "Input should be greater than 0", "greater_than", request.Price));
        }

        return errors;
    }

    public static async Task<(string Email, string Password)?> ReadLoginRequestAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            return (form["username"].ToString(), form["password"].ToString());
        }

        try
        {
            var body = await JsonSerializer.DeserializeAsync<LoginBody>(request.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (body is null)
            {
                return null;
            }

            return (body.Username ?? string.Empty, body.Password ?? string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static async Task<(CategoryUpdateRequest? Request, IResult? Error)> ReadCategoryUpdateRequestAsync(HttpRequest request)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body);
            var root = document.RootElement;
            var model = new CategoryUpdateRequest
            {
                HasName = root.TryGetProperty("name", out var nameElement),
                Name = root.TryGetProperty("name", out nameElement) && nameElement.ValueKind != JsonValueKind.Null ? nameElement.GetString() : null,
                HasDescription = root.TryGetProperty("description", out var descriptionElement),
                Description = root.TryGetProperty("description", out descriptionElement) && descriptionElement.ValueKind != JsonValueKind.Null
                    ? descriptionElement.GetString()
                    : null
            };

            var errors = ValidateCategoryUpdateRequest(model);
            return errors.Count > 0 ? (null, ValidationError(errors)) : (model, null);
        }
        catch (JsonException)
        {
            return (null, ValidationError([new ValidationIssue(["body"], "Invalid JSON body", "json_invalid")]));
        }
    }

    public static async Task<(ItemUpdateRequest? Request, IResult? Error)> ReadItemUpdateRequestAsync(HttpRequest request)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body);
            var root = document.RootElement;

            var model = new ItemUpdateRequest
            {
                HasName = root.TryGetProperty("name", out var nameElement),
                Name = root.TryGetProperty("name", out nameElement) && nameElement.ValueKind != JsonValueKind.Null ? nameElement.GetString() : null,
                HasDescription = root.TryGetProperty("description", out var descriptionElement),
                Description = root.TryGetProperty("description", out descriptionElement) && descriptionElement.ValueKind != JsonValueKind.Null
                    ? descriptionElement.GetString()
                    : null,
                HasPrice = root.TryGetProperty("price", out var priceElement),
                Price = root.TryGetProperty("price", out priceElement) && priceElement.ValueKind != JsonValueKind.Null ? priceElement.GetDecimal() : null,
                HasCategoryId = root.TryGetProperty("category_id", out var categoryElement),
                CategoryId = root.TryGetProperty("category_id", out categoryElement) && categoryElement.ValueKind != JsonValueKind.Null ? categoryElement.GetInt32() : null
            };

            var errors = ValidateItemUpdateRequest(model);
            return errors.Count > 0 ? (null, ValidationError(errors)) : (model, null);
        }
        catch (JsonException)
        {
            return (null, ValidationError([new ValidationIssue(["body"], "Invalid JSON body", "json_invalid")]));
        }
        catch (FormatException)
        {
            return (null, ValidationError([new ValidationIssue(["body"], "Invalid JSON body", "json_invalid")]));
        }
        catch (InvalidOperationException)
        {
            return (null, ValidationError([new ValidationIssue(["body"], "Invalid JSON body", "json_invalid")]));
        }
    }

    public static (dotNet101.Domain.Entities.User? User, IResult? Error) RequireUser(HttpContext context, UserService userService, ITokenService tokenService)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, Results.Json(new { detail = "Not authenticated" }, statusCode: StatusCodes.Status401Unauthorized));
        }

        var token = header["Bearer ".Length..].Trim();
        var subject = tokenService.ValidateAndGetSubject(token);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return (null, Results.Json(new { detail = "Could not validate credentials" }, statusCode: StatusCodes.Status401Unauthorized));
        }

        var user = userService.GetByEmail(subject);
        if (user is null)
        {
            return (null, Results.Json(new { detail = "Could not validate credentials" }, statusCode: StatusCodes.Status401Unauthorized));
        }

        return (user, null);
    }

    private sealed class LoginBody
    {
        public string? Username { get; init; }
        public string? Password { get; init; }
    }
}
