# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY BookMatcher.Api/BookMatcher.Api.csproj BookMatcher.Api/
COPY BookMatcher.Common/BookMatcher.Common.csproj BookMatcher.Common/
COPY BookMatcher.Services/BookMatcher.Services.csproj BookMatcher.Services/
RUN dotnet restore BookMatcher.Api/BookMatcher.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/BookMatcher.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookMatcher.Api.dll"]
