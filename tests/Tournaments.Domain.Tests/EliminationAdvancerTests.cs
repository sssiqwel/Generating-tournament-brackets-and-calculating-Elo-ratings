using Tournaments.Domain;

namespace Tournaments.Domain.Tests;

public class EliminationAdvancerTests
{
    [Fact]
    public void Winner_of_first_match_takes_side_a()
    {
        var bracket = SingleEliminationScheduler.Generate([1, 2, 3, 4, 5, 6, 7, 8]);

        var updated = EliminationAdvancer.RecordResult(bracket, roundNumber: 1, matchIndex: 0, winnerId: 1);
        var played = updated.Rounds[0][0];
        var next = updated.Rounds[1][0];

        Assert.Equal(1, played.WinnerId);
        Assert.Equal(1, next.PlayerAId);
        Assert.Null(next.PlayerBId);
        Assert.Null(bracket.Rounds[1][0].PlayerAId);
    }

    [Fact]
    public void Winner_of_second_match_takes_side_b()
    {
        var bracket = SingleEliminationScheduler.Generate([1, 2, 3, 4, 5, 6, 7, 8]);

        var updated = EliminationAdvancer.RecordResult(bracket, roundNumber: 1, matchIndex: 1, winnerId: 5);

        Assert.Equal(5, updated.Rounds[0][1].WinnerId);
        Assert.Null(updated.Rounds[1][0].PlayerAId);
        Assert.Equal(5, updated.Rounds[1][0].PlayerBId);
    }

    [Fact]
    public void Bye_and_match_winner_meet_in_the_final()
    {
        var bracket = EliminationAdvancer.AdvanceByes(SingleEliminationScheduler.Generate([1, 2, 3]));
        var afterSemifinal = EliminationAdvancer.RecordResult(bracket, roundNumber: 1, matchIndex: 1, winnerId: 3);
        var afterFinal = EliminationAdvancer.RecordResult(afterSemifinal, roundNumber: 2, matchIndex: 0, winnerId: 1);

        Assert.Equal(1, bracket.Rounds[1][0].PlayerAId);
        Assert.Null(bracket.Rounds[1][0].PlayerBId);
        Assert.Equal(1, afterSemifinal.Rounds[1][0].PlayerAId);
        Assert.Equal(3, afterSemifinal.Rounds[1][0].PlayerBId);
        Assert.Equal(1, afterFinal.Rounds[1][0].WinnerId);
        Assert.Null(afterFinal.Rounds[1][0].NextMatchIndex);
    }

    [Fact]
    public void Rejects_unready_repeated_and_foreign_winner()
    {
        var bracket = EliminationAdvancer.AdvanceByes(SingleEliminationScheduler.Generate([1, 2, 3]));
        var played = EliminationAdvancer.RecordResult(bracket, roundNumber: 1, matchIndex: 1, winnerId: 2);

        Assert.Throws<InvalidOperationException>(() => EliminationAdvancer.RecordResult(bracket, 1, 0, 1));
        Assert.Throws<InvalidOperationException>(() => EliminationAdvancer.RecordResult(bracket, 2, 0, 1));
        Assert.Throws<InvalidOperationException>(() => EliminationAdvancer.RecordResult(played, 1, 1, 3));
        Assert.Throws<ArgumentException>(() => EliminationAdvancer.RecordResult(bracket, 1, 1, 9));
    }
}