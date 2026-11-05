using Microsoft.EntityFrameworkCore;
using Sayyit.API.Features.Users.Models;

namespace Sayyit.API.Services;

public sealed class SayyitDbContext(DbContextOptions<SayyitDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users", "auth");
            entity.HasKey(user => user.UserId);

            entity.Property(user => user.UserId)
                .ValueGeneratedOnAdd();

            entity.Property(user => user.EntraObjectId)
                .IsRequired();

            entity.Property(user => user.TenantId)
                .IsRequired();

            entity.Property(user => user.UserName)
                .HasMaxLength(50);

            entity.Property(user => user.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(320);

            entity.Property(user => user.UpdatedUtc)
                .HasPrecision(3)
                .IsRequired();

            entity.Property(user => user.LastSeenUtc)
                .HasPrecision(3);

            entity.HasIndex(user => new { user.TenantId, user.EntraObjectId })
                .IsUnique();

            entity.HasIndex(user => user.UserName)
                .IsUnique();
        });
    }
}
