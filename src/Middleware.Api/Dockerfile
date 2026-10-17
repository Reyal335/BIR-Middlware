FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first (layer caching), preserving folder structure
COPY ["Middleware.Api/Middleware.Api.csproj", "Middleware.Api/"]
COPY ["Middleware.Core/Middleware.Core.csproj", "Middleware.Core/"]
COPY ["Middleware.Infra/Middleware.Infra.csproj", "Middleware.Infra/"]
RUN dotnet restore "Middleware.Api/Middleware.Api.csproj"

# Copy source, again one folder per project
COPY Middleware.Api/ Middleware.Api/
COPY Middleware.Core/ Middleware.Core/
COPY Middleware.Infra/ Middleware.Infra/
RUN dotnet publish "Middleware.Api/Middleware.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Middleware.Api.dll"]
