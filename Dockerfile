# Stage 1: Build .NET 9 application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Gasbel.Domain/Gasbel.Domain.csproj", "src/Gasbel.Domain/"]
COPY ["src/Gasbel.Application/Gasbel.Application.csproj", "src/Gasbel.Application/"]
COPY ["src/Gasbel.Infrastructure/Gasbel.Infrastructure.csproj", "src/Gasbel.Infrastructure/"]
COPY ["src/Gasbel.Web/Gasbel.Web.csproj", "src/Gasbel.Web/"]

RUN dotnet restore "src/Gasbel.Web/Gasbel.Web.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/src/Gasbel.Web"
RUN dotnet publish "Gasbel.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Ensure data and uploads directories exist
RUN mkdir -p /app/data /app/wwwroot/uploads

VOLUME ["/app/data", "/app/wwwroot/uploads"]

ENTRYPOINT ["dotnet", "Gasbel.Web.dll"]
