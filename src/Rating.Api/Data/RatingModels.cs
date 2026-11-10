namespace Rating.Api.Data;

public sealed class PlayerRating
{
    public Guid PlayerId { get; set; }
    public int Rating { get; set; } = 1500;
}

public sealed class ProcessedMatch
{
    public Guid MatchId { get; set; }
}

public sealed class RatingChange
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
}