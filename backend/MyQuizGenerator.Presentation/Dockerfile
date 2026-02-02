# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MyQuizGenerator.sln", "."]
COPY ["MyQuizGenerator.Presentation/MyQuizGenerator.Presentation.csproj", "MyQuizGenerator.Presentation/"]
COPY ["MyQuizGenerator.Application/MyQuizGenerator.Application.csproj", "MyQuizGenerator.Application/"]
COPY ["MyQuizGenerator.Domain/MyQuizGenerator.Domain.csproj", "MyQuizGenerator.Domain/"]
COPY ["MyQuizGenerator.Infrastructure/MyQuizGenerator.Infrastructure.csproj", "MyQuizGenerator.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MyQuizGenerator.Presentation/MyQuizGenerator.Presentation.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MyQuizGenerator.Presentation"
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
ENTRYPOINT ["dotnet", "MyQuizGenerator.Presentation.dll"]
