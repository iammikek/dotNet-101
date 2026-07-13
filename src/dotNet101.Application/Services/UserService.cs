using dotNet101.Application.Abstractions;
using dotNet101.Application.Exceptions;
using dotNet101.Application.Models;
using dotNet101.Domain.Entities;

namespace dotNet101.Application.Services;

public sealed class UserService
{
    private readonly IAppStore _store;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IAppStore store, IPasswordHasher passwordHasher)
    {
        _store = store;
        _passwordHasher = passwordHasher;
    }

    public UserResponse Register(UserCreateRequest request)
    {
        lock (_store.SyncRoot)
        {
            if (_store.Users.Any(user => string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserEmailExistsException(request.Email);
            }

            var user = new User
            {
                Id = _store.NextUserId(),
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password)
            };

            _store.Users.Add(user);
            return UserResponse.From(user);
        }
    }

    public User? Authenticate(string email, string password)
    {
        lock (_store.SyncRoot)
        {
            var user = _store.Users.FirstOrDefault(candidate =>
                string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return null;
            }

            return _passwordHasher.VerifyPassword(password, user.PasswordHash) ? Clone(user) : null;
        }
    }

    public User? GetByEmail(string email)
    {
        lock (_store.SyncRoot)
        {
            var user = _store.Users.FirstOrDefault(candidate =>
                string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase));

            return user is null ? null : Clone(user);
        }
    }

    private static User Clone(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        PasswordHash = user.PasswordHash
    };
}
