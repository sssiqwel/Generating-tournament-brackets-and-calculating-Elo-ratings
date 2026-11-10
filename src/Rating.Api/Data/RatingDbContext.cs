using Microsoft.EntityFrameworkCore;

namespace Rating.Api.Data;

public sealed class RatingDbContext(DbContextOptions<RatingDbContext> options) : DbContext(options)
{
    public DbSet<PlayerRating> Players => Set<PlayerRating>();
    public DbSet<ProcessedMatch> ProcessedMatches => Set<ProcessedMatch>();
    public DbSet<RatingChange> RatingChanges => Set<RatingChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRating>().HasKey(player => player.PlayerId);
        modelBuilder.Entity<ProcessedMatch>().HasKey(match => match.MatchId);
    }
}