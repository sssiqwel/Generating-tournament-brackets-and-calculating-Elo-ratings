using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Identity.Api.Auth;
using Identity.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_stores_hash_instead_of_password()
    {
        var auth = CreateService();

        var user = await auth.Register(new RegisterRequest("Player@Mail.com", "secret123", "Игрок"));

        Assert.Equal("player@mail.com", user.Email);
        Assert.Equal(["Player"], user.Roles);
        Assert.NotEqual("secret123", user.DisplayName);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var auth = CreateService();
        await auth.Register(new RegisterRequest("player@mail.com", "secret123", "Игрок"));

        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            auth.Register(new RegisterRequest("PLAYER@mail.com", "secret123", "Второй")));
    }

    [Fact]
    public async Task Login_returns_token_with_player_role()
    {
        var auth = CreateService();
        var user = await auth.Register(new RegisterRequest("player@mail.com", "secret123", "Игрок"));

        var login = await auth.Login(new LoginRequest("player@mail.com", "secret123"));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);

        Assert.Equal(user.Id, login.UserId);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Player");
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        var auth = CreateService();
        await auth.Register(new RegisterRequest("player@mail.com", "secret123", "Игрок"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            auth.Login(new LoginRequest("player@mail.com", "wrong-password")));
    }

    private static AuthService CreateService()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthService(
            new IdentityDbContext(options),
            new JwtOptions("identity", "tournaments", "dev-only-identity-key-32-characters"));
    }
}