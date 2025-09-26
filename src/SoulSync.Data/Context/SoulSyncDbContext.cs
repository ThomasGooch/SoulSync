using Microsoft.EntityFrameworkCore;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using System.Text.Json;

namespace SoulSync.Data.Context;

public class SoulSyncDbContext : DbContext
{
    public SoulSyncDbContext(DbContextOptions<SoulSyncDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(u => u.Bio)
                .HasMaxLength(1000);

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(u => u.LastModifiedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // One-to-one relationship with UserProfile
            entity.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProfile configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId).IsUnique();
            
            entity.Property(p => p.Interests)
                .HasMaxLength(500);
                
            entity.Property(p => p.Location)
                .HasMaxLength(200);
                
            entity.Property(p => p.Occupation)
                .HasMaxLength(200);
                
            entity.Property(p => p.AIInsights)
                .HasMaxLength(2000);

            entity.Property(p => p.GenderIdentity)
                .HasConversion<string>();

            // Configure InterestedInGenders as JSON
            entity.Property(p => p.InterestedInGenders)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<List<GenderIdentity>>(v, JsonSerializerOptions.Default) ?? new List<GenderIdentity>());

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(p => p.LastModifiedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}