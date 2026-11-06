namespace Tournaments.Api.Data;

public sealed class Tournament
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string System { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public List<Entry> Players { get; set; } = [];
    public List<Match> Matches { get; set; } = [];
}

public sealed class Entry
{
    public Guid TournamentId { get; set; }
    public Guid PlayerId { get; set; }
    public int? Seed { get; set; }
    public Tournament? Tournament { get; set; }
}

public sealed class Match
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public int RoundNumber { get; set; }
    public int IndexInRound { get; set; }
    public Guid? PlayerAId { get; set; }
    public Guid? PlayerBId { get; set; }
    public Guid? WinnerId { get; set; }
    public double? ScoreA { get; set; }
    public Tournament? Tournament { get; set; }
}