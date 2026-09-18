# --- build ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file changes.
COPY BCAS.sln ./
COPY src/BCAS.Domain/BCAS.Domain.csproj src/BCAS.Domain/
COPY src/BCAS.Application/BCAS.Application.csproj src/BCAS.Application/
COPY src/BCAS.Infrastructure/BCAS.Infrastructure.csproj src/BCAS.Infrastructure/
COPY src/BCAS.Api/BCAS.Api.csproj src/BCAS.Api/
COPY tests/BCAS.UnitTests/BCAS.UnitTests.csproj tests/BCAS.UnitTests/
RUN dotnet restore BCAS.sln

COPY . .
RUN dotnet publish src/BCAS.Api/BCAS.Api.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the non-root user shipped with the runtime image.
USER $APP_UID

COPY --from=build /app .
ENTRYPOINT ["dotnet", "BCAS.Api.dll"]
