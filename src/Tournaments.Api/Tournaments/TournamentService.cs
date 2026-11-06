using Microsoft.EntityFrameworkCore;
using Tournaments.Api.Data;
using Tournaments.Api.Identity;
using Tournaments.Domain;
namespace Tournaments.Api.Tournaments;

public sealed record CreateTournamentRequest(string Name, string System);
public sealed record AddPlayerRequest(Guid PlayerId, int? Seed);
public sealed record EntryResponse(Guid PlayerId, int? Seed);
public sealed record MatchResponse(Guid Id, int RoundNumber, int IndexInRound, Guid? PlayerAId, Guid? PlayerBId, Guid? WinnerId);
public sealed record TournamentResponse(Guid Id, string Name, string System, string Status, IReadOnlyList<EntryResponse> Players, IReadOnlyList<MatchResponse> Matches);

public sealed class InvalidTournamentException(string message) : Exception(message);
public sealed class TournamentNotFoundException : Exception;
public sealed class UnknownPlayerException : Exception;
public sealed class DuplicatePlayerException : Exception;
public sealed class TournamentClosedException : Exception;

public sealed class TournamentService(TournamentsDbContext db, IIdentityPlayers identity)
{
    private static readonly string[] Systems = ["RoundRobin", "SingleElimination", "Swiss"];

    public async Task<TournamentResponse> Create(CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidTournamentException("Нужно название турнира.");

        if (!Systems.Contains(request.System))
            throw new InvalidTournamentException("Неизвестная система турнира.");

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            System = request.System,
            Status = "Draft"
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        return ToResponse(tournament);
    }

    public async Task AddPlayer(Guid tournamentId, AddPlayerRequest request)
    {
        var tournament = await db.Tournaments
            .Include(item => item.Players)
            .FirstOrDefaultAsync(item => item.Id == tournamentId)
            ?? throw new TournamentNotFoundException();

        if (tournament.Status != "Draft")
            throw new TournamentClosedException();

        if (request.Seed is < 1)
            throw new InvalidTournamentException("Посев начинается с 1.");

        if (tournament.Players.Any(player => player.PlayerId == request.PlayerId))
            throw new DuplicatePlayerException();

        if (request.Seed is int seed && tournament.Players.Any(player => player.Seed == seed))
            throw new DuplicatePlayerException();

        if (!await identity.Exists(request.PlayerId))
            throw new UnknownPlayerException();

        tournament.Players.Add(new Entry
        {
            TournamentId = tournament.Id,
            PlayerId = request.PlayerId,
            Seed = request.Seed
        });
        await db.SaveChangesAsync();
    }
    public async Task<TournamentResponse> BuildBracket(Guid tournamentId)
{
    var tournament = await db.Tournaments
        .Include(item => item.Players)
        .Include(item => item.Matches)
        .FirstOrDefaultAsync(item => item.Id == tournamentId)
        ?? throw new TournamentNotFoundException();

    if (tournament.Status != "Draft" || tournament.Matches.Count > 0)
        throw new TournamentClosedException();

    if (tournament.Players.Count < 2)
        throw new InvalidTournamentException("Нужно минимум два участника.");

    var ordered = tournament.Players
        .OrderBy(player => player.Seed ?? int.MaxValue)
        .ThenBy(player => player.PlayerId)
        .ToList();

    if (tournament.System == "SingleElimination" && ordered.Any(player => player.Seed is null))
        throw new InvalidTournamentException("Для олимпийской системы нужен посев.");

    var domainIds = ordered.Select((player, index) => (player.PlayerId, DomainId: index + 1)).ToList();
    var toGuid = domainIds.ToDictionary(item => item.DomainId, item => item.PlayerId);
    var ids = domainIds.Select(item => item.DomainId).ToList();

    var matches = tournament.System switch
    {
        "RoundRobin" => RoundRobinMatches(tournament.Id, ids, toGuid),
        "SingleElimination" => EliminationMatches(tournament.Id, ids, toGuid),
        "Swiss" => SwissMatches(tournament.Id, ids, toGuid),
        _ => throw new InvalidTournamentException("Неизвестная система турнира.")
    };

    tournament.Status = "Started";
   db.Matches.AddRange(matches);
    await db.SaveChangesAsync();
    return ToResponse(tournament);
}

private static List<Match> RoundRobinMatches(Guid tournamentId, List<int> ids, Dictionary<int, Guid> toGuid)
{
    var matches = new List<Match>();
    foreach (var round in RoundRobinScheduler.Generate(ids))
    {
        for (var index = 0; index < round.Matches.Count; index++)
        {
            var pair = round.Matches[index];
            matches.Add(new Match
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                RoundNumber = round.Number,
                IndexInRound = index,
                PlayerAId = toGuid[pair.HomeId],
                PlayerBId = toGuid[pair.AwayId]
            });
        }
    }

    return matches;
}

private static List<Match> EliminationMatches(Guid tournamentId, List<int> ids, Dictionary<int, Guid> toGuid)
{
    var bracket = EliminationAdvancer.AdvanceByes(SingleEliminationScheduler.Generate(ids));
    var matches = new List<Match>();

    foreach (var round in bracket.Rounds)
    {
        foreach (var pair in round)
        {
            matches.Add(new Match
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                RoundNumber = pair.RoundNumber,
                IndexInRound = pair.IndexInRound,
                PlayerAId = pair.PlayerAId is int playerA ? toGuid[playerA] : null,
                PlayerBId = pair.PlayerBId is int playerB ? toGuid[playerB] : null,
                WinnerId = pair.WinnerId is int winner ? toGuid[winner] : null
            });
        }
    }

    return matches;
}

private static List<Match> SwissMatches(Guid tournamentId, List<int> ids, Dictionary<int, Guid> toGuid)
{
    var pairing = SwissScheduler.Pair(ids.Select(id => new SwissPlayer(id, 0, [], false)).ToList());
    var matches = pairing.Matches.Select((pair, index) => new Match
    {
        Id = Guid.NewGuid(),
        TournamentId = tournamentId,
        RoundNumber = 1,
        IndexInRound = index,
        PlayerAId = toGuid[pair.PlayerAId],
        PlayerBId = toGuid[pair.PlayerBId]
    }).ToList();

    if (pairing.ByePlayerId is int bye)
    {
        matches.Add(new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            RoundNumber = 1,
            IndexInRound = matches.Count,
            PlayerAId = toGuid[bye],
            WinnerId = toGuid[bye]
        });
    }

    return matches;
}
    private static TournamentResponse ToResponse(Tournament tournament) => new(
        tournament.Id,
        tournament.Name,
        tournament.System,
        tournament.Status,
        tournament.Players.Select(player => new EntryResponse(player.PlayerId, player.Seed)).ToList(),
       tournament.Matches
    .OrderBy(match => match.RoundNumber)
    .ThenBy(match => match.IndexInRound)
    .Select(match => new MatchResponse(
        match.Id,
        match.RoundNumber,
        match.IndexInRound,
        match.PlayerAId,
        match.PlayerBId,
        match.WinnerId)).ToList());
}