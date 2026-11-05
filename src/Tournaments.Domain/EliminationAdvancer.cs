namespace Tournaments.Domain;

public static class EliminationAdvancer
{
    public static EliminationBracket AdvanceByes(EliminationBracket bracket)
    {
        ArgumentNullException.ThrowIfNull(bracket);

        var current = bracket;

        foreach (var match in bracket.Rounds[0].Where(match => match.IsBye))
        {
            var source = Find(current, match.RoundNumber, match.IndexInRound);
            if (source.WinnerId is not null)
                continue;

            var winnerId = (source.PlayerAId ?? source.PlayerBId)!.Value;
            current = PlaceWinner(current, source, winnerId);
        }

        return current;
    }

    public static EliminationBracket RecordResult(
        EliminationBracket bracket,
        int roundNumber,
        int matchIndex,
        int winnerId)
    {
        ArgumentNullException.ThrowIfNull(bracket);

        var match = Find(bracket, roundNumber, matchIndex);

        if (match.PlayerAId is null || match.PlayerBId is null)
            throw new InvalidOperationException("Оба участника матча ещё не определены.");

        if (match.WinnerId is not null)
            throw new InvalidOperationException("Результат матча уже записан.");

        if (winnerId != match.PlayerAId && winnerId != match.PlayerBId)
            throw new ArgumentException("Победитель должен быть одним из участников матча.", nameof(winnerId));

        return PlaceWinner(bracket, match, winnerId);
    }

    private static EliminationBracket PlaceWinner(EliminationBracket bracket, EliminationMatch match, int winnerId)
    {
        var rounds = bracket.Rounds.Select(round => round.ToList()).ToList();
        var currentRound = rounds[match.RoundNumber - 1];
        currentRound[match.IndexInRound] = match with { WinnerId = winnerId };

        if (match.NextMatchIndex is int nextIndex)
        {
            var nextRound = rounds[match.RoundNumber];
            var next = nextRound[nextIndex];
            nextRound[nextIndex] = match.FeedsPlayerA
                ? next with { PlayerAId = winnerId }
                : next with { PlayerBId = winnerId };
        }

        return new EliminationBracket(
            bracket.BracketSize,
            rounds.Select(IReadOnlyList<EliminationMatch> (round) => round).ToList());
    }

    private static EliminationMatch Find(EliminationBracket bracket, int roundNumber, int matchIndex)
    {
        if (roundNumber < 1 || roundNumber > bracket.Rounds.Count)
            throw new ArgumentOutOfRangeException(nameof(roundNumber));

        var round = bracket.Rounds[roundNumber - 1];
        if (matchIndex < 0 || matchIndex >= round.Count)
            throw new ArgumentOutOfRangeException(nameof(matchIndex));

        return round[matchIndex];
    }
}