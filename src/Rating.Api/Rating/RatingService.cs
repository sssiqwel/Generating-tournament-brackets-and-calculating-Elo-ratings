using Microsoft.EntityFrameworkCore;
using Rating.Api.Data;
using Tournaments.Domain;

namespace Rating.Api.Rating;

public sealed record MatchCompleted(Guid MatchId, Guid TournamentId, Guid PlayerAId, Guid PlayerBId, double ScoreA);
public sealed record PlayerRatingResponse(Guid PlayerId, int Rating);

public sealed class RatingService(RatingDbContext db)
{
    public const int DefaultRating = 1500;
    public const int K = 32;

    public async Task Apply(MatchCompleted message)
    {
        if (await db.ProcessedMatches.AnyAsync(match => match.MatchId == message.MatchId))
            return;

        var playerA = await FindOrCreate(message.PlayerAId);
        var playerB = await FindOrCreate(message.PlayerBId);
        var beforeA = playerA.Rating;
        var beforeB = playerB.Rating;
        var (afterA, afterB) = EloCalculator.Update(beforeA, beforeB, message.ScoreA, K);

        playerA.Rating = afterA;
        playerB.Rating = afterB;
        db.RatingChanges.Add(new RatingChange
        {
            Id = Guid.NewGuid(),
            MatchId = message.MatchId,
            PlayerId = playerA.PlayerId,
            RatingBefore = beforeA,
            RatingAfter = afterA
        });
        db.RatingChanges.Add(new RatingChange
        {
            Id = Guid.NewGuid(),
            MatchId = message.MatchId,
            PlayerId = playerB.PlayerId,
            RatingBefore = beforeB,
            RatingAfter = afterB
        });
        db.ProcessedMatches.Add(new ProcessedMatch { MatchId = message.MatchId });
        await db.SaveChangesAsync();
    }

    public async Task<PlayerRatingResponse?> Find(Guid playerId)
    {
        var player = await db.Players.FindAsync(playerId);
        return player is null ? null : new PlayerRatingResponse(player.PlayerId, player.Rating);
    }

    public async Task<IReadOnlyList<PlayerRatingResponse>> Leaderboard()
    {
        return await db.Players
            .OrderByDescending(player => player.Rating)
            .ThenBy(player => player.PlayerId)
            .Select(player => new PlayerRatingResponse(player.PlayerId, player.Rating))
            .ToListAsync();
    }

    private async Task<PlayerRating> FindOrCreate(Guid playerId)
    {
        var player = await db.Players.FindAsync(playerId);
        if (player is not null)
            return player;

        player = new PlayerRating { PlayerId = playerId, Rating = DefaultRating };
        db.Players.Add(player);
        return player;
    }
}