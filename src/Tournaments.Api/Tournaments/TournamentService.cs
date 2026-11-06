using Microsoft.EntityFrameworkCore;
using Tournaments.Api.Data;
using Tournaments.Api.Identity;

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

    private static TournamentResponse ToResponse(Tournament tournament) => new(
        tournament.Id,
        tournament.Name,
        tournament.System,
        tournament.Status,
        tournament.Players.Select(player => new EntryResponse(player.PlayerId, player.Seed)).ToList(),
        tournament.Matches.Select(match => new MatchResponse(
            match.Id,
            match.RoundNumber,
            match.IndexInRound,
            match.PlayerAId,
            match.PlayerBId,
            match.WinnerId)).ToList());
}