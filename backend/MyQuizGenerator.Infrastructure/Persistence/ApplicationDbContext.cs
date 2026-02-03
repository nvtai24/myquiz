
using Microsoft.EntityFrameworkCore;

namespace MyQuizGenerator.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }


    // // TODO: Add entities
    // public DbSet<Quiz> Quizzes { get; set; }
    // public DbSet<Question> Questions { get; set; }
    // public DbSet<Answer> Answers { get; set; }

    // Fluent API
    // Used to configure the database schema
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}