namespace Tournaments.Domain;

public static class EloCalculator
{
    public static double ExpectedScore(int ratingA, int ratingB)
    {
        var exponent = (ratingB - ratingA) / 400.0;
        return 1.0 / (1.0 + Math.Pow(10, exponent));
    }

    public static (int RatingA, int RatingB) Update(int ratingA, int ratingB, double scoreA, int k)
    {
        if (scoreA is not (0d or 0.5d or 1d))
            throw new ArgumentOutOfRangeException(nameof(scoreA), "Счёт должен быть 1, 0.5 или 0.");

        if (k <= 0)
            throw new ArgumentOutOfRangeException(nameof(k), "Коэффициент K должен быть больше нуля.");

        var expectedA = ExpectedScore(ratingA, ratingB);
        var expectedB = 1.0 - expectedA;
        var scoreB = 1.0 - scoreA;

        var newA = Round(ratingA + k * (scoreA - expectedA));
        var newB = Round(ratingB + k * (scoreB - expectedB));
        return (newA, newB);
    }

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}