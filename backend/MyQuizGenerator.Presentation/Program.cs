using Microsoft.OpenApi.Models;
using MyQuizGenerator.Application;
using MyQuizGenerator.Infrastructure;
using MyQuizGenerator.Infrastructure.Persistence;
using MyQuizGenerator.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Service Configuration
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyQuiz Generator API",
        Version = "v1",
        Description = "API for Quiz Generation with JWT Authentication"
    });

    // JWT Bearer security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the text input below.\n\nExample: eyJhbGciOiJIUzI1NiIs..."
    });

    // Apply security requirement globally
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:4200", "https://learn.myquiz.fun", "https://play.myquiz.fun", "https://myquiz-fu.vercel.app")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Clean Architecture layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Background jobs
builder.Services.AddHostedService<MyQuizGenerator.Infrastructure.BackgroundJobs.ExpirePaymentTransactionsJob>();

var app = builder.Build();

// Database seeding - creates default roles and admin user
await DatabaseSeeder.SeedAsync(app.Services);

// Middleware Pipeline - order matters

// 1. Request logging
app.UseRequestLogging();

// 2. Global exception handler
app.UseGlobalExceptionHandler();



// Disable Swagger in production - comment out if you want to enable it
// 3. Development tools
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

// 4. HTTPS redirection
app.UseHttpsRedirection();

// 5. CORS - must be before Authentication/Authorization
app.UseCors("AllowClient");

// 6. Authentication - validates JWT token and populates User
app.UseAuthentication();

// 7. Authorization - checks roles and policies
app.UseAuthorization();

// 8. Map controllers
app.MapControllers();

app.Run();
