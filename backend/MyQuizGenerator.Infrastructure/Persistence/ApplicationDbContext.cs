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

    public DbSet<Deck> Decks { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<DeckInvitation> DeckInvitations { get; set; }
    public DbSet<DeckMember> DeckMembers { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<UserAnswer> UserAnswers { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<UserSubscriptionPlan> UserSubscriptionPlans { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<DeckRating> DeckRatings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Ignore(u => u.FullName);
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

            // Global Query Filter for Soft Delete
            entity.HasQueryFilter(q => !q.IsDeleted);
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

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.Property(q => q.DeckId).IsRequired();
            entity.Property(q => q.UserId).IsRequired();
            entity.Property(q => q.StartedAt).IsRequired();
            entity.Property(q => q.EndedAt).IsRequired();
            entity.Property(q => q.TotalTime).IsRequired();
            entity.Property(q => q.TotalQuestions).IsRequired();
            entity.Property(q => q.CorrectAnswers).IsRequired();

            entity.HasOne(q => q.Deck)
                .WithMany(d => d.QuizAttempts)
                .HasForeignKey(q => q.DeckId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.Property(q => q.QuizAttemptId).IsRequired();
            entity.Property(q => q.QuestionId).IsRequired();
            entity.Property(q => q.Answer).IsRequired();
            entity.Property(q => q.IsCorrect).IsRequired();
            entity.Property(q => q.OptionsSnapshot).IsRequired();
            entity.Property(q => q.CorrectAnswersSnapshot).IsRequired();
            entity.Property(q => q.QuestionSnapshot).IsRequired();

            entity.HasOne(q => q.QuizAttempt)
                .WithMany(qa => qa.UserAnswers)
                .HasForeignKey(q => q.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(q => q.Question)
                .WithMany(qa => qa.UserAnswers)
                .HasForeignKey(q => q.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.Property(s => s.Name).IsRequired();
            entity.Property(s => s.Description).IsRequired();
            entity.Property(s => s.DailyGenerateLimit).IsRequired();
            entity.Property(s => s.NumDeckLimit).IsRequired();
            entity.Property(s => s.Price).IsRequired();
            entity.Property(s => s.Duration).IsRequired();
            entity.Property(s => s.IsActive).IsRequired();
            entity.Property(s => s.Order).IsRequired();
            entity.Property(s => s.CreatedAt).IsRequired();
            entity.Property(s => s.UpdatedAt);

            entity.HasMany(s => s.UserSubscriptionPlans)
                .WithOne(usp => usp.SubscriptionPlan)
                .HasForeignKey(usp => usp.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSubscriptionPlan>(entity =>
        {
            entity.Property(s => s.UserId).IsRequired();
            entity.Property(s => s.SubscriptionPlanId).IsRequired();
            entity.Property(s => s.StartDate).IsRequired();
            entity.Property(s => s.EndDate).IsRequired();

            entity.HasOne<AppUser>()
                .WithMany(u => u.UserSubscriptionPlans)
                .HasForeignKey(usp => usp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.SubscriptionPlanId).IsRequired();
            entity.Property(p => p.OrderCode).IsRequired();
            entity.Property(p => p.Amount).IsRequired();
            entity.Property(p => p.Status).IsRequired();
            entity.Property(p => p.Content).IsRequired();
            entity.Property(p => p.Description).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();

            entity.HasIndex(p => p.OrderCode).IsUnique();

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(p => p.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeckRating>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.DeckId).IsRequired();
            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.Rating).IsRequired();
            entity.Property(r => r.CreatedAt).IsRequired();

            entity.HasOne(r => r.Deck)
                .WithMany(d => d.DeckRatings)
                .HasForeignKey(r => r.DeckId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany(u => u.DeckRatings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure one rating per user per deck
            entity.HasIndex(r => new { r.DeckId, r.UserId }).IsUnique();
        });
    }

    // convert enum to string in EF Core
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

}
