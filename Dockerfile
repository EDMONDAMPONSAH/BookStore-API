# --- BUILD STAGE ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY BookStore.Api.csproj ./
RUN dotnet restore ./BookStore.Api.csproj

COPY . ./
RUN dotnet publish ./BookStore.Api.csproj -c Release -o /app/publish

# --- RUNTIME STAGE ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5009
ENTRYPOINT ["dotnet", "BookStore.Api.dll"]
