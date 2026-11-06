using System.Net;

namespace Tournaments.Api.Identity;

public interface IIdentityPlayers
{
    Task<bool> Exists(Guid playerId);
}

public sealed class IdentityPlayers(HttpClient http) : IIdentityPlayers
{
    public async Task<bool> Exists(Guid playerId)
    {
        using var response = await http.GetAsync($"users/{playerId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}

public sealed class ForwardBearerHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
            request.Headers.TryAddWithoutValidation("Authorization", header);

        return base.SendAsync(request, cancellationToken);
    }
}