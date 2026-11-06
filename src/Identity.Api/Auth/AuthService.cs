using Identity.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string[] Roles);

public sealed record LoginResponse(string AccessToken, Guid UserId, string[] Roles);

public sealed class DuplicateEmailException : Exception;

public sealed class InvalidCredentialsException : Exception;

public sealed class AuthService(IdentityDbContext db, JwtOptions jwtOptions)
{
    public async Task<UserResponse> Register(RegisterRequest request)
    {
        var email = Normalize(request.Email);
        if (await db.Users.AnyAsync(user => user.Email == email))
            throw new DuplicateEmailException();

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "Player"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return ToResponse(user);
    }

    public async Task<LoginResponse> Login(LoginRequest request)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(account => account.Email == email);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return new LoginResponse(JwtTokenFactory.Create(user, jwtOptions), user.Id, [user.Role]);
    }

    public async Task<UserResponse?> Find(Guid userId)
    {
        var user = await db.Users.SingleOrDefaultAsync(account => account.Id == userId);
        return user is null ? null : ToResponse(user);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(UserAccount user) =>
        new(user.Id, user.Email, user.DisplayName, [user.Role]);
}