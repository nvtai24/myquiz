using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Infrastructure.Identity;

namespace MyQuizGenerator.Infrastructure.Persistence;

/// <summary>
/// Application database context with ASP.NET Identity support.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Ignore(u => u.FullName);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(r => r.Token).IsRequired();
            entity.Property(r => r.JwtId).IsRequired();
            entity.Property(r => r.CreationAt).IsRequired();
            entity.Property(r => r.ExpiryAt).IsRequired();
            entity.Property(r => r.Used).IsRequired();
            entity.Property(r => r.Invalidated).IsRequired();
            entity.Property(r => r.UserId).IsRequired();

            entity.HasOne<AppUser>()
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.ToTable("UploadedFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.OriginalFileName).IsRequired();
        });
    }

}
