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
public async Task<TournamentDetails?> Tournament(Guid id)
{
    var response = await http.GetAsync($"/tournaments/tournaments/{id}");
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        return null;

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<TournamentDetails>();
}
public async Task<string?> RecordResult(Guid tournamentId, Guid matchId, double scoreA)
{
    if (string.IsNullOrWhiteSpace(session.AccessToken))
        return "Сначала войдите.";

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"/tournaments/tournaments/{tournamentId}/matches/{matchId}/result");
    request.Headers.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);
    request.Content = JsonContent.Create(new { scoreA });

    var response = await http.SendAsync(request);
    if (response.IsSuccessStatusCode)
        return null;

    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        return "Сначала войдите.";

    var problem = await response.Content.ReadFromJsonAsync<ErrorBody>();
    return problem?.Error ?? "Не удалось записать результат.";
}
public sealed record TournamentItem(Guid Id, string Name, string System, string Status);
public sealed record TournamentDetails(
    Guid Id,
    string Name,
    string System,
    string Status,
    PlayerEntry[] Players,
    MatchItem[] Matches);

public sealed record PlayerEntry(Guid PlayerId, int? Seed);

public sealed record MatchItem(
    Guid Id,
    int RoundNumber,
    int IndexInRound,
    Guid? PlayerAId,
    Guid? PlayerBId,
    Guid? WinnerId,
    double? ScoreA);
    private sealed record LoginResponse(string AccessToken, Guid UserId, string[] Roles);
    private sealed record ErrorBody(string? Error);
    public sealed record PlayerRating(Guid PlayerId, int Rating);
}