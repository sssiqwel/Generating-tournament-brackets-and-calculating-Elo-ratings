using Tournaments.Domain;

namespace Tournaments.Domain.Tests;

public class EloCalculatorTests
{
    [Fact]
    public void Equal_ratings_expect_draw()
    {
        Assert.Equal(0.5, EloCalculator.ExpectedScore(1500, 1500), precision: 5);
    }

    [Fact]
    public void Win_between_equals_moves_rating_by_half_k()
    {
        var (winner, loser) = EloCalculator.Update(1500, 1500, scoreA: 1, k: 32);

        Assert.Equal(1516, winner);
        Assert.Equal(1484, loser);
    }

    [Fact]
    public void Draw_between_equals_changes_nothing()
    {
        var (a, b) = EloCalculator.Update(1500, 1500, scoreA: 0.5, k: 32);

        Assert.Equal(1500, a);
        Assert.Equal(1500, b);
    }

    [Fact]
    public void Favorite_gains_less_from_a_win_than_underdog()
    {
        var (favorite, underdogLoses) = EloCalculator.Update(1600, 1400, scoreA: 1, k: 32);
        var (underdog, favoriteLoses) = EloCalculator.Update(1400, 1600, scoreA: 1, k: 32);

        Assert.Equal(1608, favorite);
        Assert.Equal(1392, underdogLoses);
        Assert.Equal(1424, underdog);
        Assert.Equal(1576, favoriteLoses);
    }

    [Fact]
    public void Rejects_unknown_score_and_non_positive_k()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EloCalculator.Update(1500, 1500, scoreA: 0.7, k: 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => EloCalculator.Update(1500, 1500, scoreA: 1, k: 0));
    }
}