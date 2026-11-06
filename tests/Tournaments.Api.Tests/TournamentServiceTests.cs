using Microsoft.EntityFrameworkCore;
using Tournaments.Api.Data;
using Tournaments.Api.Identity;
using Tournaments.Api.Tournaments;

namespace Tournaments.Api.Tests;

public sealed class TournamentServiceTests
{
    [Fact]
    public async Task Create_saves_draft()
    {
        await using var db = NewDb();
        var service = new TournamentService(db, new FakePlayers(true));

        var tournament = await service.Create(new CreateTournamentRequest("Кубок", "SingleElimination"));

        Assert.Equal("Draft", tournament.Status);
        Assert.Equal("Кубок", tournament.Name);
        Assert.Empty(tournament.Players);
        Assert.Empty(tournament.Matches);
    }

    [Fact]
    public async Task Create_rejects_unknown_system()
    {
        await using var db = NewDb();
        var service = new TournamentService(db, new FakePlayers(true));

        await Assert.ThrowsAsync<InvalidTournamentException>(() =>
            service.Create(new CreateTournamentRequest("Кубок", "League")));
    }

    [Fact]
    public async Task AddPlayer_saves_entry_when_identity_knows_player()
    {
        await using var db = NewDb();
        var service = new TournamentService(db, new FakePlayers(true));
        var tournament = await service.Create(new CreateTournamentRequest("Кубок", "RoundRobin"));
        var playerId = Guid.NewGuid();

        await service.AddPlayer(tournament.Id, new AddPlayerRequest(playerId, 1));

        var saved = await db.Entries.SingleAsync();
        Assert.Equal(playerId, saved.PlayerId);
        Assert.Equal(1, saved.Seed);
    }

    [Fact]
    public async Task AddPlayer_rejects_unknown_player()
    {
        await using var db = NewDb();
        var service = new TournamentService(db, new FakePlayers(false));
        var tournament = await service.Create(new CreateTournamentRequest("Кубок", "Swiss"));

        await Assert.ThrowsAsync<UnknownPlayerException>(() =>
            service.AddPlayer(tournament.Id, new AddPlayerRequest(Guid.NewGuid(), null)));

        Assert.Empty(db.Entries);
    }

    [Fact]
    public async Task AddPlayer_rejects_duplicate()
    {
        await using var db = NewDb();
        var service = new TournamentService(db, new FakePlayers(true));
        var tournament = await service.Create(new CreateTournamentRequest("Кубок", "Swiss"));
        var playerId = Guid.NewGuid();
        await service.AddPlayer(tournament.Id, new AddPlayerRequest(playerId, null));

        await Assert.ThrowsAsync<DuplicatePlayerException>(() =>
            service.AddPlayer(tournament.Id, new AddPlayerRequest(playerId, null)));
    }
    [Fact]
public async Task BuildBracket_pairs_single_elimination_by_seed()
{
    await using var db = NewDb();
    var service = new TournamentService(db, new FakePlayers(true));
    var tournament = await service.Create(new CreateTournamentRequest("Кубок", "SingleElimination"));
    var ids = await AddPlayers(service, tournament.Id, 4);

    var started = await service.BuildBracket(tournament.Id);
    var firstRound = started.Matches.Where(match => match.RoundNumber == 1).OrderBy(match => match.IndexInRound).ToList();

    Assert.Equal("Started", started.Status);
    Assert.Equal(ids[0], firstRound[0].PlayerAId);
    Assert.Equal(ids[3], firstRound[0].PlayerBId);
    Assert.Equal(ids[1], firstRound[1].PlayerAId);
    Assert.Equal(ids[2], firstRound[1].PlayerBId);
}

[Fact]
public async Task BuildBracket_creates_round_robin_pairs()
{
    await using var db = NewDb();
    var service = new TournamentService(db, new FakePlayers(true));
    var tournament = await service.Create(new CreateTournamentRequest("Кубок", "RoundRobin"));
    await AddPlayers(service, tournament.Id, 3);

    var started = await service.BuildBracket(tournament.Id);

    Assert.Equal(3, started.Matches.Count);
    Assert.All(started.Matches, match => Assert.NotNull(match.PlayerBId));
}

[Fact]
public async Task BuildBracket_pairs_swiss_in_order()
{
    await using var db = NewDb();
    var service = new TournamentService(db, new FakePlayers(true));
    var tournament = await service.Create(new CreateTournamentRequest("Кубок", "Swiss"));
    var ids = await AddPlayers(service, tournament.Id, 4);

    var started = await service.BuildBracket(tournament.Id);
    var round = started.Matches.OrderBy(match => match.IndexInRound).ToList();

    Assert.Equal(ids[0], round[0].PlayerAId);
    Assert.Equal(ids[1], round[0].PlayerBId);
    Assert.Equal(ids[2], round[1].PlayerAId);
    Assert.Equal(ids[3], round[1].PlayerBId);
}

[Fact]
public async Task BuildBracket_rejects_second_build()
{
    await using var db = NewDb();
    var service = new TournamentService(db, new FakePlayers(true));
    var tournament = await service.Create(new CreateTournamentRequest("Кубок", "Swiss"));
    await AddPlayers(service, tournament.Id, 2);
    await service.BuildBracket(tournament.Id);

    await Assert.ThrowsAsync<TournamentClosedException>(() => service.BuildBracket(tournament.Id));
}

private static async Task<Guid[]> AddPlayers(TournamentService service, Guid tournamentId, int count)
{
    var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
    for (var seed = 1; seed <= count; seed++)
        await service.AddPlayer(tournamentId, new AddPlayerRequest(ids[seed - 1], seed));

    return ids;
}
    private static TournamentsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<TournamentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TournamentsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class FakePlayers(bool exists) : IIdentityPlayers
    {
        public Task<bool> Exists(Guid playerId) => Task.FromResult(exists);
    }
}