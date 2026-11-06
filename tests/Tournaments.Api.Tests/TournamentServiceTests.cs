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