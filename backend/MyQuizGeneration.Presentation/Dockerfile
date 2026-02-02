# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MyQuizGenerator.sln", "."]
COPY ["MyQuizGeneration.Presentation/MyQuizGeneration.Presentation.csproj", "MyQuizGeneration.Presentation/"]
COPY ["MyQuizGenerator.Application/MyQuizGenerator.Application.csproj", "MyQuizGenerator.Application/"]
COPY ["MyQuizGeneration.Domain/MyQuizGeneration.Domain.csproj", "MyQuizGeneration.Domain/"]
COPY ["MyQuizGenerator.Infrastructure/MyQuizGenerator.Infrastructure.csproj", "MyQuizGenerator.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MyQuizGeneration.Presentation/MyQuizGeneration.Presentation.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MyQuizGeneration.Presentation"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyQuizGeneration.Presentation.dll"]
