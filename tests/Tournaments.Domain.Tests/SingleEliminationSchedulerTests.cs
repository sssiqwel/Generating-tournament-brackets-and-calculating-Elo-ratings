using Tournaments.Domain;

namespace Tournaments.Domain.Tests;

public class SingleEliminationSchedulerTests
{
    [Fact]
    public void Eight_players_get_standard_first_round()
    {
        var bracket = SingleEliminationScheduler.Generate([1, 2, 3, 4, 5, 6, 7, 8]);
        var firstRound = bracket.Rounds[0];

        Assert.Equal(8, bracket.BracketSize);
        Assert.Equal(3, bracket.Rounds.Count);
        Assert.Equal([1, 8], Players(firstRound[0]));
        Assert.Equal([4, 5], Players(firstRound[1]));
        Assert.Equal([2, 7], Players(firstRound[2]));
        Assert.Equal([3, 6], Players(firstRound[3]));
        Assert.All(firstRound, match => Assert.False(match.IsBye));
        Assert.Null(bracket.Rounds[2][0].NextMatchIndex);
    }

    [Fact]
    public void Five_players_give_byes_to_three_strongest()
    {
        var bracket = SingleEliminationScheduler.Generate([1, 2, 3, 4, 5]);
        var firstRound = bracket.Rounds[0];

        Assert.Equal(8, bracket.BracketSize);
        Assert.Equal(3, bracket.Rounds.Count);
        Assert.Equal([1, null], Players(firstRound[0]));
        Assert.Equal([4, 5], Players(firstRound[1]));
        Assert.Equal([2, null], Players(firstRound[2]));
        Assert.Equal([3, null], Players(firstRound[3]));
        Assert.Equal(3, firstRound.Count(match => match.IsBye));
        Assert.All(bracket.Rounds[1], match => Assert.Null(match.PlayerAId));
    }

    [Fact]
    public void Three_players_put_strongest_against_bye()
    {
        var firstRound = SingleEliminationScheduler.Generate([1, 2, 3]).Rounds[0];

        Assert.Equal([1, null], Players(firstRound[0]));
        Assert.Equal([2, 3], Players(firstRound[1]));
        Assert.Equal(0, firstRound[0].NextMatchIndex);
        Assert.Equal(0, firstRound[1].NextMatchIndex);
        Assert.True(firstRound[0].FeedsPlayerA);
        Assert.False(firstRound[1].FeedsPlayerA);
    }

    [Fact]
    public void Top_two_seeds_are_in_opposite_halves()
    {
        var firstRound = SingleEliminationScheduler.Generate([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]).Rounds[0];
        var half = firstRound.Count / 2;

        var seedOne = Assert.Single(firstRound.Select((match, index) => (match, index)), pair => pair.match.PlayerAId == 1 || pair.match.PlayerBId == 1).index;
        var seedTwo = Assert.Single(firstRound.Select((match, index) => (match, index)), pair => pair.match.PlayerAId == 2 || pair.match.PlayerBId == 2).index;

        Assert.True(seedOne < half);
        Assert.True(seedTwo >= half);
        Assert.Equal(3, firstRound.Count(match => match.IsBye));
    }

    [Fact]
    public void Rejects_too_few_or_repeated_players()
    {
        Assert.Throws<ArgumentException>(() => SingleEliminationScheduler.Generate([7]));
        Assert.Throws<ArgumentException>(() => SingleEliminationScheduler.Generate([7, 7]));
        Assert.Throws<ArgumentNullException>(() => SingleEliminationScheduler.Generate(null!));
    }

    private static int?[] Players(EliminationMatch match) => [match.PlayerAId, match.PlayerBId];
}