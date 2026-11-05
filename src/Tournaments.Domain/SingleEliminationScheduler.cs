namespace Tournaments.Domain;
public sealed record EliminationMatch(
    int RoundNumber,
    int IndexInRound,
    int? PlayerAId,
    int? PlayerBId,
    int? NextMatchIndex,
    bool FeedsPlayerA,
    int? WinnerId = null)
{
    public bool IsBye => (PlayerAId is null) ^ (PlayerBId is null);
}

public sealed record EliminationBracket(
    int BracketSize,
    IReadOnlyList<IReadOnlyList<EliminationMatch>> Rounds);

public static class SingleEliminationScheduler
{
    public static EliminationBracket Generate(IReadOnlyList<int> seededPlayerIds)
    {
        ArgumentNullException.ThrowIfNull(seededPlayerIds);

        if (seededPlayerIds.Count < 2)
            throw new ArgumentException("Нужно минимум два участника.", nameof(seededPlayerIds));

        if (seededPlayerIds.Distinct().Count() != seededPlayerIds.Count)
            throw new ArgumentException("Идентификаторы участников должны быть уникальны.", nameof(seededPlayerIds));

        var bracketSize = NextPowerOfTwo(seededPlayerIds.Count);
        var order = SeedOrder(bracketSize);
        var slots = new int?[bracketSize];

        for (var slot = 0; slot < bracketSize; slot++)
        {
            var seed = order[slot];
            slots[slot] = seed <= seededPlayerIds.Count ? seededPlayerIds[seed - 1] : null;
        }

        var rounds = new List<IReadOnlyList<EliminationMatch>>();
        var matchesInRound = bracketSize / 2;

        for (var roundNumber = 1; matchesInRound >= 1; roundNumber++)
        {
            var matches = new List<EliminationMatch>(matchesInRound);
            var hasNextRound = matchesInRound > 1;

            for (var index = 0; index < matchesInRound; index++)
            {
                int? playerA = null;
                int? playerB = null;

                if (roundNumber == 1)
                {
                    playerA = slots[index * 2];
                    playerB = slots[index * 2 + 1];
                }

                matches.Add(new EliminationMatch(
                    roundNumber,
                    index,
                    playerA,
                    playerB,
                    hasNextRound ? index / 2 : null,
                    hasNextRound && index % 2 == 0));
            }

            rounds.Add(matches);
            matchesInRound /= 2;
        }

        return new EliminationBracket(bracketSize, rounds);
    }

    private static int NextPowerOfTwo(int count)
    {
        var size = 1;
        while (size < count)
            size *= 2;

        return size;
    }

    private static List<int> SeedOrder(int bracketSize)
    {
        var positions = new List<int> { 1 };

        while (positions.Count < bracketSize)
        {
            var mirror = positions.Count * 2 + 1;
            var next = new List<int>(positions.Count * 2);

            foreach (var seed in positions)
            {
                next.Add(seed);
                next.Add(mirror - seed);
            }

            positions = next;
        }

        return positions;
    }
}