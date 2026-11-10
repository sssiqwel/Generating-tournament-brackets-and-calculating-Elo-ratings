using Microsoft.EntityFrameworkCore;
using Rating.Api.Data;
using Rating.Api.Rating;

namespace Rating.Api.Tests;

public sealed class RatingServiceTests
{
    [Fact]
    public async Task Apply_updates_equal_players_after_win()
    {
        await using var db = NewDb();
        var service = new RatingService(db);
        var matchId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await service.Apply(new MatchCompleted(matchId, Guid.NewGuid(), playerA, playerB, 1));

        Assert.Equal(1516, (await service.Find(playerA))!.Rating);
        Assert.Equal(1484, (await service.Find(playerB))!.Rating);
    }

    [Fact]
    public async Task Apply_ignores_same_match_twice()
    {
        await using var db = NewDb();
        var service = new RatingService(db);
        var message = new MatchCompleted(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        await service.Apply(message);
        await service.Apply(message);

        Assert.Equal(1516, (await service.Find(message.PlayerAId))!.Rating);
        Assert.Equal(1, await db.ProcessedMatches.CountAsync());
    }

    [Fact]
    public async Task Leaderboard_orders_by_rating()
    {
        await using var db = NewDb();
        var service = new RatingService(db);
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        await service.Apply(new MatchCompleted(Guid.NewGuid(), Guid.NewGuid(), winner, loser, 1));

        var board = await service.Leaderboard();

        Assert.Equal(winner, board[0].PlayerId);
        Assert.Equal(loser, board[1].PlayerId);
    }

    [Fact]
    public async Task Find_returns_null_for_unknown_player()
    {
        await using var db = NewDb();
        var service = new RatingService(db);

        Assert.Null(await service.Find(Guid.NewGuid()));
    }

    private static RatingDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new RatingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}