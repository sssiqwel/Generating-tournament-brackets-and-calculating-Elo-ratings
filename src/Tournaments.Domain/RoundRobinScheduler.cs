namespace Tournaments.Domain;

public sealed record RoundRobinMatch(int HomeId, int AwayId);

public sealed record RoundRobinRound(int Number, IReadOnlyList<RoundRobinMatch> Matches);

public static class RoundRobinScheduler
{
    public static IReadOnlyList<RoundRobinRound> Generate(IReadOnlyList<int> playerIds)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        if (playerIds.Count < 2)
            throw new ArgumentException("Нужно минимум два участника.", nameof(playerIds));

        if (playerIds.Distinct().Count() != playerIds.Count)
            throw new ArgumentException("Идентификаторы участников должны быть уникальны.", nameof(playerIds));

        var seats = new List<int?>(playerIds.Count + 1);
        foreach (var id in playerIds)
            seats.Add(id);

        if (seats.Count % 2 == 1)
            seats.Add(null);

        var count = seats.Count;
        var pairsPerRound = count / 2;
        var rounds = new List<RoundRobinRound>(count - 1);

        for (var round = 0; round < count - 1; round++)
        {
            var matches = new List<RoundRobinMatch>(pairsPerRound);

            for (var i = 0; i < pairsPerRound; i++)
            {
                var home = seats[i];
                var away = seats[count - 1 - i];

                if (home is int homeId && away is int awayId)
                    matches.Add(new RoundRobinMatch(homeId, awayId));
            }

            rounds.Add(new RoundRobinRound(round + 1, matches));

            var last = seats[count - 1];
            for (var i = count - 1; i > 1; i--)
                seats[i] = seats[i - 1];
            seats[1] = last;
        }

        return rounds;
    }
}