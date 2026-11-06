using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(user =>
        {
            user.HasIndex(account => account.Email).IsUnique();
            user.Property(account => account.Email).HasMaxLength(256);
            user.Property(account => account.DisplayName).HasMaxLength(100);
            user.Property(account => account.Role).HasMaxLength(32);
        });
    }
}