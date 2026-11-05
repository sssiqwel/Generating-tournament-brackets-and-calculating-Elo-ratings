using Tournaments.Domain;

namespace Tournaments.Domain.Tests;

public class SwissSchedulerTests
{
    [Fact]
    public void New_players_are_paired_with_neighbors()
    {
        var pairing = SwissScheduler.Pair([
            Player(1), Player(2), Player(3), Player(4)
        ]);

        Assert.Null(pairing.ByePlayerId);
        Assert.Equal([new SwissMatch(1, 2), new SwissMatch(3, 4)], pairing.Matches);
    }

    [Fact]
    public void Equal_scores_play_inside_their_group()
    {
        var pairing = SwissScheduler.Pair([
            Player(1, score: 2), Player(2, score: 2), Player(3), Player(4)
        ]);

        Assert.Equal([new SwissMatch(1, 2), new SwissMatch(3, 4)], pairing.Matches);
    }

    [Fact]
    public void Bye_goes_to_lowest_player_who_has_not_had_one()
    {
        var first = SwissScheduler.Pair([Player(1), Player(2), Player(3)]);
        var second = SwissScheduler.Pair([
            Player(1), Player(2), Player(3, receivedBye: true)
        ]);

        Assert.Equal(3, first.ByePlayerId);
        Assert.Equal([new SwissMatch(1, 2)], first.Matches);
        Assert.Equal(2, second.ByePlayerId);
        Assert.Equal([new SwissMatch(1, 3)], second.Matches);
    }

    [Fact]
    public void Backtracks_when_nearest_opponent_blocks_the_rest()
    {
        var pairing = SwissScheduler.Pair([
            Player(1, opponents: [2]),
            Player(2, opponents: [1, 4]),
            Player(3),
            Player(4, opponents: [2])
        ]);

        Assert.Equal([new SwissMatch(1, 4), new SwissMatch(2, 3)], pairing.Matches);
    }

    [Fact]
    public void Leader_plays_nearest_and_lowest_receives_bye()
    {
        var pairing = SwissScheduler.Pair([
            Player(5, score: 2), Player(1), Player(2), Player(3), Player(4)
        ]);

        Assert.Equal(4, pairing.ByePlayerId);
        Assert.Equal([new SwissMatch(5, 1), new SwissMatch(2, 3)], pairing.Matches);
    }

    [Fact]
    public void Rejects_bad_input_and_impossible_round()
    {
        Assert.Throws<ArgumentException>(() => SwissScheduler.Pair([Player(1)]));
        Assert.Throws<ArgumentException>(() => SwissScheduler.Pair([Player(1), Player(1)]));
        Assert.Throws<InvalidOperationException>(() => SwissScheduler.Pair([
            Player(1, opponents: [2]),
            Player(2, opponents: [1]),
            Player(3)
        ]));
    }

    private static SwissPlayer Player(
        int playerId,
        int score = 0,
        int[]? opponents = null,
        bool receivedBye = false) =>
        new(playerId, score, opponents ?? [], receivedBye);
}