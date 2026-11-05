using Tournaments.Domain;

namespace Tournaments.Domain.Tests;

public class RoundRobinSchedulerTests
{
    [Fact]
    public void Two_players_get_one_match()
    {
        var rounds = RoundRobinScheduler.Generate([1, 2]);

        var round = Assert.Single(rounds);
        var match = Assert.Single(round.Matches);
        Assert.Equal(1, round.Number);
        Assert.Equal(new RoundRobinMatch(1, 2), match);
    }

    [Fact]
    public void Four_players_follow_circle_and_play_once()
    {
        var rounds = RoundRobinScheduler.Generate([1, 2, 3, 4]);

        Assert.Equal(3, rounds.Count);
        Assert.Equal([new RoundRobinMatch(1, 4), new RoundRobinMatch(2, 3)], rounds[0].Matches);
        Assert.Equal([new RoundRobinMatch(1, 3), new RoundRobinMatch(4, 2)], rounds[1].Matches);
        Assert.Equal([new RoundRobinMatch(1, 2), new RoundRobinMatch(3, 4)], rounds[2].Matches);

        var pairs = rounds
            .SelectMany(round => round.Matches)
            .Select(match => (Math.Min(match.HomeId, match.AwayId), Math.Max(match.HomeId, match.AwayId)))
            .ToList();

        Assert.Equal(6, pairs.Distinct().Count());
    }

    [Fact]
    public void Five_players_each_skip_one_round()
    {
        var rounds = RoundRobinScheduler.Generate([1, 2, 3, 4, 5]);

        Assert.Equal(5, rounds.Count);
        Assert.All(rounds, round => Assert.Equal(2, round.Matches.Count));

        foreach (var playerId in new[] { 1, 2, 3, 4, 5 })
        {
            var byes = rounds.Count(round => round.Matches.All(match => match.HomeId != playerId && match.AwayId != playerId));
            Assert.Equal(1, byes);
        }
    }

    [Fact]
    public void Rejects_too_few_or_repeated_players()
    {
        Assert.Throws<ArgumentException>(() => RoundRobinScheduler.Generate([1]));
        Assert.Throws<ArgumentException>(() => RoundRobinScheduler.Generate([1, 1]));
        Assert.Throws<ArgumentNullException>(() => RoundRobinScheduler.Generate(null!));
    }
}