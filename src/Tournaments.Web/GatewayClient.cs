using System.Net.Http.Json;

namespace Tournaments.Web;

public sealed class GatewayClient(HttpClient http, PlayerSession session)
{
    public async Task<string?> Login(string email, string password)
    {
        var response = await http.PostAsJsonAsync("/identity/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode)
            return "Не удалось войти. Проверьте почту и пароль.";

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        session.AccessToken = body!.AccessToken;
        session.UserId = body.UserId;
        session.Email = email;
        return null;
    }

    public async Task<PlayerRating[]> Leaderboard()
    {
        return await http.GetFromJsonAsync<PlayerRating[]>("/rating/leaderboard") ?? [];
    }
public async Task<TournamentItem[]> Tournaments()
{
    return await http.GetFromJsonAsync<TournamentItem[]>("/tournaments/tournaments") ?? [];
}
public sealed record TournamentItem(Guid Id, string Name, string System, string Status);
    private sealed record LoginResponse(string AccessToken, Guid UserId, string[] Roles);
    public sealed record PlayerRating(Guid PlayerId, int Rating);
}