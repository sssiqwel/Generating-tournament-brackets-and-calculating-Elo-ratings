namespace Tournaments.Domain;

public sealed record SwissPlayer(
    int PlayerId,
    int Score,
    IReadOnlyCollection<int> PreviousOpponentIds,
    bool ReceivedBye);

public sealed record SwissMatch(int PlayerAId, int PlayerBId);

public sealed record SwissPairing(IReadOnlyList<SwissMatch> Matches, int? ByePlayerId);

public static class SwissScheduler
{
    public static SwissPairing Pair(IReadOnlyList<SwissPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count < 2)
            throw new ArgumentException("Нужно минимум два участника.", nameof(players));

        if (players.Select(player => player.PlayerId).Distinct().Count() != players.Count)
            throw new ArgumentException("Идентификаторы участников должны быть уникальны.", nameof(players));

        var ordered = players
            .OrderByDescending(player => player.Score)
            .ThenBy(player => player.PlayerId)
            .ToList();

        int? byePlayerId = null;
        if (ordered.Count % 2 == 1)
        {
            var bye = ordered.LastOrDefault(player => !player.ReceivedBye) ?? ordered[^1];
            byePlayerId = bye.PlayerId;
            ordered.Remove(bye);
        }

        var opponents = ordered.ToDictionary(
            player => player.PlayerId,
            player => player.PreviousOpponentIds.ToHashSet());
        var remaining = ordered.Select(player => player.PlayerId).ToList();
        var matches = new List<SwissMatch>();

        if (!TryPair(remaining, opponents, matches))
            throw new InvalidOperationException("Не удалось составить пары без повторных встреч.");

        return new SwissPairing(matches, byePlayerId);
    }

    private static bool TryPair(
        List<int> remaining,
        Dictionary<int, HashSet<int>> opponents,
        List<SwissMatch> matches)
    {
        if (remaining.Count == 0)
            return true;

        var playerId = remaining[0];

        for (var index = 1; index < remaining.Count; index++)
        {
            var opponentId = remaining[index];
            if (opponents[playerId].Contains(opponentId) || opponents[opponentId].Contains(playerId))
                continue;

            remaining.RemoveAt(index);
            remaining.RemoveAt(0);
            matches.Add(new SwissMatch(playerId, opponentId));

            if (TryPair(remaining, opponents, matches))
                return true;

            matches.RemoveAt(matches.Count - 1);
            remaining.Insert(0, playerId);
            remaining.Insert(index, opponentId);
        }

        return false;
    }
}