using Microsoft.EntityFrameworkCore;

namespace Tournaments.Api.Data;

public sealed class TournamentsDbContext(DbContextOptions<TournamentsDbContext> options) : DbContext(options)
{
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tournament>(tournament =>
        {
            tournament.Property(item => item.Name).HasMaxLength(200);
            tournament.Property(item => item.System).HasMaxLength(32);
            tournament.Property(item => item.Status).HasMaxLength(32);
            tournament.HasMany(item => item.Players).WithOne(player => player.Tournament).HasForeignKey(player => player.TournamentId);
            tournament.HasMany(item => item.Matches).WithOne(match => match.Tournament).HasForeignKey(match => match.TournamentId);
        });

        modelBuilder.Entity<Entry>().HasKey(player => new { player.TournamentId, player.PlayerId });
    }
}