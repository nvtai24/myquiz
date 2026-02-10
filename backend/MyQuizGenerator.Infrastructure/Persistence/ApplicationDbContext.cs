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
    public DbSet<Deck> Decks { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<DeckInvitation> DeckInvitations { get; set; }
    public DbSet<DeckMember> DeckMembers { get; set; }

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

            entity.HasOne<Deck>()
                .WithMany(d => d.Documents)
                .HasForeignKey(f => f.DeckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.Property(d => d.Name).IsRequired();
            entity.Property(d => d.Description).IsRequired();
            entity.Property(d => d.Visibility).IsRequired();
            entity.Property(d => d.OwnerId).IsRequired();

            entity.HasOne<AppUser>()
                .WithMany(u => u.Decks)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(q => q.Content).IsRequired();
            entity.Property(q => q.Type).IsRequired();
            entity.Property(q => q.Hint).IsRequired();
            entity.Property(q => q.Explanation).IsRequired();
            entity.Property(q => q.Options).IsRequired();
            entity.Property(q => q.CorrectAnswers).IsRequired();
            entity.Property(q => q.DeckId).IsRequired();

            entity.HasOne(q => q.Deck)
                .WithMany(d => d.Questions)
                .HasForeignKey(q => q.DeckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeckInvitation>(entity =>
        {
            entity.Property(d => d.DeckId).IsRequired();
            entity.Property(d => d.Email).IsRequired();
            entity.Property(d => d.Token).IsRequired();
            entity.Property(d => d.SharedAt).IsRequired();
            entity.Property(d => d.Status).IsRequired();

            entity.HasOne(d => d.Deck)
                .WithMany(d => d.DeckInvitations)
                .HasForeignKey(d => d.DeckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeckMember>(entity =>
        {
            entity.HasKey(dm => dm.Id);
            entity.Property(dm => dm.UserId).IsRequired();
            entity.Property(dm => dm.JoinedAt).IsRequired();

            entity.HasOne(dm => dm.Deck)
                .WithMany(d => d.DeckMembers)
                .HasForeignKey(dm => dm.DeckId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany(u => u.DeckMembers)
                .HasForeignKey(dm => dm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }


    // convert enum to string in EF Core
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

}
